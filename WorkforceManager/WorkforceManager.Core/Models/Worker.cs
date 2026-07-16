using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkforceManager.Core.Models
{
    /// <summary>
    /// يمثل عامل في القسم.
    /// كل عامل مرتبط بمجموعة من المهارات (WorkerSkills) توضح المراحل
    /// التي يستطيع تنفيذها، ومرتبط بسجلات الإنتاج اليومي الخاصة به.
    /// </summary>
    public class Worker
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العامل مطلوب")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>رقم كودي/وظيفي اختياري لتسهيل البحث والفرز (اختياري)</summary>
        [MaxLength(30)]
        public string? EmployeeCode { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// وصف حر لمهارات العامل (منسوخ من الملاحظات الأصلية عند
        /// الاستيراد). ده مش بديل عن الربط الدقيق بالمراحل (WorkerSkill)،
        /// لكنه مرجع نصي سريع لحد ما مدير القسم يحدد المهارات
        /// الدقيقة لكل عامل من داخل البرنامج.
        /// </summary>
        [MaxLength(1000)]
        public string? SkillsNotes { get; set; }

        /// <summary>تاريخ التحاق العامل بالقسم</summary>
        public DateTime? HireDate { get; set; }

        /// <summary>
        /// يسمح بإلغاء تفعيل عامل (ترك العمل) دون حذف بياناته وسجل إنتاجه القديم.
        /// أفضل من الحذف الفعلي (Soft Delete) للحفاظ على سلامة السجلات التاريخية.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ------- العلاقات (Navigation Properties) -------

        /// <summary>كل المهارات (المراحل) التي يجيد هذا العامل تنفيذها</summary>
        public virtual ICollection<WorkerSkill> Skills { get; set; } = new List<WorkerSkill>();

        /// <summary>كل سجلات الإنتاج اليومي المسجّلة لهذا العامل عبر الزمن</summary>
        public virtual ICollection<DailyProduction> ProductionRecords { get; set; } = new List<DailyProduction>();

        /// <summary>كل سجلات الحضور والغياب الخاصة بهذا العامل عبر الزمن</summary>
        public virtual ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();

        /// <summary>كل الجزاءات المسجّلة على هذا العامل عبر الزمن (بتتخصم من يومياته الأسبوعية)</summary>
        public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();

        [NotMapped]
        public string DisplayName => string.IsNullOrWhiteSpace(EmployeeCode)
            ? FullName
            : $"{FullName} ({EmployeeCode})";
    }
}
