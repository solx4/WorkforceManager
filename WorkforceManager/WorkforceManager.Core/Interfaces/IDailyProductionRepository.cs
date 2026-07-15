using WorkforceManager.Core.Models;

namespace WorkforceManager.Core.Interfaces
{
    public interface IDailyProductionRepository : IGenericRepository<DailyProduction>
    {
        /// <summary>كل سجلات الإنتاج في تاريخ معين (لحساب أجور اليوم وتقييم كل العمال)</summary>
        Task<IReadOnlyList<DailyProduction>> GetByDateAsync(DateTime date);

        /// <summary>كل سجلات عامل معين خلال فترة زمنية (لعرض تاريخه وأداءه)</summary>
        Task<IReadOnlyList<DailyProduction>> GetByWorkerAndRangeAsync(int workerId, DateTime from, DateTime to);
    }
}
