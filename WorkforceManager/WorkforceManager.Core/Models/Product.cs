using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkforceManager.Core.Models
{
    /// <summary>
    /// يمثل منتج داخل القسم (مثال: قميص رجالي، حقيبة، ... إلخ).
    /// كل منتج له مجموعة مراحل تصنيع خاصة به (ProductionStage)،
    /// وكل مرحلة داخل هذا المنتج لها سعر مستقل حتى لو تكرر اسم
    /// نفس المرحلة في منتج آخر بسعر مختلف.
    /// </summary>
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>كود اختياري للمنتج لتسهيل الفرز والتقارير</summary>
        [MaxLength(30)]
        public string? ProductCode { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// يسمح بإخفاء منتج توقف إنتاجه دون حذف بياناته التاريخية
        /// أو المراحل والأسعار المرتبطة به.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ------- العلاقات -------

        /// <summary>كل مراحل التصنيع الخاصة بهذا المنتج تحديدًا (بأسعارها المستقلة)</summary>
        public virtual ICollection<ProductionStage> Stages { get; set; } = new List<ProductionStage>();
    }
}
