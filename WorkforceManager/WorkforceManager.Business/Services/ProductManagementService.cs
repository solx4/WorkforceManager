using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤولة عن كل عمليات "الكتابة" على المنتجات ومراحلها: إضافة/تعديل
    /// منتج، إيقافه (Soft Delete)، وإدارة مراحله وكوتاتها.
    /// نقطة مهمة جدًا: تعديل كوتة مرحلة بيسري على التسجيلات الجديدة فقط —
    /// السجلات القديمة محمية بالـ Snapshot (PiecesPerWorkdayAtEntry)
    /// المتسجل وقت الإدخال، فالحسابات التاريخية عمرها ما بتتأثر.
    /// </summary>
    public class ProductManagementService
    {
        private readonly IProductRepository _productRepo;
        private readonly IGenericRepository<ProductionStage> _stageRepo;

        public ProductManagementService(
            IProductRepository productRepo,
            IGenericRepository<ProductionStage> stageRepo)
        {
            _productRepo = productRepo;
            _stageRepo = stageRepo;
        }

        // ======================= المنتجات =======================

        /// <summary>يضيف منتج جديد (الاسم إجباري)</summary>
        public async Task<Product> CreateProductAsync(string name, string? productCode = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج مطلوب", nameof(name));

            var product = new Product
            {
                Name = name.Trim(),
                ProductCode = string.IsNullOrWhiteSpace(productCode) ? null : productCode.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
            };

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();
            return product;
        }

        /// <summary>يعدّل بيانات منتج موجود</summary>
        public async Task<Product> UpdateProductAsync(int productId, string name, string? productCode = null, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج مطلوب", nameof(name));

            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException("المنتج المحدد غير موجود");

            product.Name = name.Trim();
            product.ProductCode = string.IsNullOrWhiteSpace(productCode) ? null : productCode.Trim();
            product.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync();
            return product;
        }

        /// <summary>
        /// إيقاف منتج (توقف إنتاجه): بيختفي هو ومراحله من شاشة التسجيل،
        /// وكل سجلاته التاريخية بتفضل محفوظة ومحسوبة في التقارير القديمة.
        /// </summary>
        public async Task DeactivateProductAsync(int productId)
        {
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException("المنتج المحدد غير موجود");

            product.IsActive = false;
            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync();
        }

        /// <summary>إعادة تفعيل منتج موقوف (رجع للإنتاج تاني)</summary>
        public async Task ReactivateProductAsync(int productId)
        {
            var product = await _productRepo.GetByIdAsync(productId)
                ?? throw new InvalidOperationException("المنتج المحدد غير موجود");

            product.IsActive = true;
            _productRepo.Update(product);
            await _productRepo.SaveChangesAsync();
        }

        // ======================= المراحل =======================

        /// <summary>
        /// يضيف مرحلة جديدة لمنتج بكوتة يوميتها. اسم المرحلة ميتكررش
        /// جوه نفس المنتج (لكن عادي يتكرر في منتجات تانية بكوتة مختلفة —
        /// دي قاعدة أساسية في النظام). لو الترتيب مش متحدد بياخد آخر ترتيب + 1.
        /// </summary>
        public async Task<ProductionStage> AddStageAsync(
            int productId, string stageName, int piecesPerWorkday, int? sortOrder = null)
        {
            if (string.IsNullOrWhiteSpace(stageName))
                throw new ArgumentException("اسم المرحلة مطلوب", nameof(stageName));
            if (piecesPerWorkday <= 0)
                throw new ArgumentException("كوتة اليومية يجب أن تكون رقمًا موجبًا أكبر من صفر", nameof(piecesPerWorkday));

            var product = await _productRepo.GetWithStagesAsync(productId)
                ?? throw new InvalidOperationException("المنتج المحدد غير موجود");

            var trimmedName = stageName.Trim();
            if (product.Stages.Any(s => s.StageName == trimmedName))
                throw new InvalidOperationException($"المرحلة \"{trimmedName}\" موجودة بالفعل في هذا المنتج");

            var stage = new ProductionStage
            {
                ProductId = productId,
                StageName = trimmedName,
                PiecesPerWorkday = piecesPerWorkday,
                SortOrder = sortOrder ?? (product.Stages.Count == 0 ? 1 : product.Stages.Max(s => s.SortOrder) + 1)
            };

            await _stageRepo.AddAsync(stage);
            await _stageRepo.SaveChangesAsync();
            return stage;
        }

        /// <summary>
        /// يعدّل مرحلة (الاسم / الكوتة / الترتيب). تغيير الكوتة بيسري على
        /// التسجيلات الجديدة فقط — القديم محمي بالـ Snapshot.
        /// </summary>
        public async Task<ProductionStage> UpdateStageAsync(
            int stageId, string stageName, int piecesPerWorkday, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(stageName))
                throw new ArgumentException("اسم المرحلة مطلوب", nameof(stageName));
            if (piecesPerWorkday <= 0)
                throw new ArgumentException("كوتة اليومية يجب أن تكون رقمًا موجبًا أكبر من صفر", nameof(piecesPerWorkday));

            var stage = await _stageRepo.GetByIdAsync(stageId)
                ?? throw new InvalidOperationException("المرحلة المحددة غير موجودة");

            // منع تكرار الاسم الجديد مع مرحلة تانية في نفس المنتج
            var trimmedName = stageName.Trim();
            var duplicate = (await _stageRepo.FindAsync(
                s => s.ProductId == stage.ProductId && s.Id != stageId && s.StageName == trimmedName))
                .Any();
            if (duplicate)
                throw new InvalidOperationException($"المرحلة \"{trimmedName}\" موجودة بالفعل في هذا المنتج");

            stage.StageName = trimmedName;
            stage.PiecesPerWorkday = piecesPerWorkday;
            stage.SortOrder = sortOrder;

            _stageRepo.Update(stage);
            await _stageRepo.SaveChangesAsync();
            return stage;
        }

        /// <summary>إيقاف مرحلة (بتختفي من شاشة التسجيل، وسجلاتها التاريخية محفوظة)</summary>
        public async Task DeactivateStageAsync(int stageId)
        {
            var stage = await _stageRepo.GetByIdAsync(stageId)
                ?? throw new InvalidOperationException("المرحلة المحددة غير موجودة");

            stage.IsActive = false;
            _stageRepo.Update(stage);
            await _stageRepo.SaveChangesAsync();
        }

        /// <summary>إعادة تفعيل مرحلة موقوفة</summary>
        public async Task ReactivateStageAsync(int stageId)
        {
            var stage = await _stageRepo.GetByIdAsync(stageId)
                ?? throw new InvalidOperationException("المرحلة المحددة غير موجودة");

            stage.IsActive = true;
            _stageRepo.Update(stage);
            await _stageRepo.SaveChangesAsync();
        }
    }
}
