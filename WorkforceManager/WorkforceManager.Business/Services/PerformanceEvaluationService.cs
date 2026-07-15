using WorkforceManager.Business.DTOs;
using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤول عن تقييم أداء العمال في يوم معين، بمقارنة عدد اليوميات
    /// المنجزة لكل عامل بمتوسط زملائه في نفس اليوم، مع الأخذ في
    /// الاعتبار حالة حضوره: الغياب بدون إذن بيظهر في التقييم كأسوأ
    /// تصنيف بغض النظر عن أي إنتاج، والغياب بإذن بيتحسب كحالة محايدة.
    /// </summary>
    public class PerformanceEvaluationService
    {
        private readonly IDailyProductionRepository _productionRepo;
        private readonly IAttendanceRepository _attendanceRepo;

        // حدود نسبية لتصنيف الأداء مقارنة بالمتوسط (قابلة للتعديل لاحقًا لو العميل عايز حساسية مختلفة)
        private const double TopPerformerThreshold = 20.0;   // أعلى من المتوسط بـ 20% أو أكثر
        private const double AboveAverageThreshold = 5.0;    // أعلى من المتوسط بـ 5% إلى 20%
        private const double BelowAverageThreshold = -10.0;  // أقل من المتوسط بـ 10% أو أكثر

        public PerformanceEvaluationService(IDailyProductionRepository productionRepo, IAttendanceRepository attendanceRepo)
        {
            _productionRepo = productionRepo;
            _attendanceRepo = attendanceRepo;
        }

        /// <summary>
        /// يبني تقييم كل عامل له نشاط في تاريخ معين (إنتاج أو حضور مسجّل)،
        /// بمقارنته بمتوسط عدد يوميات زملائه اللي أنتجوا في نفس اليوم فقط.
        /// </summary>
        public async Task<List<WorkerDailySummaryDto>> EvaluateDayAsync(DateTime date)
        {
            var productionRecords = await _productionRepo.GetByDateAsync(date);
            var attendanceRecords = await _attendanceRepo.GetByDateAsync(date);

            if (productionRecords.Count == 0 && attendanceRecords.Count == 0)
                return new List<WorkerDailySummaryDto>();

            var attendanceByWorker = attendanceRecords.ToDictionary(a => a.WorkerId, a => a.Status);

            // 1) العمال اللي عندهم إنتاج فعلي في اليوم ده
            var perWorker = productionRecords
                .GroupBy(r => new { r.WorkerId, r.Worker.FullName })
                .Select(g => new WorkerDailySummaryDto
                {
                    WorkerId = g.Key.WorkerId,
                    WorkerName = g.Key.FullName,
                    Date = date.Date,
                    TotalPieces = g.Sum(r => r.PieceCount),
                    TotalWorkdays = g.Sum(r => r.WorkdaysCompleted),
                    AttendanceStatus = attendanceByWorker.TryGetValue(g.Key.WorkerId, out var st) ? st : null,
                    Breakdown = g.Select(r => new StageBreakdownDto
                    {
                        ProductName = r.ProductionStage.Product.Name,
                        StageName = r.ProductionStage.StageName,
                        PieceCount = r.PieceCount,
                        PiecesPerWorkday = r.PiecesPerWorkdayAtEntry
                    }).ToList()
                })
                .ToList();

            // 2) العمال اللي مسجّل حضورهم (غياب عادة) لكن معندهمش إنتاج — لازم يظهروا برضه
            var workersWithProduction = perWorker.Select(w => w.WorkerId).ToHashSet();
            foreach (var absentee in attendanceRecords.Where(a => !workersWithProduction.Contains(a.WorkerId)))
            {
                perWorker.Add(new WorkerDailySummaryDto
                {
                    WorkerId = absentee.WorkerId,
                    WorkerName = absentee.Worker.FullName,
                    Date = date.Date,
                    TotalPieces = 0,
                    TotalWorkdays = 0,
                    AttendanceStatus = absentee.Status
                });
            }

            // متوسط اليوميات بيتحسب على أساس اللي أنتجوا فعليًا فقط (منطقي أكتر من ضم الغائبين بصفر)
            var producingWorkers = perWorker.Where(w => w.TotalPieces > 0).ToList();
            var teamAverage = producingWorkers.Count > 0 ? producingWorkers.Average(w => w.TotalWorkdays) : 0;

            foreach (var worker in perWorker)
            {
                worker.TeamAverageWorkdays = teamAverage;
                worker.Rating = ClassifyPerformance(worker, producingWorkers);
            }

            return perWorker.OrderByDescending(w => w.TotalWorkdays).ToList();
        }

        private static PerformanceRating ClassifyPerformance(
            WorkerDailySummaryDto worker, List<WorkerDailySummaryDto> producingWorkers)
        {
            // الغياب بدون إذن = أسوأ تصنيف دايمًا، بغض النظر عن أي حاجة تانية
            if (worker.AttendanceStatus == AttendanceStatus.AbsentWithoutPermission)
                return PerformanceRating.UnexcusedAbsence;

            // الغياب بإذن = حالة محايدة، مش بيتحاسب كتقصير في الأداء
            if (worker.AttendanceStatus == AttendanceStatus.AbsentWithPermission)
                return PerformanceRating.Average;

            if (worker.TotalPieces == 0) return PerformanceRating.Average;

            var isTop = producingWorkers.Count > 0 && producingWorkers.All(w => w.TotalWorkdays <= worker.TotalWorkdays);

            if (isTop && worker.PercentVsAverage >= TopPerformerThreshold) return PerformanceRating.TopPerformer;
            if (worker.PercentVsAverage >= AboveAverageThreshold) return PerformanceRating.AboveAverage;
            if (worker.PercentVsAverage <= BelowAverageThreshold) return PerformanceRating.BelowAverage;
            return PerformanceRating.Average;
        }
    }
}
