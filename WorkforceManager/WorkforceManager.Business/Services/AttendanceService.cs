using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤول عن تسجيل حضور/غياب العمال، ومنع التسجيل المكرر لنفس
    /// اليوم، وحساب ملخصات الحضور المستخدمة في ملف العامل والتقييم.
    /// </summary>
    public class AttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepo;

        public AttendanceService(IAttendanceRepository attendanceRepo)
        {
            _attendanceRepo = attendanceRepo;
        }

        /// <summary>
        /// يسجل حالة حضور عامل في يوم معين. لو فيه سجل موجود لنفس
        /// العامل ونفس اليوم، بيتم تحديثه بدل ما يتكرر (upsert).
        /// </summary>
        public async Task<Attendance> RecordAttendanceAsync(
            int workerId, DateTime date, AttendanceStatus status,
            TimeSpan? checkIn = null, TimeSpan? checkOut = null, string? notes = null)
        {
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
        /// حفظة واحدة. بيرجع عدد العمال اللي اتسجلوا/اتحدّثوا.
        /// </summary>
        public async Task<int> RecordAttendanceBatchAsync(
            DateTime date, IEnumerable<(int WorkerId, AttendanceStatus Status)> entries)
        {
            var entryList = entries.ToList();
            if (entryList.Count == 0) return 0;

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

        /// <summary>يبني ملخص حضور عامل معين خلال فترة زمنية (افتراضيًا آخر 30 يوم لو مفيش تاريخ محدد)</summary>
        public async Task<AttendanceSummaryDto> GetSummaryAsync(int workerId, DateTime from, DateTime to)
        {
            var records = await _attendanceRepo.GetByWorkerAndRangeAsync(workerId, from, to);

            return new AttendanceSummaryDto
            {
                TotalDaysTracked = records.Count,
                PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
                AbsentWithPermissionDays = records.Count(r => r.Status == AttendanceStatus.AbsentWithPermission),
                AbsentWithoutPermissionDays = records.Count(r => r.Status == AttendanceStatus.AbsentWithoutPermission)
            };
        }
    }
}
