using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.UI.Views;

namespace WorkforceManager.UI.ViewModels
{
    /// <summary>
    /// عقل شاشة المنتجات والمراحل: قائمة المنتجات (مع بحث وإظهار
    /// الموقوف)، ولوحة تفاصيل بتعرض مراحل المنتج المحدد بكوتاتها،
    /// مع كل عمليات الإدارة: إضافة/تعديل/إيقاف منتج أو مرحلة.
    /// تعديل الكوتة بيسري على التسجيلات الجديدة فقط — القديم محمي
    /// بالـ Snapshot، والرسائل في الشاشة بتوضح ده للمستخدم.
    /// </summary>
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ProductsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // ------- حالة الشاشة -------

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showInactive;

        partial void OnShowInactiveChanged(bool value) => ApplyFilter();
        partial void OnSearchTextChanged(string value) => ApplyFilter();

        /// <summary>كل المنتجات المحمّلة من القاعدة (المصدر قبل الفلترة)</summary>
        private List<ProductRow> _allProducts = new();

        /// <summary>المنتجات المعروضة بعد البحث/الفلترة</summary>
        public ObservableCollection<ProductRow> Products { get; } = new();

        [ObservableProperty]
        private ProductRow? _selectedProduct;

        partial void OnSelectedProductChanged(ProductRow? value)
        {
            // تحديث لوحة المراحل فورًا عند تغيير المنتج المحدد
            Stages.Clear();
            if (value is null) return;
            foreach (var s in value.Stages.OrderBy(s => s.SortOrder))
                Stages.Add(s);
        }

        /// <summary>مراحل المنتج المحدد (مرتبة بترتيب خط الإنتاج)</summary>
        public ObservableCollection<StageRow> Stages { get; } = new();

        // ------- التحميل والفلترة -------

        [RelayCommand]
        public async Task LoadAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var products = await productRepo.GetAllWithStagesAsync();
            _allProducts = products.Select(p => new ProductRow
            {
                ProductId = p.Id,
                Name = p.Name,
                ProductCode = p.ProductCode ?? "—",
                Description = p.Description ?? "",
                IsActive = p.IsActive,
                Stages = p.Stages.Select(s => new StageRow
                {
                    StageId = s.Id,
                    StageName = s.StageName,
                    PiecesPerWorkday = s.PiecesPerWorkday,
                    SortOrder = s.SortOrder,
                    IsActive = s.IsActive
                }).ToList()
            }).ToList();

