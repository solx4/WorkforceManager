namespace WorkforceManager.Business.DTOs
{
    /// <summary>
    /// نقطة واحدة في الرسم البياني الأسبوعي للمنتجات: منتج معين في أسبوع
    /// معين، وكام قطعة "مكتملة" اتنتجت منه — المكتملة = المسجلة على آخر
    /// مرحلة في خط إنتاج المنتج (قرار متفق عليه: مجموع قطع كل المراحل
    /// بيضلل لأنه بيعد نفس القطعة أكتر من مرة وهي بتعدي على المراحل).
    /// </summary>
    public class ProductWeeklyPointDto
    {
        /// <summary>بداية الأسبوع (الخميس)</summary>
        public DateTime WeekStart { get; init; }

        /// <summary>نهاية الأسبوع (الأربع)</summary>
        public DateTime WeekEnd { get; init; }

        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;

        /// <summary>القطع المسجلة على آخر مرحلة للمنتج خلال الأسبوع</summary>
        public int CompletedPieces { get; init; }
    }
}
