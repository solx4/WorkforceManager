using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// قلب الحسابات الأسبوعية كلها. أسبوع الشغل بيبدأ الخميس وينتهي
    /// الأربع (متفق عليه مع مدير القسم)، وكل أسبوع بيبدأ عداد يوميات
    /// جديد — لكن كل السجلات القديمة محفوظة زي ما هي، لأن الملخص بيتحسب
    /// من السجلات الفعلية (إنتاج/حضور/جزاءات) وقت الطلب مش من عداد مخزّن.
    ///
    /// قواعد الخصم:
    /// - غياب بإذن: محايد تمامًا، مفيش أي خصم.
    /// - غياب بدون إذن: خصم نص يومية (0.5) عن كل يوم غياب.
    /// - الجزاءات: بتتخصم بقيمتها (نص يوم / يوم / 3 أيام / أسبوع = 6 يوميات).
    /// الصافي = المنتَج − خصم الغياب − خصم الجزاءات، وده اللي بيتحدد بيه
    /// أحسن عامل في الأسبوع وبيظهر في تقرير كل عامل.
    /// </summary>
    public class WeeklySummaryService
    {
        /// <summary>خصم الغياب بدون إذن عن اليوم الواحد = نص يومية</summary>
        private const decimal UnexcusedAbsenceDeductionPerDay = 0.5m;

        private readonly IDailyProductionRepository _productionRepo;
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IPenaltyRepository _penaltyRepo;
        private readonly IWorkerRepository _workerRepo;

        public WeeklySummaryService(
            IDailyProductionRepository productionRepo,
            IAttendanceRepository attendanceRepo,
            IPenaltyRepository penaltyRepo,
            IWorkerRepository workerRepo)
        {
            _productionRepo = productionRepo;
            _attendanceRepo = attendanceRepo;
            _penaltyRepo = penaltyRepo;
            _workerRepo = workerRepo;
        }

        /// <summary>
        /// يحسب بداية ونهاية أسبوع الشغل (الخميس → الأربع) اللي بيقع
        /// فيه التاريخ المُعطى. مثال: أي يوم من خميس 9/7 لأربع 15/7
        /// يرجع (9/7 ، 15/7).
        /// </summary>
        public static (DateTime WeekStart, DateTime WeekEnd) GetWorkWeekRange(DateTime anyDate)
        {
            var date = anyDate.Date;
            // عدد الأيام اللي فاتت من آخر يوم خميس: الخميس نفسه = 0، الجمعة = 1 ... الأربع = 6
            int daysSinceThursday = ((int)date.DayOfWeek - (int)DayOfWeek.Thursday + 7) % 7;
            var weekStart = date.AddDays(-daysSinceThursday);
            return (weekStart, weekStart.AddDays(6));
        }

        /// <summary>
        /// يبني الملخص الأسبوعي الكامل لكل العمال النشطين في الأسبوع اللي
        /// بيقع فيه التاريخ المعطى، مرتبين تنازليًا بصافي اليوميات، مع
        /// تحديد أحسن عامل في الأسبوع (أعلى صافي، بشرط إنه أنتج فعلاً).
        /// </summary>
        public async Task<List<WorkerWeeklySummaryDto>> GetTeamWeeklySummaryAsync(DateTime anyDateInWeek)
        {
            var (weekStart, weekEnd) = GetWorkWeekRange(anyDateInWeek);

            // تحميل كل بيانات الأسبوع مرة واحدة (3 استعلامات بس مهما كان عدد العمال)
            var production = await _productionRepo.GetByRangeAsync(weekStart, weekEnd);
            var attendance = await _attendanceRepo.GetByRangeAsync(weekStart, weekEnd);
            var penalties = await _penaltyRepo.GetByRangeAsync(weekStart, weekEnd);

            // كل العمال اللي ليهم أي نشاط في الأسبوع (إنتاج أو حضور أو جزاء) لازم يظهروا في الملخص
            var workerIds = production.Select(p => p.WorkerId)
                .Concat(attendance.Select(a => a.WorkerId))
                .Concat(penalties.Select(p => p.WorkerId))
                .Distinct()
                .ToList();

            var summaries = new List<WorkerWeeklySummaryDto>();

            foreach (var workerId in workerIds)
            {
                var workerProduction = production.Where(p => p.WorkerId == workerId).ToList();
                var workerAttendance = attendance.Where(a => a.WorkerId == workerId).ToList();
                var workerPenalties = penalties.Where(p => p.WorkerId == workerId).ToList();

                // اسم العامل من أي سجل متاح (كلهم محمّلين بـ Include)
                var workerRef = workerProduction.FirstOrDefault()?.Worker
                    ?? workerAttendance.FirstOrDefault()?.Worker
                    ?? workerPenalties.First().Worker;

                var absentWithoutPermission = workerAttendance.Count(a => a.Status == AttendanceStatus.AbsentWithoutPermission);

                summaries.Add(new WorkerWeeklySummaryDto
                {
                    WorkerId = workerId,
                    WorkerName = workerRef.FullName,
                    EmployeeCode = workerRef.EmployeeCode,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,

                    ProducedWorkdays = workerProduction.Sum(p => p.WorkdaysCompleted),
                    TotalPieces = workerProduction.Sum(p => p.PieceCount),
                    // تفصيل الإنتاج مجمّع حسب المرحلة (نفس المرحلة ممكن تتكرر في كذا يوم خلال الأسبوع)
                    Breakdown = workerProduction
                        .GroupBy(p => p.ProductionStageId)
                        .Select(g => new StageBreakdownDto
                        {
                            ProductName = g.First().ProductionStage.Product.Name,
                            StageName = g.First().ProductionStage.StageName,
                            PieceCount = g.Sum(p => p.PieceCount),
                            PiecesPerWorkday = g.First().PiecesPerWorkdayAtEntry
                        })
                        .ToList(),

                    PresentDays = workerAttendance.Count(a => a.Status == AttendanceStatus.Present),
                    AbsentWithPermissionDays = workerAttendance.Count(a => a.Status == AttendanceStatus.AbsentWithPermission),
                    AbsentWithoutPermissionDays = absentWithoutPermission,
                    // قاعدة الخصم المتفق عليها: نص يومية عن كل يوم غياب بدون إذن
                    AbsenceDeduction = absentWithoutPermission * UnexcusedAbsenceDeductionPerDay,

                    Penalties = workerPenalties.Select(p => new PenaltySummaryDto
                    {
                        PenaltyId = p.Id,
                        Date = p.Date,
                        Reason = p.Reason,
                        Deduction = p.Deduction
                    }).ToList(),
                    PenaltyDeduction = workerPenalties.Sum(p => p.DeductedWorkdays)
                });
            }

            // الترتيب بصافي اليوميات (بعد كل الخصومات) — ده أساس تقييم الأسبوع
            var ordered = summaries.OrderByDescending(s => s.NetWorkdays).ToList();

            // أحسن عامل في الأسبوع = أعلى صافي، بشرط إن صافيه موجب وأنتج فعلاً
            // (عشان أسبوع كله غياب وجزاءات ميطلعش فيه "أحسن عامل" بصافي صفر أو سالب)
            var best = ordered.FirstOrDefault(s => s.ProducedWorkdays > 0 && s.NetWorkdays > 0);
            if (best is not null)
                best.IsBestWorkerOfWeek = true;

            return ordered;
        }

        /// <summary>
        /// الملخص الأسبوعي لعامل واحد بس (لبروفايله وتقريره الفردي).
        /// بيرجع null لو العامل مالوش أي نشاط في الأسبوع ده.
        /// </summary>
        public async Task<WorkerWeeklySummaryDto?> GetWorkerWeeklySummaryAsync(int workerId, DateTime anyDateInWeek)
        {
            var team = await GetTeamWeeklySummaryAsync(anyDateInWeek);
            return team.FirstOrDefault(s => s.WorkerId == workerId);
        }

        /// <summary>
        /// الهستوري الأسبوعي الكامل لعامل: ملخص كل أسبوع من تاريخ بدايته
        /// (أو من أول سجل ليه) لحد النهاردة — ده اللي بيظهر في بروفايل
        /// العامل كسجل تاريخي، والقديم كله محفوظ لأنه بيتحسب من السجلات الدائمة.
        /// </summary>
        public async Task<List<WorkerWeeklySummaryDto>> GetWorkerWeeklyHistoryAsync(int workerId, DateTime from, DateTime to)
        {
            var history = new List<WorkerWeeklySummaryDto>();

            // نمشي أسبوع بأسبوع من بداية أسبوع "from" لحد الأسبوع اللي فيه "to"
            var (cursor, _) = GetWorkWeekRange(from);
            var (lastWeekStart, _) = GetWorkWeekRange(to);

            while (cursor <= lastWeekStart)
            {
                var summary = await GetWorkerWeeklySummaryAsync(workerId, cursor);
                if (summary is not null)
                    history.Add(summary);

                cursor = cursor.AddDays(7);
            }

            // الأحدث الأول — أول حاجة المدير عايز يشوفها هي آخر أسبوع
            history.Reverse();
            return history;
        }
    }
}
