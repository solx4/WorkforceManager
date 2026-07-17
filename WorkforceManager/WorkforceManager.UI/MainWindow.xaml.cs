using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.UI.Views;

namespace WorkforceManager.UI
{
    /// <summary>
    /// النافذة الرئيسية: قائمة جانبية ثابتة + منطقة محتوى (MainContent)
    /// بتستبدل الـ View المعروض حسب الاختيار. كل شاشة بتتحل من الـ DI
    /// عشان تاخد كل خدماتها جاهزة من غير new يدوي.
    /// التنقل بيشتغل بحدث Checked: أول Checked لزرار "العمال" بيحصل
    /// أثناء InitializeComponent قبل ما MainContent يتبني — الحارس
    /// (null check) بيتخطاه، والـ Constructor بيحمّل الشاشة الافتراضية بعدها.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // الشاشة الافتراضية عند فتح البرنامج: شاشة العمال
            MainContent.Content = App.AppHost.Services.GetRequiredService<WorkersView>();
        }

        private void NavWorkers_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent is null) return; // بيحصل مرة واحدة أثناء تهيئة النافذة
            MainContent.Content = App.AppHost.Services.GetRequiredService<WorkersView>();
        }

        private void NavProducts_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent is null) return;
            MainContent.Content = App.AppHost.Services.GetRequiredService<ProductsView>();
        }

        private void NavDailyEntry_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent is null) return;
            MainContent.Content = App.AppHost.Services.GetRequiredService<DailyEntryView>();
        }

        private void NavReports_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent is null) return;
            MainContent.Content = App.AppHost.Services.GetRequiredService<ReportsView>();
        }
    }
}
