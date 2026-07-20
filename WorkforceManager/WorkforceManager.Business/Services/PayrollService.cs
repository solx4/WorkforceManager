using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// كشف أجور فترة زمنية مخصصة (شهر مثلاً). بيجمّع كل الأيام في المدى
    /// مباشرة — مش أسابيع كاملة زي WeeklySummaryService — عشان المدير
    /// يختار من تاريخ لتاريخ ويطلع إجمالي أجر كل عامل بالجنيه.
    ///
    /// نفس قواعد الحساب: الأجر = صافي اليوميات × سعر اليومية، والصافي =
    /// (إنتاج + شغل بالساعة) − خصم الغياب بدون إذن (نص يومية/يوم) −
    /// خصم الجزاءات. السعر الحالي للعامل بيُطبّق على كل الفترة.
    /// </summary>
    public class PayrollService
    {
        /// <summary>خصم الغياب بدون إذن عن اليوم الواحد = نص يومية (نفس WeeklySummaryService)</summary>
        private const decimal UnexcusedAbsenceDeductionPerDay = 0.5m;

        private readonly IDailyProductionRepository _productionRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IPenaltyRepository _penaltyRepo;
        private readonly IHourlyWorkLogRepository _hourlyRepo;

        public PayrollService(
            IDailyProductionRepository productionRepo,
            IAttendanceRepository attendanceRepo,
            IPenaltyRepository penaltyRepo,
            IHourlyWorkLogRepository hourlyRepo)
        {
            _productionRepo = productionRepo;
            _attendanceRepo = attendanceRepo;
            _penaltyRepo = penaltyRepo;
            _hourlyRepo = hourlyRepo;
        }

        /// <summary>يبني كشف أجور كل العمال اللي لهم نشاط في الفترة [from, to]</summary>
        public async Task<PeriodPayrollDto> GetPeriodPayrollAsync(DateTime from, DateTime to)
        {
            var fromDate = from.Date;
            var toDate = to.Date;

            // تحميل كل بيانات الفترة مرة واحدة (4 استعلامات)
            var production = await _productionRepo.GetByRangeAsync(fromDate, toDate);
            var attendance = await _attendanceRepo.GetByRangeAsync(fromDate, toDate);
            var penalties = await _penaltyRepo.GetByRangeAsync(fromDate, toDate);
            var hourly = await _hourlyRepo.GetByRangeAsync(fromDate, toDate);

            var productionByWorker = production.ToLookup(p => p.WorkerId);
            var attendanceByWorker = attendance.ToLookup(a => a.WorkerId);
            var penaltiesByWorker = penalties.ToLookup(p => p.WorkerId);
            var hourlyByWorker = hourly.ToLookup(h => h.WorkerId);

            var workerIds = productionByWorker.Select(g => g.Key)
                .Concat(attendanceByWorker.Select(g => g.Key))
                .Concat(penaltiesByWorker.Select(g => g.Key))
                .Concat(hourlyByWorker.Select(g => g.Key))
                .Distinct();

            var workers = new List<WorkerPayrollDto>();
            foreach (var workerId in workerIds)
            {
                var wp = productionByWorker[workerId].ToList();
                var wa = attendanceByWorker[workerId].ToList();
                var wpen = penaltiesByWorker[workerId].ToList();
                var wh = hourlyByWorker[workerId].ToList();

                // بيانات العامل (اسم + سعر) من أي سجل محمّل بـ Include للـ Worker
                var workerRef = wp.FirstOrDefault()?.Worker
                    ?? wa.FirstOrDefault()?.Worker
                    ?? wh.FirstOrDefault()?.Worker
                    ?? wpen.First().Worker;

                var producedWorkdays = wp.Sum(p => p.WorkdaysCompleted) + wh.Sum(h => h.WorkdaysCredited);
                var absentWithoutPermission = wa.Count(a => a.Status == AttendanceStatus.AbsentWithoutPermission);

                // عدد أيام العمل الفعلية = أيام فيها إنتاج أو شغل ساعة (بدون تكرار)
                var workDays = wp.Select(p => p.Date.Date)
                    .Concat(wh.Select(h => h.Date.Date))
                    .Distinct()
                    .Count();

                workers.Add(new WorkerPayrollDto
                {
                    WorkerId = workerId,
                    WorkerName = workerRef.FullName,
                    EmployeeCode = workerRef.EmployeeCode,
                    IsHourly = workerRef.IsHourly,
                    DailyWageEgp = workerRef.DailyWageEgp,
                    ProducedWorkdays = producedWorkdays,
                    AbsenceDeduction = absentWithoutPermission * UnexcusedAbsenceDeductionPerDay,
                    PenaltyDeduction = wpen.Sum(p => p.DeductedWorkdays),
                    DaysWorked = workDays
                });
            }

            return new PeriodPayrollDto
            {
                From = fromDate,
                To = toDate,
                Workers = workers.OrderByDescending(w => w.NetWageEgp).ToList()
            };
        }
    }
}
