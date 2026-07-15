using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using WorkforceManager.Core.Enums;

namespace WorkforceManager.Core.Models
{
    /// <summary>
    /// سجل حضور يومي واحد لعامل: حاضر / غائب بإذن / غائب بدون إذن،
    /// مع وقت الحضور والانصراف الفعلي (لو حاضر). هذا النموذج مستقل
    /// عن DailyProduction لأن عامل ممكن يكون حاضر لكن من غير إنتاج
    /// مسجل (يوم تدريب مثلاً)، أو العكس مش وارد أصلاً (غايب يبقى
    /// معندوش إنتاج تلقائيًا).
    /// </summary>
    [Index(nameof(WorkerId), nameof(Date), IsUnique = true)] // يوم واحد بالظبط لكل عامل، منع تكرار التسجيل
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Worker))]
        public int WorkerId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        /// <summary>وقت الحضور الفعلي (يُملأ فقط لو الحالة "حاضر")</summary>
        public TimeSpan? CheckInTime { get; set; }

        /// <summary>وقت الانصراف الفعلي (يُملأ فقط لو الحالة "حاضر")</summary>
        public TimeSpan? CheckOutTime { get; set; }

        /// <summary>سبب الغياب أو أي ملاحظة (اختياري)</summary>
        [MaxLength(300)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ------- العلاقات -------

        public virtual Worker Worker { get; set; } = null!;

        [NotMapped]
        public bool IsAbsence => Status != AttendanceStatus.Present;
    }
}
