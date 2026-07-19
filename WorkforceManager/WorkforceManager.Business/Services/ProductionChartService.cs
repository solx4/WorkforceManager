using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// بيانات الرسم البياني الأسبوعي للمنتجات: كام قطعة مكتملة اتنتجت
    /// من كل منتج في كل أسبوع خلال فترة معينة.
    ///
    /// "المكتملة" = القطع المسجلة على آخر مرحلة في خط إنتاج المنتج
    /// (أعلى ترتيب بين مراحله النشطة) — دي القطع اللي خرجت من الخط
    /// فعلاً. القطع المسجلة على المراحل الأبكر شغل جارٍ مش إنتاج مكتمل،
    /// وجمعها كان هيعد نفس القطعة أكتر من مرة.
    /// </summary>
    public class ProductionChartService
    {
        private readonly IDailyProductionRepository _productionRepo;
        private readonly IProductRepository _productRepo;

        public ProductionChartService(
            IDailyProductionRepository productionRepo,
            IProductRepository productRepo)
        {
            _productionRepo = productionRepo;
            _productRepo = productRepo;
        }

        /// <summary>
        /// يبني نقاط الرسم: (أسبوع، منتج، قطع مكتملة) لكل منتج اتشتغل عليه
        /// خلال الفترة. الأسابيع بتتحدد بحدود أسبوع العمل (خميس → أربع)،
        /// والأسابيع اللي مفيهاش إنتاج مكتمل لمنتج مش بترجع نقطة ليه
        /// (الشاشة بتكمّل الأصفار للعرض).
        /// </summary>
        public async Task<List<ProductWeeklyPointDto>> GetProductWeeklyCompletedAsync(DateTime from, DateTime to)
        {
            // حدود الفترة مظبوطة على أسابيع عمل كاملة
            var (rangeStart, _) = WeeklySummaryService.GetWorkWeekRange(from);
            var (_, rangeEnd) = WeeklySummaryService.GetWorkWeekRange(to);

            // آخر مرحلة لكل منتج (أعلى ترتيب بين مراحله النشطة — ولو كلها
            // موقوفة بناخد أعلى ترتيب بين الكل عشان المنتجات القديمة تفضل تتحسب)
            var products = await _productRepo.GetAllWithStagesAsync();
            var lastStageByProduct = products
                .Where(p => p.Stages.Count > 0)
                .ToDictionary(
                    p => p.Id,
                    p => (p.Stages.Any(s => s.IsActive) ? p.Stages.Where(s => s.IsActive) : p.Stages)
                        .OrderByDescending(s => s.SortOrder).ThenByDescending(s => s.Id)
                        .First().Id);

            var records = await _productionRepo.GetByRangeAsync(rangeStart, rangeEnd);

            // القطع المكتملة بس: السجلات اللي على آخر مرحلة لمنتجها
            return records
                .Where(r => lastStageByProduct.TryGetValue(r.ProductionStage.ProductId, out var lastStageId)
                            && r.ProductionStageId == lastStageId)
                .GroupBy(r => (WeekStart: WeeklySummaryService.GetWorkWeekRange(r.Date).WeekStart,
                               r.ProductionStage.ProductId))
                .Select(g => new ProductWeeklyPointDto
                {
                    WeekStart = g.Key.WeekStart,
                    WeekEnd = g.Key.WeekStart.AddDays(6),
                    ProductId = g.Key.ProductId,
                    ProductName = g.First().ProductionStage.Product.Name,
                    CompletedPieces = g.Sum(r => r.PieceCount)
                })
                .OrderBy(p => p.WeekStart).ThenByDescending(p => p.CompletedPieces)
                .ToList();
        }
    }
}
