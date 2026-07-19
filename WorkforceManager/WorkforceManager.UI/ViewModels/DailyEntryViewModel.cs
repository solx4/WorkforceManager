using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.UI.ViewModels
{
    /// <summary>
    /// عقل شاشة التسجيل اليومي، وفيها 3 أقسام لنفس اليوم المختار:
    ///
    /// 1) رحلات الإنتاج: ممكن الشغل يكون على منتج أو أكتر في نفس اليوم —
    ///    كل منتج ليه "رحلة" مستقلة (FlowSessionViewModel): مراحله بترتيب
    ///    خط الإنتاج، توزيع العمال المؤهلين، نطاقات الإنتاج، معاينة
    ///    اليوميات، وحفظ مستقل. زرار "إضافة منتج" بيضيف رحلة جديدة.
    ///
    /// 2) الحضور: كل العمال النشطين بحالة حضور وحفظ جماعي (Upsert).
    /// 3) الجزاءات: تسجيل جزاء بسبب وخصم محدد، وقائمة جزاءات اليوم مع حذف.
    /// </summary>
    public partial class DailyEntryViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>كل المنتجات النشطة بمراحلها — بتتحمل مرة واحدة وتتشارك بين كل الرحلات</summary>
        private readonly List<ProductOption> _products = new();

        public DailyEntryViewModel(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            // خيارات الخصم الثابتة لقائمة الجزاءات
            DeductionOptions = new List<DeductionOption>
            {
                new(PenaltyDeduction.HalfDay),
                new(PenaltyDeduction.OneDay),
                new(PenaltyDeduction.ThreeDays),
                new(PenaltyDeduction.OneWeek)
            };
            SelectedDeduction = DeductionOptions[0];
        }

        // ------- اليوم المختار (مشترك بين الأقسام الثلاثة) -------

        [ObservableProperty]
        private DateTime _entryDate = DateTime.Today;

        partial void OnEntryDateChanged(DateTime value)
        {
            // تغيير اليوم بيعيد تحميل كل حاجة مرتبطة بيه
            _ = ReloadForDateAsync();
        }

        /// <summary>أول تحميل للشاشة: المنتجات + أول رحلة + الحضور + الجزاءات</summary>
        public async Task InitializeAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                var products = await productRepo.GetActiveWithStagesAsync();
                _products.Clear();
                foreach (var p in products)
                {
                    // المراحل بترتيب خط الإنتاج + رقم الترتيب المعروض (1، 2، 3...)
                    var stages = p.Stages
                        .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                        .Select((s, i) => new StageEntryOption
                        {
                            StageId = s.Id,
                            StageName = s.StageName,
                            PiecesPerWorkday = s.PiecesPerWorkday,
                            DisplayOrder = i + 1
                        }).ToList();

                    _products.Add(new ProductOption { ProductId = p.Id, Name = p.Name, Stages = stages });
                }
            }

            // أول رحلة جاهزة بأول منتج — الشاشة بتفتح شغالة على طول
            FlowSessions.Clear();
            var firstSession = CreateSession();
            firstSession.SelectedProduct = _products.FirstOrDefault();
            FlowSessions.Add(firstSession);

            await LoadAttendanceAsync();
            await LoadPenaltiesAsync();
        }

        private async Task ReloadForDateAsync()
        {
            // كل رحلة بتعيد تحميل "مسجل اليوم" بتاعها لليوم الجديد
            foreach (var session in FlowSessions)
                await session.ReloadAsync();

            await LoadAttendanceAsync();
            await LoadPenaltiesAsync();
        }

        // ======================= قسم رحلات الإنتاج (منتج أو أكتر في اليوم) =======================

        public ObservableCollection<FlowSessionViewModel> FlowSessions { get; } = new();

        /// <summary>رحلة جديدة مربوطة بيوم الشاشة وتحديث الحضور بعد حفظها</summary>
        private FlowSessionViewModel CreateSession() =>
            new(_scopeFactory, _products, () => EntryDate, LoadAttendanceAsync);

        /// <summary>إضافة منتج تاني للشغل عليه في نفس اليوم (رحلة جديدة فاضية بيختار منتجها)</summary>
        [RelayCommand]
        private void AddFlowSession()
        {
            FlowSessions.Add(CreateSession());
        }

        [RelayCommand]
        private void RemoveFlowSession(FlowSessionViewModel? session)
        {
            if (session is null) return;

            // لازم تفضل رحلة واحدة على الأقل — الشاشة من غير ولا رحلة ملهاش معنى
            if (FlowSessions.Count == 1)
            {
                MessageBox.Show("لازم تفضل رحلة منتج واحدة على الأقل — لو عايز منتج مختلف غيّره من القائمة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // تأكيد بس لو المستخدم كتب حاجة فيها (عشان ميخسرش شغله بضغطة غلط)
            if (session.HasUserInput &&
                MessageBox.Show($"إزالة رحلة \"{session.SelectedProduct?.Name ?? "بدون منتج"}\"؟ اللي اتكتب فيها هيضيع (اللي اتحفظ قبل كده محفوظ عادي).",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            FlowSessions.Remove(session);
        }

        // ======================= قسم الحضور =======================

        public static List<AttendanceStatusOption> StatusOptions { get; } = new()
        {
            new AttendanceStatusOption(null, "—"),
            new AttendanceStatusOption(AttendanceStatus.Present, "حاضر"),
            new AttendanceStatusOption(AttendanceStatus.AbsentWithPermission, "غياب بإذن"),
            new AttendanceStatusOption(AttendanceStatus.AbsentWithoutPermission, "غياب بدون إذن")
        };

        public ObservableCollection<AttendanceRow> AttendanceRows { get; } = new();

        private async Task LoadAttendanceAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var workerRepo = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
            var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();

            var workers = await workerRepo.GetActiveWithSkillsAsync();
            var existing = (await attendanceRepo.GetByDateAsync(EntryDate))
                .ToDictionary(a => a.WorkerId, a => a.Status);

            AttendanceRows.Clear();
            foreach (var w in workers)
            {
                // القيمة المحفوظة مسبقًا بتظهر محددة، ولو مفيش سجل بتبقى "—"
                var selected = existing.TryGetValue(w.Id, out var st)
                    ? StatusOptions.First(o => o.Value == st)
                    : StatusOptions[0];

                AttendanceRows.Add(new AttendanceRow
                {
                    WorkerId = w.Id,
                    FullName = w.FullName,
                    EmployeeCode = w.EmployeeCode ?? "—",
                    SelectedStatus = selected
                });
            }
        }

        [RelayCommand]
        private async Task SaveAttendanceAsync()
        {
            // تجميع الصفوف اللي المستخدم حدّد لها حالة ("—" معناها مفيش تسجيل، بنسيبه)
            var toSave = AttendanceRows
                .Where(row => row.SelectedStatus?.Value is not null)
                .Select(row => (row.WorkerId, Status: row.SelectedStatus!.Value!.Value))
                .ToList();

            if (toSave.Count == 0)
            {
                MessageBox.Show("مفيش أي حالة حضور محددة للحفظ", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<AttendanceService>();

            // حفظ جماعي في حفظة واحدة بدل استعلام + حفظ لكل عامل
            var saved = await attendanceService.RecordAttendanceBatchAsync(EntryDate, toSave);

            MessageBox.Show($"تم حفظ حضور {saved} عامل بتاريخ {EntryDate:yyyy/MM/dd}",
                "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ======================= قسم الجزاءات =======================

        public List<DeductionOption> DeductionOptions { get; }

        [ObservableProperty]
        private AttendanceRow? _penaltyWorker; // بنستخدم نفس صفوف الحضور كقائمة اختيار العامل

        [ObservableProperty]
        private string _penaltyReason = string.Empty;

        [ObservableProperty]
        private DeductionOption? _selectedDeduction;

        public ObservableCollection<PenaltyRow> DayPenalties { get; } = new();

        private async Task LoadPenaltiesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var penaltyService = scope.ServiceProvider.GetRequiredService<PenaltyService>();

            var penalties = await penaltyService.GetPenaltiesByDateAsync(EntryDate);
            DayPenalties.Clear();
            foreach (var p in penalties)
            {
                DayPenalties.Add(new PenaltyRow
                {
                    PenaltyId = p.Id,
                    WorkerName = p.Worker.FullName,
                    Reason = p.Reason,
                    DeductionName = p.Deduction.ToArabicName()
                });
            }
        }

        [RelayCommand]
        private async Task AddPenaltyAsync()
        {
            if (PenaltyWorker is null)
            {
                MessageBox.Show("اختار العامل الأول", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(PenaltyReason))
            {
                MessageBox.Show("اكتب سبب الجزاء", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SelectedDeduction is null) return;

            using var scope = _scopeFactory.CreateScope();
            var penaltyService = scope.ServiceProvider.GetRequiredService<PenaltyService>();
            await penaltyService.RecordPenaltyAsync(
                PenaltyWorker.WorkerId, EntryDate, PenaltyReason, SelectedDeduction.Value);

            // تفريغ الفورم وإعادة تحميل قائمة اليوم
            PenaltyReason = string.Empty;
            PenaltyWorker = null;
            await LoadPenaltiesAsync();
        }

        [RelayCommand]
        private async Task RemovePenaltyAsync(PenaltyRow? row)
        {
            if (row is null) return;

            if (MessageBox.Show($"حذف جزاء \"{row.Reason}\" عن {row.WorkerName}؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var scope = _scopeFactory.CreateScope();
            var penaltyService = scope.ServiceProvider.GetRequiredService<PenaltyService>();
            await penaltyService.RemovePenaltyAsync(row.PenaltyId);
            await LoadPenaltiesAsync();
        }
    }

    // ======================= نماذج العرض المشتركة للشاشة =======================

    /// <summary>منتج في قائمة الاختيار مع مراحله المرتبة الجاهزة</summary>
    public class ProductOption
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = "";
        public List<StageEntryOption> Stages { get; init; } = new();
    }

    /// <summary>مرحلة في قوائم الاختيار (النطاقات) — برقم ترتيبها في خط الإنتاج</summary>
    public class StageEntryOption
    {
        public int StageId { get; init; }
        public string StageName { get; init; } = "";
        public int PiecesPerWorkday { get; init; }
        public int DisplayOrder { get; init; }

        /// <summary>الاسم المعروض في قوائم "من/إلى": الترتيب + الاسم</summary>
        public string Display => $"{DisplayOrder}. {StageName}";
    }

    /// <summary>خيار حالة حضور في القائمة المنسدلة ("—" = بدون تسجيل)</summary>
    public record AttendanceStatusOption(AttendanceStatus? Value, string Display);

    /// <summary>سطر حضور لعامل واحد</summary>
    public partial class AttendanceRow : ObservableObject
    {
        public int WorkerId { get; init; }
        public string FullName { get; init; } = "";
        public string EmployeeCode { get; init; } = "";

        [ObservableProperty]
        private AttendanceStatusOption? _selectedStatus;
    }

    /// <summary>خيار خصم جزاء في القائمة المنسدلة</summary>
    public class DeductionOption
    {
        public DeductionOption(PenaltyDeduction value) => Value = value;
        public PenaltyDeduction Value { get; }
        public string Display => Value.ToArabicName();
    }

    /// <summary>جزاء واحد في قائمة جزاءات اليوم</summary>
    public class PenaltyRow
    {
        public int PenaltyId { get; init; }
        public string WorkerName { get; init; } = "";
        public string Reason { get; init; } = "";
        public string DeductionName { get; init; } = "";
    }
}
