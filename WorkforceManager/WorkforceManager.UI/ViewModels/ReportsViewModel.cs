using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using WorkforceManager.Business.DTOs;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Enums;

namespace WorkforceManager.UI.ViewModels
{
    /// <summary>
    /// عقل شاشة التقارير والتقييم، وفيها تبويبين:
    /// 1) تقييم اليوم: كل عامل مقارن بمتوسط زمايله اللي أنتجوا في نفس
    ///    اليوم، بتصنيف ملوّن (الأفضل / فوق المتوسط / متوسط / تحت
    ///    المتوسط / غياب بدون إذن)، مع تفاصيل إنتاجه وجزاءاته.
    /// 2) كشف الأسبوع: الترتيب النهائي بصافي اليوميات (بعد كل الخصومات)
    ///    مع تنقّل بين الأسابيع وتصدير الكشف لملف Excel منسّق.
    /// </summary>
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ReportsViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        /// <summary>أول تحميل للشاشة: تقرير النهارده + كشف الأسبوع الحالي + رسم المنتجات</summary>
        public async Task InitializeAsync()
        {
            await LoadDailyAsync();
            await LoadWeeklyAsync();
            await LoadChartAsync();
        }

        // ======================= تبويب تقييم اليوم =======================

        [ObservableProperty]
        private DateTime _dailyDate = DateTime.Today;

        partial void OnDailyDateChanged(DateTime value)
        {
            // تغيير اليوم بيعيد تحميل التقرير (وأي خطأ بيظهر مش بيضيع بصمت)
            SafeAsync.Run(LoadDailyAsync);
        }

        /// <summary>سطر ملخص فوق الجدول: متوسط الفريق وعدد المنتجين</summary>
        [ObservableProperty]
        private string _dailySummaryText = string.Empty;

        public ObservableCollection<DailyReportRow> DailyRows { get; } = new();

        private async Task LoadDailyAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var evaluationService = scope.ServiceProvider.GetRequiredService<PerformanceEvaluationService>();
            var penaltyService = scope.ServiceProvider.GetRequiredService<PenaltyService>();

            var evaluations = await evaluationService.EvaluateDayAsync(DailyDate);
            // جزاءات اليوم بتتضم للعرض (مش جزء من تقييم الأداء نفسه)
            var penaltiesByWorker = (await penaltyService.GetPenaltiesByDateAsync(DailyDate))
                .GroupBy(p => p.WorkerId)
                .ToDictionary(g => g.Key,
                    g => string.Join("، ", g.Select(p => $"{p.Reason} ({p.Deduction.ToArabicName()})")));

            DailyRows.Clear();
            foreach (var e in evaluations)
            {
                penaltiesByWorker.TryGetValue(e.WorkerId, out var penaltiesText);
                DailyRows.Add(DailyReportRow.From(e, penaltiesText ?? ""));
            }

