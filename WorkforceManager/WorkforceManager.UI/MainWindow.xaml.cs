using System.Windows;
using System.Windows.Controls;

namespace WorkforceManager.UI
{
    /// <summary>
    /// النافذة الرئيسية: قائمة جانبية ثابتة + منطقة محتوى (MainContent)
    /// بتستبدل الـ View المعروض حسب الاختيار. الشاشات الفعلية
    /// (WorkersView, ProductsView, DailyEntryView, ReportsView)
    /// هتُبنى في مجلد Views كل واحدة على حدة في الخطوة الجاية.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // الشاشة الافتراضية عند فتح البرنامج
            MainContent.Content = new TextBlock
            {
                Text = "شاشة العمال والمهارات — قيد الإنشاء",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void NavWorkers_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MainContent.Content = App.AppHost.Services.GetRequiredService<WorkersView>();
        }

        private void NavProducts_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MainContent.Content = App.AppHost.Services.GetRequiredService<ProductsView>();
        }

        private void NavDailyEntry_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MainContent.Content = App.AppHost.Services.GetRequiredService<DailyEntryView>();
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MainContent.Content = App.AppHost.Services.GetRequiredService<ReportsView>();
        }
    }
}
