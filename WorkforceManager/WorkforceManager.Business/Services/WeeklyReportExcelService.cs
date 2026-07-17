using ClosedXML.Excel;
using WorkforceManager.Business.DTOs;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// تصدير كشف الأسبوع لملف Excel حقيقي (xlsx) منسّق وجاهز للطباعة
    /// أو الإرسال — نفس الكشف اللي المدير كان بيعمله يدويًا في الإكسل،
    /// لكن هنا الأرقام محسوبة من النظام والإكسل بقى مجرد مخرج نهائي.
    /// الخدمة بتاخد البيانات جاهزة ومش بتلمس قاعدة البيانات — كل
    /// الحسابات مسؤولية WeeklySummaryService.
    /// </summary>
    public class WeeklyReportExcelService
    {
        // ألوان الكشف (متناسقة مع ألوان البرنامج)
        private static readonly XLColor HeaderColor = XLColor.FromHtml("#1F3864");
        private static readonly XLColor BestRowColor = XLColor.FromHtml("#FFF6D9");
        private static readonly XLColor StripeColor = XLColor.FromHtml("#F2F4F8");

        /// <summary>
        /// يبني ملف الكشف ويحفظه في المسار المطلوب. بيرمي استثناء واضح
        /// لو القائمة فاضية بدل ما يطلع ملف فاضي مضلل.
        /// </summary>
        public void ExportWeeklySummary(IReadOnlyList<WorkerWeeklySummaryDto> summaries, string filePath)
        {
            if (summaries.Count == 0)
                throw new InvalidOperationException("لا توجد بيانات في هذا الأسبوع للتصدير");

            var weekStart = summaries[0].WeekStart;
            var weekEnd = summaries[0].WeekEnd;

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("كشف الأسبوع");
            sheet.RightToLeft = true; // الشيت كله يمين-لشمال زي البرنامج

            // ---------- صف العنوان (مدموج فوق الجدول) ----------
            sheet.Range(1, 1, 1, 12).Merge();
            var title = sheet.Cell(1, 1);
            title.Value = $"كشف أسبوع العمل: من الخميس {weekStart:yyyy/MM/dd} إلى الأربعاء {weekEnd:yyyy/MM/dd}";
            title.Style.Font.SetBold().Font.SetFontSize(14);
            title.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Row(1).Height = 26;

            // ---------- صف العناوين ----------
            string[] headers =
            {
                "الترتيب", "اسم العامل", "الكود",
                "اليوميات المنتجة", "إجمالي القطع",
                "حضور", "غياب بإذن", "غياب بدون إذن",
                "خصم الغياب", "خصم الجزاءات", "صافي اليوميات", "تفاصيل الجزاءات"
            };
            for (var c = 0; c < headers.Length; c++)
            {
                var cell = sheet.Cell(2, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
                cell.Style.Fill.SetBackgroundColor(HeaderColor);
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            sheet.Row(2).Height = 22;

            // ---------- صفوف العمال (مرتبين بالصافي زي ما جايين من الخدمة) ----------
            for (var i = 0; i < summaries.Count; i++)
            {
                var s = summaries[i];
                var row = i + 3; // البيانات بتبدأ من الصف الثالث (بعد العنوان والهيدر)

                sheet.Cell(row, 1).Value = i + 1;
                sheet.Cell(row, 2).Value = s.IsBestWorkerOfWeek ? $"⭐ {s.WorkerName}" : s.WorkerName;
                sheet.Cell(row, 3).Value = s.EmployeeCode ?? "—";
                sheet.Cell(row, 4).Value = s.ProducedWorkdays;
                sheet.Cell(row, 5).Value = s.TotalPieces;
                sheet.Cell(row, 6).Value = s.PresentDays;
                sheet.Cell(row, 7).Value = s.AbsentWithPermissionDays;
                sheet.Cell(row, 8).Value = s.AbsentWithoutPermissionDays;
                sheet.Cell(row, 9).Value = s.AbsenceDeduction;
                sheet.Cell(row, 10).Value = s.PenaltyDeduction;
                sheet.Cell(row, 11).Value = s.NetWorkdays;
                sheet.Cell(row, 12).Value = string.Join("، ",
                    s.Penalties.Select(p => $"{p.Reason} ({p.DeductionName})"));

                // الصافي بالخط العريض، وبالأحمر لو سالب (عامل عليه خصومات أكتر من إنتاجه)
                var netCell = sheet.Cell(row, 11);
                netCell.Style.Font.SetBold();
                if (s.NetWorkdays < 0)
                    netCell.Style.Font.SetFontColor(XLColor.Red);

                // تظليل: أحسن عامل أصفر فاتح، والصفوف الزوجية رمادي خفيف (سهولة القراءة)
                if (s.IsBestWorkerOfWeek)
                    sheet.Range(row, 1, row, 12).Style.Fill.SetBackgroundColor(BestRowColor);
                else if (i % 2 == 1)
                    sheet.Range(row, 1, row, 12).Style.Fill.SetBackgroundColor(StripeColor);
            }

            // ---------- تنسيق عام ----------
            var lastRow = summaries.Count + 2;
            var table = sheet.Range(2, 1, lastRow, 12);
            table.Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
            table.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            table.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            // الأعمدة الرقمية في النص، والأسماء والجزاءات على اليمين
            sheet.Range(3, 1, lastRow, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            sheet.Range(3, 3, lastRow, 11).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            sheet.SheetView.FreezeRows(2); // العنوان والهيدر ثابتين مع التمرير
            sheet.Columns().AdjustToContents();
            sheet.Column(2).Width = Math.Max(sheet.Column(2).Width, 28);  // عمود الاسم مش بيتزنق
            sheet.Column(12).Width = Math.Max(sheet.Column(12).Width, 30); // عمود الجزاءات كذلك

            workbook.SaveAs(filePath);
        }
    }
}
