using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.Business.DTOs;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.UI.ViewModels
{
    /// <summary>
    /// عقل شاشة التسجيل اليومي، وفيها 3 أقسام لنفس اليوم المختار:
    ///
    /// 1) رحلة الإنتاج: تختار المنتج فتظهر مراحله بترتيب خط الإنتاج،
    ///    توزّع على كل مرحلة عامل أو أكتر (المؤهلين بس)، وتسجل الإنتاج
    ///    كنطاقات "من مرحلة إلى مرحلة: عدد قطع". القطع بتتوزع تلقائيًا
    ///    بالتساوي على عمال المرحلة (وتقدر تعدّل يدوي)، ومعاينة اليوميات
    ///    بتظهر لحظيًا قبل الحفظ. الحفظ بيسجل كل حاجة دفعة واحدة
    ///    وبيعلّم حضور تلقائي لكل من شارك (ProductionFlowService).
    ///
    /// 2) الحضور: كل العمال النشطين بحالة حضور وحفظ جماعي (Upsert).
    /// 3) الجزاءات: تسجيل جزاء بسبب وخصم محدد، وقائمة جزاءات اليوم مع حذف.
    /// </summary>
    public partial class DailyEntryViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// بيمنع إعادة الحساب أثناء ما الكود نفسه بيعدّل القيم (بناء الصفوف
        /// أو التوزيع التلقائي) — من غيره كل تعديل برمجي كان هيشغّل
        /// سلسلة إعادة حساب لا نهائية.
        /// </summary>
        private bool _suppressFlowCallbacks;

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

        /// <summary>أول تحميل للشاشة: المنتجات + الحضور + الجزاءات</summary>
        public async Task InitializeAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var productRepo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

                var products = await productRepo.GetActiveWithStagesAsync();
                Products.Clear();
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

                    Products.Add(new ProductOption { ProductId = p.Id, Name = p.Name, Stages = stages });
                }
            }

            SelectedProduct = Products.FirstOrDefault(); // بيشغّل تحميل الرحلة تلقائيًا
            await LoadAttendanceAsync();
            await LoadPenaltiesAsync();
        }

        private async Task ReloadForDateAsync()
        {
            await LoadFlowForProductAsync();
            await LoadAttendanceAsync();
            await LoadPenaltiesAsync();
        }

        // ======================= قسم رحلة الإنتاج =======================

        public ObservableCollection<ProductOption> Products { get; } = new();

        [ObservableProperty]
        private ProductOption? _selectedProduct;

        partial void OnSelectedProductChanged(ProductOption? value)
        {
            _ = LoadFlowForProductAsync();
        }

        /// <summary>مراحل المنتج المختار — بطاقة لكل مرحلة بعمالها المؤهلين</summary>
        public ObservableCollection<FlowStageRow> FlowStages { get; } = new();

        /// <summary>نطاقات الإنتاج: "من مرحلة إلى مرحلة: عدد قطع"</summary>
        public ObservableCollection<FlowRangeRow> FlowRanges { get; } = new();

        /// <summary>معاينة يوميات كل عامل قبل الحفظ (بتتحدث لحظيًا)</summary>
        public ObservableCollection<FlowWorkerTotalDto> FlowPreview { get; } = new();

        /// <summary>تحذيرات لحظية (نطاقات متداخلة، توزيع مش مظبوط...) قبل ما المستخدم يحفظ</summary>
        [ObservableProperty]
        private string _flowWarning = string.Empty;

        /// <summary>يبني بطاقات المراحل وعمالها المؤهلين للمنتج المختار</summary>
        private async Task LoadFlowForProductAsync()
        {
            _suppressFlowCallbacks = true;
            try
            {
                FlowStages.Clear();
                FlowRanges.Clear();
                FlowPreview.Clear();
                FlowWarning = string.Empty;

                var product = SelectedProduct;
                if (product is null || product.Stages.Count == 0) return;

                using var scope = _scopeFactory.CreateScope();
                var workerRepo = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
                var productionRepo = scope.ServiceProvider.GetRequiredService<IDailyProductionRepository>();

                // المؤهلين لكل مراحل المنتج باستعلام واحد
                var skillsByStage = (await workerRepo.GetSkillsForProductAsync(product.ProductId))
                    .ToLookup(ws => ws.ProductionStageId);

                // الإنتاج المسجل بالفعل النهارده على مراحل المنتج (تحذير من الإدخال المزدوج)
                var stageIds = product.Stages.Select(s => s.StageId).ToHashSet();
                var alreadyByStage = (await productionRepo.GetByDateAsync(EntryDate))
                    .Where(r => stageIds.Contains(r.ProductionStageId))
                    .GroupBy(r => r.ProductionStageId)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.PieceCount));

                foreach (var stage in product.Stages)
                {
                    alreadyByStage.TryGetValue(stage.StageId, out var already);
                    FlowStages.Add(new FlowStageRow
                    {
                        StageId = stage.StageId,
                        DisplayOrder = stage.DisplayOrder,
                        StageName = stage.StageName,
                        Quota = stage.PiecesPerWorkday,
                        QualifiedWorkers = skillsByStage[stage.StageId]
                            .Select(ws => new WorkerPick(ws.WorkerId, ws.Worker.FullName))
                            .ToList(),
                        AlreadyText = already > 0 ? $"مسجل اليوم: {already}" : ""
                    });
                }

                // نطاق افتراضي جاهز: من أول مرحلة لآخر مرحلة — لو اليوم كله
                // بنفس العدد يبقى المستخدم يكتب رقم واحد بس ويحفظ
                FlowRanges.Add(new FlowRangeRow(product.Stages, OnFlowStructureEdited)
                {
                    FromStage = product.Stages.First(),
                    ToStage = product.Stages.Last()
                });
            }
            finally
            {
                _suppressFlowCallbacks = false;
            }
        }

        /// <summary>تغيير هيكلي (نطاق اتعدل/اتضاف/اتشال أو عامل اتضاف/اتشال) → إعادة حساب وتوزيع</summary>
        private void OnFlowStructureEdited()
        {
            if (_suppressFlowCallbacks) return;
            RecomputeFlow();
        }

        /// <summary>تعديل يدوي في نصيب عامل → تحديث المعاينة بس (من غير ما نداس على تعديله)</summary>
        private void OnFlowSharesEdited()
        {
            if (_suppressFlowCallbacks) return;
            RecomputeTotals(new List<string>());
        }

        /// <summary>
        /// إعادة الحساب الكاملة: بيحسب إنتاج كل مرحلة من النطاقات، وبيوزّع
        /// قطع كل مرحلة بالتساوي على عمالها (الباقي بيتوزع واحدة واحدة على
        /// الأوائل)، وبعدها بيحدّث المعاينة. أي مشكلة بتظهر كتحذير لحظي.
        /// </summary>
        private void RecomputeFlow()
        {
            var warnings = new List<string>();

            _suppressFlowCallbacks = true;
            try
            {
                // 1) إنتاج كل مرحلة من النطاقات (بنفس قواعد الخدمة: بلا تداخل وبترتيب صحيح)
                foreach (var row in FlowStages) row.ComputedPieces = 0;

                var indexByStageId = FlowStages
                    .Select((row, index) => (row.StageId, index))
                    .ToDictionary(x => x.StageId, x => x.index);

                foreach (var range in FlowRanges)
                {
                    if (range.FromStage is null || range.ToStage is null) continue;
                    if (string.IsNullOrWhiteSpace(range.PiecesText)) continue;

                    if (!int.TryParse(range.PiecesText.Trim(), out var pieces) || pieces <= 0)
                    {
                        warnings.Add($"⚠ عدد القطع \"{range.PiecesText}\" مش رقم صحيح موجب");
                        continue;
                    }

                    var fromIndex = indexByStageId[range.FromStage.StageId];
                    var toIndex = indexByStageId[range.ToStage.StageId];
                    if (fromIndex > toIndex)
                    {
                        warnings.Add($"⚠ نطاق معكوس: \"{range.FromStage.StageName}\" بتيجي بعد \"{range.ToStage.StageName}\" في الترتيب");
                        continue;
                    }

                    for (var i = fromIndex; i <= toIndex; i++)
                    {
                        if (FlowStages[i].ComputedPieces != 0)
                        {
                            warnings.Add($"⚠ مرحلة \"{FlowStages[i].StageName}\" واقعة في أكتر من نطاق — النطاقات ميصحش تتداخل");
                            continue;
                        }
                        FlowStages[i].ComputedPieces = pieces;
                    }
                }

                // 2) توزيع متساوٍ تلقائي على عمال كل مرحلة (قابل للتعديل اليدوي بعدها)
                foreach (var row in FlowStages)
                {
                    var workers = row.AssignedWorkers;
                    if (workers.Count == 0) continue;

                    if (row.ComputedPieces == 0)
                    {
                        foreach (var share in workers) share.SharePieces = "";
                        continue;
                    }

                    var baseShare = row.ComputedPieces / workers.Count;
                    var remainder = row.ComputedPieces % workers.Count;
                    for (var i = 0; i < workers.Count; i++)
                        workers[i].SharePieces = (baseShare + (i < remainder ? 1 : 0)).ToString();
                }
            }
            finally
            {
                _suppressFlowCallbacks = false;
            }

            RecomputeTotals(warnings);
        }

        /// <summary>يبني معاينة إجمالي كل عامل (قطع + يوميات) ويجمّع التحذيرات في سطر واحد</summary>
        private void RecomputeTotals(List<string> warnings)
        {
            var totals = new Dictionary<int, (string Name, int Pieces, decimal Workdays)>();

            foreach (var row in FlowStages)
            {
                if (row.ComputedPieces == 0 && row.AssignedWorkers.Count == 0) continue;

                var stageSum = 0;
                foreach (var share in row.AssignedWorkers)
                {
                    if (!int.TryParse(share.SharePieces?.Trim(), out var pieces) || pieces <= 0) continue;

                    stageSum += pieces;
                    var workdays = Math.Round((decimal)pieces / row.Quota, 2);
                    totals[share.WorkerId] = totals.TryGetValue(share.WorkerId, out var t)
                        ? (t.Name, t.Pieces + pieces, t.Workdays + workdays)
                        : (share.WorkerName, pieces, workdays);
                }

                // مرحلة عليها إنتاج لكن التوزيع مش مساويه — تحذير قبل ما الحفظ يرفضها
                if (row.ComputedPieces > 0 && stageSum != row.ComputedPieces)
                    warnings.Add($"⚠ مرحلة \"{row.StageName}\": مجموع التوزيع ({stageSum}) ≠ إنتاج المرحلة ({row.ComputedPieces})");
            }

            FlowPreview.Clear();
            foreach (var t in totals.Values.OrderByDescending(t => t.Workdays))
            {
                FlowPreview.Add(new FlowWorkerTotalDto
                {
                    WorkerName = t.Name,
                    TotalPieces = t.Pieces,
                    TotalWorkdays = t.Workdays
                });
            }

            FlowWarning = string.Join("\n", warnings.Distinct());
        }

        // ------- أوامر رحلة الإنتاج -------

        [RelayCommand]
        private void AddRange()
        {
            if (SelectedProduct is null) return;
            FlowRanges.Add(new FlowRangeRow(SelectedProduct.Stages, OnFlowStructureEdited));
        }

        [RelayCommand]
        private void RemoveRange(FlowRangeRow? range)
        {
            if (range is null) return;
            FlowRanges.Remove(range);
            RecomputeFlow();
        }

        [RelayCommand]
        private void AddWorkerToStage(FlowStageRow? stage)
        {
            if (stage?.SelectedWorkerToAdd is not { } pick) return;

            // منع إضافة نفس العامل مرتين لنفس المرحلة
            if (stage.AssignedWorkers.Any(s => s.WorkerId == pick.WorkerId)) return;

            stage.AssignedWorkers.Add(new FlowShareEntry(stage, pick.WorkerId, pick.Name, OnFlowSharesEdited));
            stage.SelectedWorkerToAdd = null;
            RecomputeFlow(); // إعادة التوزيع المتساوي بعد إضافة عامل
        }

        [RelayCommand]
        private void RemoveWorkerShare(FlowShareEntry? share)
        {
            if (share is null) return;
            share.Parent.AssignedWorkers.Remove(share);
            RecomputeFlow(); // إعادة التوزيع المتساوي بعد إزالة عامل
        }

        [RelayCommand]
        private async Task SaveFlowAsync()
        {
            if (SelectedProduct is null) return;

            // النطاقات المكتملة بس (مرحلة بداية ونهاية وعدد صحيح موجب)
            var ranges = FlowRanges
                .Where(r => r.FromStage is not null && r.ToStage is not null &&
                            int.TryParse(r.PiecesText?.Trim(), out var p) && p > 0)
                .Select(r => new FlowRangeDto
                {
                    FromStageId = r.FromStage!.StageId,
                    ToStageId = r.ToStage!.StageId,
                    PieceCount = int.Parse(r.PiecesText!.Trim())
                })
                .ToList();

            // أنصبة العمال من كل بطاقات المراحل
            var shares = FlowStages
                .SelectMany(row => row.AssignedWorkers
                    .Where(s => int.TryParse(s.SharePieces?.Trim(), out var p) && p > 0)
                    .Select(s => new FlowShareDto
                    {
                        ProductionStageId = row.StageId,
                        WorkerId = s.WorkerId,
                        PieceCount = int.Parse(s.SharePieces!.Trim())
                    }))
                .ToList();

            try
            {
                FlowSaveResultDto result;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var flowService = scope.ServiceProvider.GetRequiredService<ProductionFlowService>();
                    // الخدمة بتتحقق من كل حاجة تاني (مصدر الحقيقة الوحيد للقواعد) — يا كله يا مفيش
                    result = await flowService.RecordFlowAsync(SelectedProduct.ProductId, EntryDate, ranges, shares);
                }

                // ملخص واضح لكل اللي حصل: سجلات + يوميات كل عامل + الحضور التلقائي
                var totalsLines = string.Join("\n", result.WorkerTotals.Select(t =>
                    $"  • {t.WorkerName}: {t.TotalPieces} قطعة ≈ {t.TotalWorkdays} يومية"));
                var attendanceLine = result.AttendanceMarkedCount > 0
                    ? $"\n\n✔ اتسجل حضور تلقائي لـ {result.AttendanceMarkedCount} عامل"
                    : "";

                MessageBox.Show(
                    $"تم حفظ رحلة إنتاج \"{SelectedProduct.Name}\" بتاريخ {EntryDate:yyyy/MM/dd}\n" +
                    $"({result.RecordsCount} سجل على {result.StagesCovered} مراحل)\n\n" +
                    $"يوميات العمال:\n{totalsLines}{attendanceLine}",
                    "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

                // إعادة تحميل: "مسجل اليوم" بيتحدث والرحلة بتبدأ نظيفة، والحضور بيعكس التلقائي
                await LoadFlowForProductAsync();
                await LoadAttendanceAsync();
            }
            catch (InvalidOperationException ex)
            {
                // رسائل التحقق العربية الواضحة من الخدمة بتوصل للمستخدم زي ما هي
                MessageBox.Show(ex.Message, "راجع بيانات الرحلة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

    // ======================= نماذج العرض الخاصة بالشاشة =======================

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

    /// <summary>عامل مؤهل في قائمة اختيار عمال المرحلة</summary>
    public record WorkerPick(int WorkerId, string Name);

    /// <summary>
    /// بطاقة مرحلة واحدة في رحلة الإنتاج: بياناتها + عمالها المؤهلين +
    /// العمال المتوزعين عليها بأنصبتهم + إنتاجها المحسوب من النطاقات.
    /// </summary>
    public partial class FlowStageRow : ObservableObject
    {
        public int StageId { get; init; }
        public int DisplayOrder { get; init; }
        public string StageName { get; init; } = "";
        public int Quota { get; init; }
        public List<WorkerPick> QualifiedWorkers { get; init; } = new();

        /// <summary>مفيش عمال مؤهلين للمرحلة دي — لازم تتربط المهارات الأول (قرار: المؤهلين بس)</summary>
        public bool HasNoQualified => QualifiedWorkers.Count == 0;

        public string QuotaText => $"الكوتة: {Quota}";

        /// <summary>تنبيه لو فيه إنتاج متسجل بالفعل على المرحلة في نفس اليوم</summary>
        [ObservableProperty]
        private string _alreadyText = "";

        [ObservableProperty]
        private WorkerPick? _selectedWorkerToAdd;

        /// <summary>إنتاج المرحلة المحسوب من النطاقات (صفر = مش داخلة الرحلة النهارده)</summary>
        [ObservableProperty]
        private int _computedPieces;

        public ObservableCollection<FlowShareEntry> AssignedWorkers { get; } = new();
    }

    /// <summary>نصيب عامل واحد من قطع مرحلة (خانة القطع بتتملى تلقائي وتتعدل يدوي)</summary>
    public partial class FlowShareEntry : ObservableObject
    {
        public FlowShareEntry(FlowStageRow parent, int workerId, string workerName, Action onEdited)
        {
            Parent = parent;
            WorkerId = workerId;
            WorkerName = workerName;
            _onEdited = onEdited;
        }

        private readonly Action _onEdited;

        /// <summary>البطاقة الأم — عشان أمر الإزالة يعرف يشيل النصيب من مرحلته</summary>
        public FlowStageRow Parent { get; }
        public int WorkerId { get; }
        public string WorkerName { get; }

        [ObservableProperty]
        private string _sharePieces = "";

        partial void OnSharePiecesChanged(string value) => _onEdited();
    }

    /// <summary>نطاق إنتاج واحد: من مرحلة إلى مرحلة بعدد قطع</summary>
    public partial class FlowRangeRow : ObservableObject
    {
        private readonly Action _onEdited;

        public FlowRangeRow(List<StageEntryOption> stageOptions, Action onEdited)
        {
            StageOptions = stageOptions;
            _onEdited = onEdited;
        }

        /// <summary>مراحل المنتج بالترتيب — نفس القائمة لقايمتي "من" و"إلى"</summary>
        public List<StageEntryOption> StageOptions { get; }

        [ObservableProperty]
        private StageEntryOption? _fromStage;

        [ObservableProperty]
        private StageEntryOption? _toStage;

        [ObservableProperty]
        private string _piecesText = "";

        partial void OnFromStageChanged(StageEntryOption? value) => _onEdited();
        partial void OnToStageChanged(StageEntryOption? value) => _onEdited();
        partial void OnPiecesTextChanged(string value) => _onEdited();
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
