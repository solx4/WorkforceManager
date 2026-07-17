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
    /// عقل شاشة التسجيل اليومي الذكي، وفيها 3 أقسام لنفس اليوم المختار:
    /// 1) الإنتاج: تختار منتج ومرحلة، يظهرلك العمال المؤهلين ليها، تكتب
    ///    عدد القطع قدام كل واحد اشتغل، وتحفظ الكل بضغطة واحدة.
    /// 2) الحضور: كل العمال النشطين بحالة حضور (حاضر/غياب بإذن/بدون إذن)
    ///    وحفظ جماعي (Upsert — التعديل بيحدّث مش بيكرر).
    /// 3) الجزاءات: تسجيل جزاء بسبب وخصم محدد، وقائمة جزاءات اليوم مع حذف.
    /// </summary>
    public partial class DailyEntryViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

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

        // ------- قسم الإنتاج -------

        public ObservableCollection<ProductOption> Products { get; } = new();

        [ObservableProperty]
        private ProductOption? _selectedProduct;

        partial void OnSelectedProductChanged(ProductOption? value)
        {
            Stages.Clear();
            if (value is null) return;
            foreach (var s in value.Stages) Stages.Add(s);
            // اختيار أول مرحلة تلقائيًا — أقل ضغطات للمستخدم
            SelectedStage = Stages.FirstOrDefault();
        }

        public ObservableCollection<StageEntryOption> Stages { get; } = new();

        [ObservableProperty]
        private StageEntryOption? _selectedStage;

        partial void OnSelectedStageChanged(StageEntryOption? value)
        {
            _ = LoadProductionEntriesAsync();
        }

        /// <summary>معلومة الكوتة المعروضة جنب المرحلة المختارة</summary>
        [ObservableProperty]
        private string _stageInfo = string.Empty;

        /// <summary>ملاحظة لو مفيش عمال مؤهلين متسجلين للمرحلة (fallback بيعرض الكل)</summary>
        [ObservableProperty]
        private string _qualifiedNote = string.Empty;

        public ObservableCollection<ProductionEntryRow> Entries { get; } = new();

        /// <summary>أول تحميل للشاشة: المنتجات + الحضور + الجزاءات</summary>
        public async Task InitializeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            var products = await productRepo.GetActiveWithStagesAsync();
            Products.Clear();
            foreach (var p in products)
            {
                Products.Add(new ProductOption
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Stages = p.Stages
                        .OrderBy(s => s.SortOrder)
                        .Select(s => new StageEntryOption
                        {
                            StageId = s.Id,
                            StageName = s.StageName,
                            PiecesPerWorkday = s.PiecesPerWorkday
                        }).ToList()
                });
            }

            SelectedProduct = Products.FirstOrDefault();
            await ReloadForDateAsync();
        }

        private async Task ReloadForDateAsync()
        {
            await LoadProductionEntriesAsync();
            await LoadAttendanceAsync();
            await LoadPenaltiesAsync();
        }

        private async Task LoadProductionEntriesAsync()
        {
            Entries.Clear();
            QualifiedNote = string.Empty;

            var stage = SelectedStage;
            if (stage is null)
            {
                StageInfo = string.Empty;
                return;
            }

            StageInfo = $"كوتة اليومية: {stage.PiecesPerWorkday} قطعة = يومية كاملة";

            using var scope = _scopeFactory.CreateScope();
            var workerRepo = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
            var productionRepo = scope.ServiceProvider.GetRequiredService<IDailyProductionRepository>();

            // الأصل: العمال المؤهلين للمرحلة دي بس. لو مفيش (المهارات لسه
            // ما اتربطتش)، بنعرض كل النشطين مع تنبيه — عشان الشغل ميقفش
            var workers = await workerRepo.GetQualifiedForStageAsync(stage.StageId);
            if (workers.Count == 0)
            {
                workers = await workerRepo.GetActiveWithSkillsAsync();
                QualifiedNote = "لا يوجد عمال مسجّلة لهم هذه المهارة بعد — معروض كل العمال النشطين";
            }

            // اللي اتسجل لهم إنتاج بالفعل على نفس المرحلة في نفس اليوم (منع الإدخال المزدوج بالغلط)
            var existing = (await productionRepo.GetByDateAsync(EntryDate))
                .Where(r => r.ProductionStageId == stage.StageId)
                .GroupBy(r => r.WorkerId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.PieceCount));

            foreach (var w in workers)
            {
                existing.TryGetValue(w.Id, out var already);
                Entries.Add(new ProductionEntryRow
                {
                    WorkerId = w.Id,
                    FullName = w.FullName,
                    EmployeeCode = w.EmployeeCode ?? "—",
                    AlreadyRecordedText = already > 0 ? $"مسجل اليوم: {already}" : ""
                });
            }
        }

        [RelayCommand]
        private async Task SaveProductionAsync()
        {
            if (SelectedStage is null) return;

            // تجميع الإدخالات الصالحة بس (أرقام موجبة)
            var toSave = new List<(int WorkerId, int PieceCount)>();
            var invalid = new List<string>();
            foreach (var e in Entries)
            {
                if (string.IsNullOrWhiteSpace(e.PiecesText)) continue;
                if (int.TryParse(e.PiecesText.Trim(), out var pieces) && pieces > 0)
                    toSave.Add((e.WorkerId, pieces));
                else
                    invalid.Add(e.FullName);
            }

            if (invalid.Count > 0)
            {
                MessageBox.Show($"قيم غير صحيحة (لازم أرقام موجبة) عند:\n{string.Join("\n", invalid)}",
                    "تحقق من الإدخال", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (toSave.Count == 0)
            {
                MessageBox.Show("مفيش أي أرقام متكتبة للحفظ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var workdayService = scope.ServiceProvider.GetRequiredService<WorkdayCalculationService>();
            var saved = await workdayService.RecordProductionBatchAsync(SelectedStage.StageId, EntryDate, toSave);

            MessageBox.Show($"تم حفظ إنتاج {saved} عامل على مرحلة \"{SelectedStage.StageName}\" بتاريخ {EntryDate:yyyy/MM/dd}",
                "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

            // إعادة التحميل: الخانات بتتفضى و"مسجل اليوم" بيتحدث
            await LoadProductionEntriesAsync();
        }

        // ------- قسم الحضور -------

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

        // ------- قسم الجزاءات -------

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

    // ------- نماذج العرض الخاصة بالشاشة -------

    /// <summary>منتج في قائمة الاختيار مع مراحله الجاهزة</summary>
    public class ProductOption
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = "";
        public List<StageEntryOption> Stages { get; init; } = new();
    }

    /// <summary>مرحلة في قائمة الاختيار (بكوتتها لعرض المعلومة)</summary>
    public class StageEntryOption
    {
        public int StageId { get; init; }
        public string StageName { get; init; } = "";
        public int PiecesPerWorkday { get; init; }
    }

    /// <summary>سطر إدخال إنتاج لعامل واحد (خانة القطع بيكتب فيها المستخدم)</summary>
    public partial class ProductionEntryRow : ObservableObject
    {
        public int WorkerId { get; init; }
        public string FullName { get; init; } = "";
        public string EmployeeCode { get; init; } = "";
        public string AlreadyRecordedText { get; init; } = "";

        /// <summary>عدد القطع كنص (بيتحقق منه وقت الحفظ)</summary>
        [ObservableProperty]
        private string _piecesText = string.Empty;
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
