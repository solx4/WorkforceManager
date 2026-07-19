using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤول عن تسجيل حضور/غياب العمال، ومنع التسجيل المكرر لنفس اليوم.
    ///
    /// قاعدة حماية أساسية (قرار متفق عليه): ممنوع تسجيل "غياب" لعامل
    /// عنده إنتاج مسجل في نفس اليوم — ده تناقض بيانات وبيسبب خصم غياب
    /// لعامل شغال فعليًا. اللي بيسجل لازم يمسح الإنتاج الأول لو العامل
    /// فعلاً كان غايب.
    /// </summary>
    public class AttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IDailyProductionRepository _productionRepo;

        public AttendanceService(
            IAttendanceRepository attendanceRepo,
            IDailyProductionRepository productionRepo)
        {
            _attendanceRepo = attendanceRepo;
            _productionRepo = productionRepo;
        }

        /// <summary>
        /// يسجل حالة حضور عامل في يوم معين. لو فيه سجل موجود لنفس
        /// العامل ونفس اليوم، بيتم تحديثه بدل ما يتكرر (upsert).
        /// تسجيل غياب لعامل له إنتاج في نفس اليوم بيترفض برسالة واضحة.
        /// </summary>
        public async Task<Attendance> RecordAttendanceAsync(
            int workerId, DateTime date, AttendanceStatus status,
            TimeSpan? checkIn = null, TimeSpan? checkOut = null, string? notes = null)
        {
            if (status != AttendanceStatus.Present)
            {
                var dayProduction = await _productionRepo.GetByDateAsync(date);
                var producer = dayProduction.FirstOrDefault(r => r.WorkerId == workerId);
                if (producer is not null)
                    throw new InvalidOperationException(
                        $"العامل \"{producer.Worker.FullName}\" له إنتاج مسجل في {date:yyyy/MM/dd} — " +
                        "ميصحش يتسجل غايب في نفس اليوم. لو فعلاً كان غايب، امسح إنتاجه الأول من تبويب \"سجلات اليوم\".");
            }

            var existing = await _attendanceRepo.GetByWorkerAndDateAsync(workerId, date);

            if (existing is not null)
            {
                existing.Status = status;
                existing.CheckInTime = status == AttendanceStatus.Present ? checkIn : null;
                existing.CheckOutTime = status == AttendanceStatus.Present ? checkOut : null;
                existing.Notes = notes;
                _attendanceRepo.Update(existing);
                await _attendanceRepo.SaveChangesAsync();
                return existing;
            }

            var record = new Attendance
            {
                WorkerId = workerId,
                Date = date.Date,
                Status = status,
                CheckInTime = status == AttendanceStatus.Present ? checkIn : null,
                CheckOutTime = status == AttendanceStatus.Present ? checkOut : null,
                Notes = notes
            };

            await _attendanceRepo.AddAsync(record);
            await _attendanceRepo.SaveChangesAsync();
            return record;
        }

        /// <summary>
        /// يسجل حضور مجموعة عمال في نفس اليوم دفعة واحدة (Upsert جماعي).
        /// أساس زر "حفظ الحضور" في شاشة التسجيل: بدل ما نعمل استعلام +
        /// حفظ منفصل لكل عامل (اللي بيبقى عشرات الرحلات لقاعدة البيانات)،
        /// بنحمّل سجلات اليوم الموجودة مرة واحدة وبنحفظ كل التعديلات في
        /// حفظة واحدة. لو فيه أي عامل متعلم "غياب" وله إنتاج في نفس اليوم،
        /// الدفعة كلها بتترفض برسالة بتسمّي العمال — يا كله سليم يا مفيش.
        /// بيرجع عدد العمال اللي اتسجلوا/اتحدّثوا.
        /// </summary>
        public async Task<int> RecordAttendanceBatchAsync(
            DateTime date, IEnumerable<(int WorkerId, AttendanceStatus Status)> entries)
        {
            var entryList = entries.ToList();
            if (entryList.Count == 0) return 0;

            // قاعدة الحماية: مفيش غياب لعامل له إنتاج في نفس اليوم
            var producersById = (await _productionRepo.GetByDateAsync(date))
                .GroupBy(r => r.WorkerId)
                .ToDictionary(g => g.Key, g => g.First().Worker.FullName);

            var conflicts = entryList
                .Where(e => e.Status != AttendanceStatus.Present && producersById.ContainsKey(e.WorkerId))
                .Select(e => producersById[e.WorkerId])
                .ToList();

            if (conflicts.Count > 0)
                throw new InvalidOperationException(
                    $"مينفعش تسجيل غياب لعمال لهم إنتاج مسجل في {date:yyyy/MM/dd}:\n" +
                    $"{string.Join("، ", conflicts)}\n\n" +
                    "لو فعلاً كانوا غايبين، امسح إنتاجهم الأول من تبويب \"سجلات اليوم\" — ومفيش أي حالة اتحفظت من الدفعة دي.");

            // كل سجلات اليوم الموجودة مرة واحدة، مفهرسة بالعامل للوصول السريع
            var existingByWorker = (await _attendanceRepo.GetByDateAsync(date))
                .ToDictionary(a => a.WorkerId);

            foreach (var (workerId, status) in entryList)
            {
                if (existingByWorker.TryGetValue(workerId, out var existing))
                {
                    // الحفظ الجماعي بيتعامل مع الحالة بس (من غير أوقات حضور/انصراف)
                    existing.Status = status;
                    existing.CheckInTime = null;
                    existing.CheckOutTime = null;
                    _attendanceRepo.Update(existing);
                }
                else
                {
                    await _attendanceRepo.AddAsync(new Attendance
                    {
                        WorkerId = workerId,
                        Date = date.Date,
                        Status = status
                    });
                }
            }

            await _attendanceRepo.SaveChangesAsync(); // حفظة واحدة لكل التعديلات
            return entryList.Count;
        }
    }
}