            var producers = evaluations.Where(e => e.TotalPieces > 0).ToList();
            DailySummaryText = producers.Count == 0
                ? "لا يوجد إنتاج مسجّل في هذا اليوم"
                : $"عدد المنتجين: {producers.Count} عامل   |   متوسط الفريق: {producers[0].TeamAverageWorkdays:0.##} يومية";
        }

        // ======================= تبويب كشف الأسبوع =======================

        /// <summary>أي تاريخ داخل الأسبوع المعروض — التنقل بيتحرك بيه 7 أيام</summary>
        private DateTime _weekAnchor = DateTime.Today;

        [ObservableProperty]
        private string _weekTitle = string.Empty;

        /// <summary>هل الأسبوع المعروض هو الأسبوع الحالي؟ (بيظهر بجانب العنوان)</summary>
        [ObservableProperty]
        private string _weekBadge = string.Empty;

        public ObservableCollection<WeeklyReportRow> WeeklyRows { get; } = new();

        [ObservableProperty]
        private WeeklyReportRow? _selectedWeeklyRow;

        private async Task LoadWeeklyAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var weeklyService = scope.ServiceProvider.GetRequiredService<WeeklySummaryService>();

            var (weekStart, weekEnd) = WeeklySummaryService.GetWorkWeekRange(_weekAnchor);
            WeekTitle = $"من الخميس {weekStart:yyyy/MM/dd} إلى الأربعاء {weekEnd:yyyy/MM/dd}";
            var (currentStart, _) = WeeklySummaryService.GetWorkWeekRange(DateTime.Today);
            WeekBadge = weekStart == currentStart ? "(الأسبوع الحالي)" : "";

            var summaries = await weeklyService.GetTeamWeeklySummaryAsync(_weekAnchor);

            WeeklyRows.Clear();
            for (var i = 0; i < summaries.Count; i++)
                WeeklyRows.Add(WeeklyReportRow.From(summaries[i], rank: i + 1));

            SelectedWeeklyRow = WeeklyRows.FirstOrDefault();
        }

        [RelayCommand]
        private Task PreviousWeekAsync()
        {
            _weekAnchor = _weekAnchor.AddDays(-7);
            return LoadWeeklyAsync();
        }

        [RelayCommand]
        private Task NextWeekAsync()
        {
            _weekAnchor = _weekAnchor.AddDays(7);
            return LoadWeeklyAsync();
        }

        [RelayCommand]
        private Task CurrentWeekAsync()
        {
            _weekAnchor = DateTime.Today;
            return LoadWeeklyAsync();
        }

        // ======================= تبويب رسم إنتاج المنتجات =======================

        /// <summary>خيارات مدة الرسم (بالأسابيع، منتهية بالأسبوع الحالي)</summary>
        public List<int> ChartWeeksOptions { get; } = new() { 4, 8, 12, 24 };

        [ObservableProperty]
        private int _selectedChartWeeks = 8;

        partial void OnSelectedChartWeeksChanged(int value)
        {
            SafeAsync.Run(LoadChartAsync);
        }

        /// <summary>أعمدة الرسم مجمعة بالأسبوع (بالترتيب الزمني)</summary>
        public ObservableCollection<ChartWeekGroup> ChartWeekGroups { get; } = new();

        /// <summary>مفتاح الألوان: منتج → لون + إجمالي الفترة</summary>
        public ObservableCollection<ChartLegendItem> ChartLegend { get; } = new();

        [ObservableProperty]
        private bool _chartHasData;

        /// <summary>لوحة ألوان السلاسل — كل منتج بياخد لون ثابت طول الرسمة</summary>
        private static readonly string[] ChartPalette =
        {
            "#1F3864", "#0B6E4F", "#B7791F", "#7A3B8F",
            "#B00020", "#0F7B8A", "#C2563B", "#5B6B7C"
        };

        /// <summary>أقصى ارتفاع للعمود بالبكسل — الباقي بيتحسب نسبيًا عليه</summary>
        private const double MaxBarHeight = 190;

        private async Task LoadChartAsync()
        {
            List<ProductWeeklyPointDto> points;
            using (var scope = _scopeFactory.CreateScope())
            {
                var chartService = scope.ServiceProvider.GetRequiredService<ProductionChartService>();
                var to = DateTime.Today;
                var from = to.AddDays(-7 * (SelectedChartWeeks - 1));
                points = await chartService.GetProductWeeklyCompletedAsync(from, to);
            }

            // المنتجات اللي ليها إنتاج مكتمل في الفترة — الأكتر إنتاجًا الأول، ولون ثابت لكل منتج
            var productTotals = points
                .GroupBy(p => (p.ProductId, p.ProductName))
                .Select(g => (g.Key.ProductId, g.Key.ProductName, Total: g.Sum(x => x.CompletedPieces)))
                .OrderByDescending(x => x.Total)
                .ToList();

            var colorByProduct = productTotals
                .Select((p, i) => (p.ProductId, Color: ChartPalette[i % ChartPalette.Length]))
                .ToDictionary(x => x.ProductId, x => x.Color);

            ChartLegend.Clear();
            foreach (var p in productTotals)
            {
                ChartLegend.Add(new ChartLegendItem
                {
                    Color = colorByProduct[p.ProductId],
                    ProductName = p.ProductName,
                    TotalText = $"{p.Total:N0} قطعة"
                });
            }

            // كل أسابيع الفترة بالترتيب الزمني (حتى الفاضية — محور الزمن لازم يكون متصل)
            var (firstWeekStart, _) = WeeklySummaryService.GetWorkWeekRange(DateTime.Today.AddDays(-7 * (SelectedChartWeeks - 1)));
            var pointsByWeek = points.ToLookup(p => p.WeekStart);
            var maxPieces = points.Count == 0 ? 1 : points.Max(p => p.CompletedPieces);

            ChartWeekGroups.Clear();
            for (var week = firstWeekStart; week <= DateTime.Today; week = week.AddDays(7))
            {
                var weekPoints = pointsByWeek[week]
                    .OrderBy(p => productTotals.FindIndex(t => t.ProductId == p.ProductId))
                    .ToList();

                ChartWeekGroups.Add(new ChartWeekGroup
                {
                    WeekLabel = $"{week:dd/MM}",
                    TotalText = weekPoints.Count == 0 ? "—" : $"{weekPoints.Sum(p => p.CompletedPieces):N0}",
                    Bars = weekPoints.Select(p => new ChartBar
                    {
                        Color = colorByProduct[p.ProductId],
                        // ارتفاع نسبي على أعلى قيمة في الفترة (بحد أدنى مرئي للقيم الصغيرة)
                        Height = Math.Max(4, (double)p.CompletedPieces / maxPieces * MaxBarHeight),
                        Tooltip = $"{p.ProductName}\nأسبوع {p.WeekStart:dd/MM} - {p.WeekEnd:dd/MM}\n{p.CompletedPieces:N0} قطعة مكتملة"
                    }).ToList()
                });
            }

            ChartHasData = points.Count > 0;
        }

        // ======================= تصدير Excel =======================

        [RelayCommand]
        private async Task ExportWeekAsync()
        {
            if (WeeklyRows.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات في هذا الأسبوع للتصدير", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var (weekStart, _) = WeeklySummaryService.GetWorkWeekRange(_weekAnchor);
            var dialog = new SaveFileDialog
            {
                Title = "حفظ كشف الأسبوع",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"كشف أسبوع {weekStart:yyyy-MM-dd}.xlsx"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var weeklyService = scope.ServiceProvider.GetRequiredService<WeeklySummaryService>();
                var excelService = scope.ServiceProvider.GetRequiredService<WeeklyReportExcelService>();

                // بنجيب البيانات طازة وقت التصدير (مش من صفوف العرض) — مصدر حقيقة واحد
                var summaries = await weeklyService.GetTeamWeeklySummaryAsync(_weekAnchor);
                excelService.ExportWeeklySummary(summaries, dialog.FileName);

                // عرض النتيجة مع خيار فتح الملف فورًا
                var open = MessageBox.Show(
                    $"تم حفظ الكشف بنجاح:\n{dialog.FileName}\n\nفتح الملف الآن؟",
                    "تم التصدير", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (open == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر حفظ الملف:\n{ex.Message}", "خطأ في التصدير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    // ======================= نماذج العرض =======================

    /// <summary>سطر واحد في جدول تقييم اليوم، بتصنيف ملوّن جاهز للعرض</summary>
    public class DailyReportRow
    {
        public string WorkerName { get; private init; } = "";
        public int TotalPieces { get; private init; }
        public decimal TotalWorkdays { get; private init; }
        public string PercentText { get; private init; } = "";
        public string RatingText { get; private init; } = "";
        public string RatingColor { get; private init; } = "#666666";
        public string AttendanceText { get; private init; } = "";
        public string BreakdownText { get; private init; } = "";
        public string PenaltiesText { get; private init; } = "";

        /// <summary>تحويل نتيجة التقييم من الخدمة لشكل العرض (نص + لون لكل تصنيف)</summary>
        public static DailyReportRow From(WorkerDailySummaryDto dto, string penaltiesText)
        {
            var (ratingText, ratingColor) = dto.Rating switch
            {
                PerformanceRating.TopPerformer => ("⭐ الأفضل النهارده", "#B7791F"),
                PerformanceRating.AboveAverage => ("فوق المتوسط", "#0B6E4F"),
                PerformanceRating.Average => ("متوسط", "#666666"),
                PerformanceRating.BelowAverage => ("تحت المتوسط", "#C62828"),
                PerformanceRating.UnexcusedAbsence => ("غياب بدون إذن", "#B00020"),
                _ => ("غير محدد", "#666666")
            };

            return new DailyReportRow
            {
                WorkerName = dto.WorkerName,
                TotalPieces = dto.TotalPieces,
                TotalWorkdays = dto.TotalWorkdays,
                // النسبة مالهاش معنى لو مفيش إنتاج أصلاً
                PercentText = dto.TotalPieces == 0 ? "—" : $"{dto.PercentVsAverage:+0.#;-0.#;0}%",
                RatingText = ratingText,
                RatingColor = ratingColor,
                AttendanceText = dto.AttendanceStatus switch
                {
                    Core.Enums.AttendanceStatus.Present => "حاضر",
                    Core.Enums.AttendanceStatus.AbsentWithPermission => "غياب بإذن",
                    Core.Enums.AttendanceStatus.AbsentWithoutPermission => "غياب بدون إذن",
                    _ => "—"
                },
                BreakdownText = string.Join("، ",
                    dto.Breakdown.Select(b => $"{b.ProductName}/{b.StageName}: {b.PieceCount}")),
                PenaltiesText = penaltiesText
            };
        }
    }

    /// <summary>مجموعة أعمدة أسبوع واحد في رسم إنتاج المنتجات</summary>
    public class ChartWeekGroup
    {
        public string WeekLabel { get; init; } = "";
        public string TotalText { get; init; } = "";
        public List<ChartBar> Bars { get; init; } = new();
    }

    /// <summary>عمود واحد في الرسم: منتج في أسبوع (اللون بيميز المنتج)</summary>
    public class ChartBar
    {
        public string Color { get; init; } = "#1F3864";
        public double Height { get; init; }
        public string Tooltip { get; init; } = "";
    }

    /// <summary>عنصر في مفتاح ألوان الرسم: المنتج ولونه وإجماليه في الفترة</summary>
    public class ChartLegendItem
    {
        public string Color { get; init; } = "#1F3864";
        public string ProductName { get; init; } = "";
        public string TotalText { get; init; } = "";
    }

    /// <summary>سطر واحد في كشف الأسبوع (مرتّب بصافي اليوميات)</summary>
    public class WeeklyReportRow
    {
        public int Rank { get; private init; }
        public string BestMark { get; private init; } = "";
        public string WorkerName { get; private init; } = "";
        public string EmployeeCode { get; private init; } = "";
        public decimal ProducedWorkdays { get; private init; }
        public int TotalPieces { get; private init; }
        public int PresentDays { get; private init; }
        public int AbsentWithPermissionDays { get; private init; }
        public int AbsentWithoutPermissionDays { get; private init; }
        public decimal AbsenceDeduction { get; private init; }
        public decimal PenaltyDeduction { get; private init; }
        public decimal NetWorkdays { get; private init; }
        public string NetColor { get; private init; } = "#1F3864";
        public string BreakdownText { get; private init; } = "";
        public string PenaltiesText { get; private init; } = "";

        /// <summary>هل فيه تفاصيل تستحق العرض في لوحة التفاصيل؟</summary>
        public bool HasBreakdown => BreakdownText.Length > 0;
        public bool HasPenalties => PenaltiesText.Length > 0;

        public static WeeklyReportRow From(WorkerWeeklySummaryDto dto, int rank) => new()
        {
            Rank = rank,
            BestMark = dto.IsBestWorkerOfWeek ? "⭐" : "",
            WorkerName = dto.WorkerName,
            EmployeeCode = dto.EmployeeCode ?? "—",
            ProducedWorkdays = dto.ProducedWorkdays,
            TotalPieces = dto.TotalPieces,
            PresentDays = dto.PresentDays,
            AbsentWithPermissionDays = dto.AbsentWithPermissionDays,
            AbsentWithoutPermissionDays = dto.AbsentWithoutPermissionDays,
            AbsenceDeduction = dto.AbsenceDeduction,
            PenaltyDeduction = dto.PenaltyDeduction,
            NetWorkdays = dto.NetWorkdays,
            NetColor = dto.NetWorkdays < 0 ? "#C62828" : "#1F3864", // الصافي السالب أحمر
            BreakdownText = string.Join("، ",
                dto.Breakdown.Select(b => $"{b.ProductName}/{b.StageName}: {b.PieceCount} قطعة ({b.Workdays} يومية)")),
            PenaltiesText = string.Join("، ",
                dto.Penalties.Select(p => $"{p.Date:MM/dd} {p.Reason} ({p.DeductionName})"))
        };
    }
}