            ApplyFilter();
        }

        /// <summary>تطبيق البحث وفلتر الموقوف على القائمة المحمّلة (في الذاكرة — عدد المنتجات صغير)</summary>
        private void ApplyFilter()
        {
            var query = SearchText.Trim();
            var selectedId = SelectedProduct?.ProductId;

            var filtered = _allProducts
                .Where(p => ShowInactive || p.IsActive)
                .Where(p => query.Length == 0
                    || p.Name.Contains(query)
                    || p.ProductCode.Contains(query)
                    || p.Stages.Any(s => s.StageName.Contains(query)))
                .ToList();

            Products.Clear();
            foreach (var p in filtered) Products.Add(p);

            // الحفاظ على الاختيار الحالي لو لسه موجود بعد الفلترة
            SelectedProduct = Products.FirstOrDefault(p => p.ProductId == selectedId)
                ?? Products.FirstOrDefault();
        }

        // ------- إدارة المنتجات -------

        [RelayCommand]
        private async Task AddProductAsync()
        {
            var dialog = new ProductEditDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();
                var created = await mgmt.CreateProductAsync(dialog.ProductName, dialog.ProductCode, dialog.ProductDescription);
                await LoadAsync();
                // اختيار المنتج الجديد فورًا عشان المستخدم يبدأ يضيف مراحله
                SelectedProduct = Products.FirstOrDefault(p => p.ProductId == created.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في إضافة المنتج", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task EditProductAsync()
        {
            if (SelectedProduct is null) return;

            var dialog = new ProductEditDialog { Owner = Application.Current.MainWindow, Title = "تعديل منتج" };
            dialog.LoadProduct(SelectedProduct.Name,
                SelectedProduct.ProductCode == "—" ? null : SelectedProduct.ProductCode,
                SelectedProduct.Description);
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();
                await mgmt.UpdateProductAsync(SelectedProduct.ProductId,
                    dialog.ProductName, dialog.ProductCode, dialog.ProductDescription);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في تعديل المنتج", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task ToggleProductActiveAsync()
        {
            if (SelectedProduct is null) return;

            var isDeactivating = SelectedProduct.IsActive;
            var message = isDeactivating
                ? $"إيقاف المنتج \"{SelectedProduct.Name}\"؟\nهيختفي هو ومراحله من شاشة التسجيل، وكل السجلات التاريخية هتفضل محفوظة."
                : $"إعادة تفعيل المنتج \"{SelectedProduct.Name}\"؟";

            if (MessageBox.Show(message, "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var scope = _scopeFactory.CreateScope();
            var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();

            if (isDeactivating)
                await mgmt.DeactivateProductAsync(SelectedProduct.ProductId);
            else
                await mgmt.ReactivateProductAsync(SelectedProduct.ProductId);

            await LoadAsync();
        }

        // ------- إدارة المراحل -------

        [RelayCommand]
        private async Task AddStageAsync()
        {
            if (SelectedProduct is null) return;

            var dialog = new StageEditDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();
                await mgmt.AddStageAsync(SelectedProduct.ProductId,
                    dialog.StageName, dialog.PiecesPerWorkday, dialog.SortOrder);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في إضافة المرحلة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task EditStageAsync(StageRow? stage)
        {
            if (stage is null) return;

            var dialog = new StageEditDialog { Owner = Application.Current.MainWindow, Title = "تعديل مرحلة" };
            dialog.LoadStage(stage.StageName, stage.PiecesPerWorkday, stage.SortOrder);
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();
                await mgmt.UpdateStageAsync(stage.StageId,
                    dialog.StageName, dialog.PiecesPerWorkday, dialog.SortOrder ?? stage.SortOrder);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في تعديل المرحلة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private async Task ToggleStageActiveAsync(StageRow? stage)
        {
            if (stage is null) return;

            var isDeactivating = stage.IsActive;
            var message = isDeactivating
                ? $"إيقاف مرحلة \"{stage.StageName}\"؟\nهتختفي من شاشة التسجيل وسجلاتها التاريخية هتفضل محفوظة."
                : $"إعادة تفعيل مرحلة \"{stage.StageName}\"؟";

            if (MessageBox.Show(message, "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var scope = _scopeFactory.CreateScope();
            var mgmt = scope.ServiceProvider.GetRequiredService<ProductManagementService>();

            if (isDeactivating)
                await mgmt.DeactivateStageAsync(stage.StageId);
            else
                await mgmt.ReactivateStageAsync(stage.StageId);

            await LoadAsync();
        }
    }

    // ------- نماذج العرض الخاصة بالشاشة -------

    /// <summary>منتج واحد في قائمة الشاشة، بمراحله المحمّلة معاه</summary>
    public class ProductRow
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = "";
        public string ProductCode { get; init; } = "";
        public string Description { get; init; } = "";
        public bool IsActive { get; init; }
        public List<StageRow> Stages { get; init; } = new();

        public string StatusText => IsActive ? "نشط" : "موقوف";
        public int ActiveStagesCount => Stages.Count(s => s.IsActive);
        public string StagesCountText => $"{ActiveStagesCount} مرحلة";
    }

    /// <summary>مرحلة واحدة في جدول مراحل المنتج المحدد</summary>
    public class StageRow
    {
        public int StageId { get; init; }
        public string StageName { get; init; } = "";
        public int PiecesPerWorkday { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }

        public string StatusText => IsActive ? "نشطة" : "موقوفة";
    }
}
