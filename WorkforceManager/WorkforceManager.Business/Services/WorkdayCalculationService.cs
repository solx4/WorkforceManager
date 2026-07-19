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

        /// <summary>
        /// يسجل إنتاج مجموعة عمال على نفس المرحلة في نفس اليوم دفعة واحدة —
        /// أساس شاشة الإدخال السريع: بدل ما المدير يسجل عامل عامل، بيدخل
        /// أرقام الكل ويحفظ مرة واحدة (حفظة واحدة على قاعدة البيانات).
        /// </summary>
        public async Task<int> RecordProductionBatchAsync(
            int productionStageId, DateTime date,
            IEnumerable<(int WorkerId, int PieceCount)> entries, string? notes = null)
        {
            var stage = await _stageRepo.GetByIdAsync(productionStageId)
                ?? throw new InvalidOperationException("المرحلة المحددة غير موجودة");

            var count = 0;
            foreach (var (workerId, pieceCount) in entries)
            {
                // القطع الصفرية/السالبة بتتتخطى بصمت — معناها العامل ده مشتغلش على المرحلة دي
                if (pieceCount <= 0) continue;

                await _productionRepo.AddAsync(new DailyProduction
                {
                    WorkerId = workerId,
                    ProductionStageId = productionStageId,
                    Date = date.Date,
                    PieceCount = pieceCount,
                    PiecesPerWorkdayAtEntry = stage.PiecesPerWorkday, // نفس الـ Snapshot بتاع التسجيل الفردي
                    Notes = notes
                });
                count++;
            }

            if (count > 0)
                await _productionRepo.SaveChangesAsync();

            return count;
        }

        /// <summary>
        /// يصحّح عدد قطع سجل إنتاج اتحفظ بالغلط. الكوتة المحفوظة وقت
        /// التسجيل (Snapshot) بتفضل زي ما هي — التصحيح للقطع بس،
        /// واليوميات بتتعاد حسابها تلقائيًا (خاصية محسوبة).
        /// </summary>
        public async Task<DailyProduction> UpdateProductionAsync(int recordId, int newPieceCount)
        {
            if (newPieceCount <= 0)
                throw new ArgumentException("عدد القطع يجب أن يكون أكبر من صفر", nameof(newPieceCount));

            var record = await _productionRepo.GetByIdAsync(recordId)
                ?? throw new InvalidOperationException("سجل الإنتاج غير موجود");

            record.PieceCount = newPieceCount;
            _productionRepo.Update(record);
            await _productionRepo.SaveChangesAsync();
            return record;
        }

        /// <summary>
        /// يحذف سجل إنتاج اتسجل بالغلط — حذف فعلي (نفس قاعدة الجزاءات:
        /// السجل الغلط ملوش قيمة تاريخية تستاهل الحفظ).
        /// </summary>
        public async Task DeleteProductionAsync(int recordId)
        {
            var record = await _productionRepo.GetByIdAsync(recordId)
                ?? throw new InvalidOperationException("سجل الإنتاج غير موجود");

            _productionRepo.Remove(record);
            await _productionRepo.SaveChangesAsync();
        }

        /// <summary>إجمالي عدد اليوميات المنجزة لعامل معين في تاريخ معين (مجموع كل المراحل التي عمل عليها)</summary>
        public async Task<decimal> GetDailyWorkdaysAsync(int workerId, DateTime date)
        {
            var records = await _productionRepo.GetByDateAsync(date);
            return records.Where(r => r.WorkerId == workerId).Sum(r => r.WorkdaysCompleted);
        }
    }
}
