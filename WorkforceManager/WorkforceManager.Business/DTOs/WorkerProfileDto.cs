namespace WorkforceManager.Business.DTOs
{
    /// <summary>ملخص حضور عامل خلال فترة زمنية معينة</summary>
    public class AttendanceSummaryDto
    {
        public int TotalDaysTracked { get; set; }
        public int PresentDays { get; set; }
        public int AbsentWithPermissionDays { get; set; }
        public int AbsentWithoutPermissionDays { get; set; }

        /// <summary>نسبة الحضور = أيام الحضور ÷ إجمالي الأيام المسجّلة × 100</summary>
        public double AttendanceRate =>
            TotalDaysTracked == 0 ? 100 : Math.Round((double)PresentDays / TotalDaysTracked * 100, 1);
    }

    /// <summary>
    /// الملف الكامل لعامل — هذا هو اللي بيظهر لحظة ما مدير القسم يدور
    /// باسم العامل: مين هو، بيعرف يعمل إيه (المهارات)، وسجل حضوره وغيابه.
    /// </summary>
    public class WorkerProfileDto
    {
        public int WorkerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? HireDate { get; set; }

        /// <summary>المراحل/المهارات التي يجيد العامل تنفيذها (اسم المرحلة + اسم المنتج)</summary>
        public List<string> Skills { get; set; } = new();

        public AttendanceSummaryDto Attendance { get; set; } = new();
    }
}
