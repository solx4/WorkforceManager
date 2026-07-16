using System.Windows.Controls;
using WorkforceManager.UI.ViewModels;

namespace WorkforceManager.UI.Views
{
    /// <summary>
    /// شاشة التسجيل اليومي: الكود هنا شكلي بس (ربط الـ ViewModel) —
    /// كل المنطق في DailyEntryViewModel حسب نمط MVVM المتبع في المشروع.
    /// </summary>
    public partial class DailyEntryView : UserControl
    {
        public DailyEntryView(DailyEntryViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // تحميل المنتجات والعمال أول ما الشاشة تظهر
            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }
    }
}
