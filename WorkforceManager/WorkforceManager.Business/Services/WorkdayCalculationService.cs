using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤول عن عملية واحدة بس وبيعملها صح: تسجيل قطع منتجة لعامل
    /// في مرحلة معينة، وحساب عدد "اليوميات" الناتجة عنها تلقائيًا.
    /// أي منطق حسابي متعلق باليوميات لازم يمر من هنا، مش يتكتب في
    /// الواجهة (UI) مباشرة.
    /// </summary>
    public class WorkdayCalculationService
    {
        private readonly IDailyProductionRepository _productionRepo;
        private readonly IGenericRepository<ProductionStage> _stageRepo;

        public WorkdayCalculationService(
            IDailyProductionRepository productionRepo,
            IGenericRepository<ProductionStage> stageRepo)
        {
            _productionRepo = productionRepo;
            _stageRepo = stageRepo;
        }

        /// <summary>
        /// يسجل إنتاج عامل في مرحلة معينة، ويحسب عدد اليوميات المنجزة
        /// تلقائيًا بناءً على كوتة اليومية الحالية للمرحلة، مع حفظ
        /// نسخة (Snapshot) من الكوتة وقت التسجيل حماية للسجل من أي
        /// تعديل لاحق للكوتة.
        /// </summary>
        public async Task<DailyProduction> RecordProductionAsync(
            int workerId, int productionStageId, int pieceCount, DateTime date, string? notes = null)
        {
            if (pieceCount <= 0)
                throw new ArgumentException("عدد القطع يجب أن يكون أكبر من صفر", nameof(pieceCount));

            var stage = await _stageRepo.GetByIdAsync(productionStageId)
                ?? throw new InvalidOperationException("المرحلة المحددة غير موجودة");

            var record = new DailyProduction
            {
                WorkerId = workerId,
                ProductionStageId = productionStageId,
                Date = date.Date,
                PieceCount = pieceCount,
                PiecesPerWorkdayAtEntry = stage.PiecesPerWorkday, // Snapshot الكوتة وقت التسجيل
                Notes = notes
            };

            await _productionRepo.AddAsync(record);
            await _productionRepo.SaveChangesAsync();

            return record;
        }

        /// <summary>إجمالي عدد اليوميات المنجزة لعامل معين في تاريخ معين (مجموع كل المراحل التي عمل عليها)</summary>
        public async Task<decimal> GetDailyWorkdaysAsync(int workerId, DateTime date)
        {
            var records = await _productionRepo.GetByDateAsync(date);
            return records.Where(r => r.WorkerId == workerId).Sum(r => r.WorkdaysCompleted);
        }
    }
}
