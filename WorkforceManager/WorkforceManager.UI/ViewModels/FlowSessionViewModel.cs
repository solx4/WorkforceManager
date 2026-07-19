using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WorkforceManager.Business.DTOs;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.UI.ViewModels
{
    /// <summary>
    /// رحلة إنتاج لمنتج واحد داخل شاشة التسجيل اليومي. الشاشة بتعرض
    /// رحلة أو أكتر في نفس اليوم (منتج أو أكتر شغالين مع بعض)، وكل
    /// رحلة مستقلة بمنتجها ومراحلها وتوزيع عمالها ونطاقاتها وحفظها.
    ///
    /// كل المنطق التفاعلي هنا: اختيار المنتج بيبني بطاقات مراحله،
    /// النطاقات بتحسب إنتاج كل مرحلة لحظيًا، القطع بتتوزع بالتساوي على
    /// عمال المرحلة (مع تعديل يدوي)، والمعاينة والتحذيرات بتتحدث فورًا.
    /// الحفظ بيمر على ProductionFlowService (مصدر الحقيقة للقواعد).
    /// </summary>
    public partial class FlowSessionViewModel : ObservableObject
    {
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>اليوم بييجي من الشاشة الأم (مشترك بين كل الرحلات والتبويبات)</summary>
        private readonly Func<DateTime> _getEntryDate;

        /// <summary>بيتنادى بعد حفظ ناجح — الشاشة الأم بتحدّث الحضور (الحضور التلقائي بيظهر فورًا)</summary>
        private readonly Func<Task> _onSavedAsync;

        /// <summary>
        /// بيمنع إعادة الحساب أثناء ما الكود نفسه بيعدّل القيم (بناء الصفوف
        /// أو التوزيع التلقائي) — من غيره كل تعديل برمجي كان هيشغّل
        /// سلسلة إعادة حساب لا نهائية.
        /// </summary>
        private bool _suppressCallbacks;

        public FlowSessionViewModel(
            IServiceScopeFactory scopeFactory,
            IReadOnlyList<ProductOption> products,
            Func<DateTime> getEntryDate,
            Func<Task> onSavedAsync)
        {
            _scopeFactory = scopeFactory;
            Products = products;
            _getEntryDate = getEntryDate;
            _onSavedAsync = onSavedAsync;
        }

        /// <summary>كل المنتجات النشطة (قائمة مشتركة بين كل الرحلات — للقراءة بس)</summary>
        public IReadOnlyList<ProductOption> Products { get; }

        [ObservableProperty]
        private ProductOption? _selectedProduct;

        partial void OnSelectedProductChanged(ProductOption? value)
        {
            // تغيير المنتج بيعيد بناء بطاقات المراحل (وأي خطأ بيظهر مش بيضيع بصمت)
            SafeAsync.Run(ReloadAsync);
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

        /// <summary>هل المستخدم كتب أي حاجة في الرحلة دي؟ (للتأكيد قبل إزالتها)</summary>
        public bool HasUserInput =>
            FlowStages.Any(s => s.AssignedWorkers.Count > 0) ||
            FlowRanges.Any(r => !string.IsNullOrWhiteSpace(r.PiecesText));

        /// <summary>يبني بطاقات المراحل وعمالها المؤهلين للمنتج المختار (وبيتنادى برضه عند تغيير اليوم)</summary>
        public async Task ReloadAsync()
        {
            _suppressCallbacks = true;
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

                // الإنتاج المسجل بالفعل في اليوم ده على مراحل المنتج (تحذير من الإدخال المزدوج)
                var stageIds = product.Stages.Select(s => s.StageId).ToHashSet();
                var alreadyByStage = (await productionRepo.GetByDateAsync(_getEntryDate()))
                    .Where(r => stageIds.Contains(r.ProductionStageId))
                    .GroupBy(r => r.ProductionStageId)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.PieceCount));

                foreach (var stage in product.Stages)
                {
                    alreadyByStage.TryGetValue(stage.StageId, out var already);
                    FlowStages.Add(new FlowStageRow(AddWorkerToStage)
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
                FlowRanges.Add(new FlowRangeRow(product.Stages, OnStructureEdited, RemoveRange)
                {
                    FromStage = product.Stages.First(),
                    ToStage = product.Stages.Last()
                });
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        /// <summary>تغيير هيكلي (نطاق اتعدل/اتضاف/اتشال أو عامل اتضاف/اتشال) → إعادة حساب وتوزيع</summary>
        private void OnStructureEdited()
        {
            if (_suppressCallbacks) return;
            RecomputeFlow();
        }

        /// <summary>تعديل يدوي في نصيب عامل → تحديث المعاينة بس (من غير ما نداس على تعديله)</summary>
        private void OnSharesEdited()
        {
            if (_suppressCallbacks) return;
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

            _suppressCallbacks = true;
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
                _suppressCallbacks = false;
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

        // ------- أوامر الرحلة -------

        [RelayCommand]
        private void AddRange()
        {
            if (SelectedProduct is null) return;
            FlowRanges.Add(new FlowRangeRow(SelectedProduct.Stages, OnStructureEdited, RemoveRange));
        }

        /// <summary>بيتنادى من زرار الحذف اللي على سطر النطاق نفسه</summary>
        private void RemoveRange(FlowRangeRow range)
        {
            FlowRanges.Remove(range);
            RecomputeFlow();
        }

        /// <summary>بيتنادى من زرار "＋ عامل" اللي على بطاقة المرحلة نفسها</summary>
        private void AddWorkerToStage(FlowStageRow stage)
        {
            if (stage.SelectedWorkerToAdd is not { } pick) return;

            // منع إضافة نفس العامل مرتين لنفس المرحلة
            if (stage.AssignedWorkers.Any(s => s.WorkerId == pick.WorkerId)) return;

            stage.AssignedWorkers.Add(
                new FlowShareEntry(stage, pick.WorkerId, pick.Name, OnSharesEdited, RemoveWorkerShare));
            stage.SelectedWorkerToAdd = null;
            RecomputeFlow(); // إعادة التوزيع المتساوي بعد إضافة عامل
        }

        /// <summary>بيتنادى من زرار ✕ اللي على شريحة العامل نفسها</summary>
        private void RemoveWorkerShare(FlowShareEntry share)
        {
            share.Parent.AssignedWorkers.Remove(share);
            RecomputeFlow(); // إعادة التوزيع المتساوي بعد إزالة عامل
        }

        [RelayCommand]
        private async Task SaveFlowAsync()
        {
            if (SelectedProduct is null)
            {
                MessageBox.Show("اختار المنتج الأول", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var entryDate = _getEntryDate();

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
                    result = await flowService.RecordFlowAsync(SelectedProduct.ProductId, entryDate, ranges, shares);
                }

                // ملخص واضح لكل اللي حصل: سجلات + يوميات كل عامل + الحضور التلقائي
                var totalsLines = string.Join("\n", result.WorkerTotals.Select(t =>
                    $"  • {t.WorkerName}: {t.TotalPieces} قطعة ≈ {t.TotalWorkdays} يومية"));
                var attendanceLine = result.AttendanceMarkedCount > 0
                    ? $"\n\n✔ اتسجل حضور تلقائي لـ {result.AttendanceMarkedCount} عامل"
                    : "";

                MessageBox.Show(
                    $"تم حفظ رحلة إنتاج \"{SelectedProduct.Name}\" بتاريخ {entryDate:yyyy/MM/dd}\n" +
                    $"({result.RecordsCount} سجل على {result.StagesCovered} مراحل)\n\n" +
                    $"يوميات العمال:\n{totalsLines}{attendanceLine}",
                    "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

                // إعادة تحميل الرحلة ("مسجل اليوم" بيتحدث وبتبدأ نظيفة) + إبلاغ الشاشة الأم (تحديث الحضور)
                await ReloadAsync();
                await _onSavedAsync();
            }
            catch (InvalidOperationException ex)
            {
                // رسائل التحقق العربية الواضحة من الخدمة بتوصل للمستخدم زي ما هي
                MessageBox.Show(ex.Message, "راجع بيانات الرحلة", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    // ======================= نماذج عرض الرحلة =======================

    /// <summary>عامل مؤهل في قائمة اختيار عمال المرحلة</summary>
    public record WorkerPick(int WorkerId, string Name);

    /// <summary>
    /// بطاقة مرحلة واحدة في رحلة الإنتاج: بياناتها + عمالها المؤهلين +
    /// العمال المتوزعين عليها بأنصبتهم + إنتاجها المحسوب من النطاقات.
    /// زرار "＋ عامل" أمره على البطاقة نفسها (بيوصّل للرحلة عبر callback).
    /// </summary>
    public partial class FlowStageRow : ObservableObject
    {
        private readonly Action<FlowStageRow> _onAddWorker;

        public FlowStageRow(Action<FlowStageRow> onAddWorker) => _onAddWorker = onAddWorker;

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

        [RelayCommand]
        private void AddWorker() => _onAddWorker(this);
    }

    /// <summary>
    /// نصيب عامل واحد من قطع مرحلة (الخانة بتتملى تلقائي بالتساوي وتتعدل
    /// يدوي). زرار ✕ أمره على الشريحة نفسها (بيوصّل للرحلة عبر callback).
    /// </summary>
    public partial class FlowShareEntry : ObservableObject
    {
        private readonly Action _onEdited;
        private readonly Action<FlowShareEntry> _onRemove;

        public FlowShareEntry(FlowStageRow parent, int workerId, string workerName,
            Action onEdited, Action<FlowShareEntry> onRemove)
        {
            Parent = parent;
            WorkerId = workerId;
            WorkerName = workerName;
            _onEdited = onEdited;
            _onRemove = onRemove;
        }

        /// <summary>البطاقة الأم — عشان أمر الإزالة يعرف يشيل النصيب من مرحلته</summary>
        public FlowStageRow Parent { get; }
        public int WorkerId { get; }
        public string WorkerName { get; }

        [ObservableProperty]
        private string _sharePieces = "";

        partial void OnSharePiecesChanged(string value) => _onEdited();

        [RelayCommand]
        private void Remove() => _onRemove(this);
    }

    /// <summary>
    /// نطاق إنتاج واحد: من مرحلة إلى مرحلة بعدد قطع.
    /// زرار الحذف أمره على السطر نفسه (بيوصّل للرحلة عبر callback).
    /// </summary>
    public partial class FlowRangeRow : ObservableObject
    {
        private readonly Action _onEdited;
        private readonly Action<FlowRangeRow> _onRemove;

        public FlowRangeRow(List<StageEntryOption> stageOptions, Action onEdited, Action<FlowRangeRow> onRemove)
        {
            StageOptions = stageOptions;
            _onEdited = onEdited;
            _onRemove = onRemove;
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

        [RelayCommand]
        private void Remove() => _onRemove(this);
    }
}
