using Microsoft.EntityFrameworkCore;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Data.Repositories
{
    public class DailyProductionRepository : GenericRepository<DailyProduction>, IDailyProductionRepository
    {
        public DailyProductionRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<DailyProduction>> GetByDateAsync(DateTime date)
        {
            return await DbSet
                .Include(dp => dp.Worker)
                .Include(dp => dp.ProductionStage)
                    .ThenInclude(ps => ps.Product)
                .Where(dp => dp.Date.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<DailyProduction>> GetByWorkerAndRangeAsync(int workerId, DateTime from, DateTime to)
        {
            return await DbSet
                .Include(dp => dp.ProductionStage)
                    .ThenInclude(ps => ps.Product)
                .Where(dp => dp.WorkerId == workerId && dp.Date.Date >= from.Date && dp.Date.Date <= to.Date)
                .OrderBy(dp => dp.Date)
                .ToListAsync();
        }
    }
}
