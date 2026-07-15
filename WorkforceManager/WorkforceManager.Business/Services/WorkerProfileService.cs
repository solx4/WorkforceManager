using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤول عن بناء "الملف الكامل" لعامل واحد — النتيجة اللي المفروض
    /// تظهر فورًا لحظة ما مدير القسم يدور على اسم عامل: مهاراته
    /// (بيعرف يعمل إيه)، وملخص حضوره وغيابه.
    /// </summary>
    public class WorkerProfileService
    {
        private readonly IWorkerRepository _workerRepo;
        private readonly AttendanceService _attendanceService;

        public WorkerProfileService(IWorkerRepository workerRepo, AttendanceService attendanceService)
        {
            _workerRepo = workerRepo;
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// يبحث عن عامل بالاسم ويرجع ملف كامل لكل نتيجة مطابقة: المهارات
        /// + ملخص الحضور لآخر 30 يوم (المدة الافتراضية المعروضة في شاشة البحث).
        /// </summary>
        public async Task<List<WorkerProfileDto>> SearchProfilesByNameAsync(string nameQuery, int attendanceWindowDays = 30)
        {
            var workers = await _workerRepo.SearchByNameAsync(nameQuery);
            var profiles = new List<WorkerProfileDto>();

            var to = DateTime.Today;
            var from = to.AddDays(-attendanceWindowDays);

            foreach (var worker in workers)
            {
                var attendanceSummary = await _attendanceService.GetSummaryAsync(worker.Id, from, to);

                profiles.Add(new WorkerProfileDto
                {
                    WorkerId = worker.Id,
                    FullName = worker.FullName,
                    EmployeeCode = worker.EmployeeCode,
                    IsActive = worker.IsActive,
                    HireDate = worker.HireDate,
                    Skills = worker.Skills
                        .Select(s => $"{s.ProductionStage.StageName} — {s.ProductionStage.Product.Name}")
                        .ToList(),
                    Attendance = attendanceSummary
                });
            }

            return profiles;
        }
    }
}
