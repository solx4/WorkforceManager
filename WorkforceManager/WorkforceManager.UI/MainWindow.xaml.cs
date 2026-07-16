using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.UI.Views;

namespace WorkforceManager.UI
{
    /// <summary>
    /// النافذة الرئيسية: قائمة جانبية ثابتة + منطقة محتوى (MainContent)
    /// بتستبدل الـ View المعروض حسب الاختيار. كل شاشة بتتحل من الـ DI
    /// عشان تاخد كل خدماتها جاهزة من غير new يدوي.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // الشاشة الافتراضية عند فتح البرنامج: شاشة العمال
            MainContent.Content = App.AppHost.Services.GetRequiredService<WorkersView>();
        }

        private void NavWorkers_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = App.AppHost.Services.GetRequiredService<WorkersView>();
        }

        private void NavProducts_Click(object sender, RoutedEventArgs e)
        {
            // TODO (مرحلة قادمة): شاشة المنتجات والمراحل
            MainContent.Content = Placeholder("شاشة المنتجات والمراحل — قيد الإنشاء");
        }

        private void NavDailyEntry_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = App.AppHost.Services.GetRequiredService<DailyEntryView>();
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            // TODO (مرحلة قادمة): شاشة التقارير والتقييم
            MainContent.Content = Placeholder("شاشة التقارير والتقييم — قيد الإنشاء");
        }

        /// <summary>نص مؤقت للشاشات اللي لسه ما اتبنتش</summary>
        private static TextBlock Placeholder(string text) => new()
        {
            Text = text,
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }
}
