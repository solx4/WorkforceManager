using System.Windows;
using System.Windows.Controls;
using WorkforceManager.UI.ViewModels;

namespace WorkforceManager.UI.Views
{
    /// <summary>
    /// نافذة معاينة وطباعة قسيمة أجر عامل. بتعرض القسيمة على الشاشة
    /// (معاينة) وبتطبعها على أي طابعة أو "Microsoft Print to PDF" —
    /// من غير أي مكتبة خارجية.
    /// </summary>
    public partial class PayslipWindow : Window
    {
        public PayslipWindow(PayslipData data)
        {
            InitializeComponent();
            DataContext = data;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            // نطبع منطقة القسيمة نفسها فقط (من غير شريط الأدوات).
            // بنكبّرها لعرض الصفحة المطبوعة مع هوامش بسيطة، ونركّزها أفقيًا.
            var element = PrintArea;
            var margin = 40.0;
            var printableWidth = dialog.PrintableAreaWidth - margin * 2;

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var contentWidth = element.DesiredSize.Width;
            var scale = contentWidth > 0 ? System.Math.Min(1.0, printableWidth / contentWidth) : 1.0;

            var original = element.LayoutTransform;
            element.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
            try
            {
                dialog.PrintVisual(element, "قسيمة أجر");
            }
            finally
            {
                // نرجّع العرض لحالته الطبيعية بعد الطباعة
                element.LayoutTransform = original;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
