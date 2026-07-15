using WorkforceManager.Core.Enums;

namespace WorkforceManager.Business.DTOs
{
    /// <summary>
    /// ملخص أداء عامل واحد في يوم واحد — الشكل النهائي اللي هيتعرض
    /// في شاشة "تقييم اليوم" وفي التقارير، بعد ما يتم تجميع كل
    /// سجلات DailyProduction الخاصة بيه وحساب عدد اليوميات المنجزة
    /// والمقارنة بالمتوسط، بالإضافة لحالة حضوره في نفس اليوم
    /// (بيؤثر على التقييم النهائي).
    /// </summary>
    public class WorkerDailySummaryDto
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        /// <summary>حالة حضور العامل في هذا اليوم (لو مسجّلة)</summary>
        public AttendanceStatus? AttendanceStatus { get; set; }

        /// <summary>إجمالي عدد القطع المنجزة في كل المراحل خلال اليوم</summary>
        public int TotalPieces { get; set; }

        /// <summary>إجمالي عدد اليوميات المنجزة في هذا اليوم (مجموع كل المراحل)</summary>
        public decimal TotalWorkdays { get; set; }

        /// <summary>متوسط عدد يوميات باقي زملائه في نفس اليوم (لغرض المقارنة)</summary>
        public decimal TeamAverageWorkdays { get; set; }

        /// <summary>الفرق بالنسبة المئوية عن المتوسط: موجب = أعلى من زملائه</summary>
        public double PercentVsAverage =>
            TeamAverageWorkdays == 0 ? 0 : (double)((TotalWorkdays - TeamAverageWorkdays) / TeamAverageWorkdays) * 100;

        public PerformanceRating Rating { get; set; }

        /// <summary>تفاصيل الإنتاج مقسّمة حسب كل منتج/مرحلة عمل عليها العامل هذا اليوم</summary>
        public List<StageBreakdownDto> Breakdown { get; set; } = new();
    }

    public class StageBreakdownDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public int PieceCount { get; set; }
        public int PiecesPerWorkday { get; set; }

        /// <summary>عدد اليوميات المنجزة في هذه المرحلة تحديدًا</summary>
        public decimal Workdays => PiecesPerWorkday == 0 ? 0 : Math.Round((decimal)PieceCount / PiecesPerWorkday, 2);
    }

    public enum PerformanceRating
    {
        UnexcusedAbsence, // غياب بدون إذن — أسوأ تصنيف بغض النظر عن أي شيء آخر
        BelowAverage,      // أقل من المتوسط بشكل ملحوظ
        Average,           // قريب من المتوسط
        AboveAverage,      // أعلى من المتوسط
        TopPerformer       // الأعلى إنتاجية في اليوم
    }
}
