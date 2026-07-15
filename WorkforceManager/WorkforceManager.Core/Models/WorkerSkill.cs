using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkforceManager.Core.Enums;

namespace WorkforceManager.Core.Models
{
    /// <summary>
    /// جدول ربط (Many-to-Many) بين العامل والمرحلة: يوضح "هذا العامل
    /// يجيد تنفيذ هذه المرحلة تحديدًا في هذا المنتج". هذا هو الأساس
    /// الذي تُبنى عليه ميزة "ابحث عن اسم عامل واعرف بيعرف يعمل إيه".
    /// </summary>
    public class WorkerSkill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Worker))]
        public int WorkerId { get; set; }

        [Required]
        [ForeignKey(nameof(ProductionStage))]
        public int ProductionStageId { get; set; }

        /// <summary>مستوى إتقان اختياري (مبتدئ / متوسط / محترف) — قابل للاستخدام مستقبلاً في التقييم</summary>
        public SkillLevel Level { get; set; } = SkillLevel.Proficient;

        // ------- العلاقات -------

        public virtual Worker Worker { get; set; } = null!;
        public virtual ProductionStage ProductionStage { get; set; } = null!;
    }
}
