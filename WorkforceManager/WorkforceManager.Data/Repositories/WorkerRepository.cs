using Microsoft.EntityFrameworkCore;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Data.Repositories
{
    public class WorkerRepository : GenericRepository<Worker>, IWorkerRepository
    {
        public WorkerRepository(AppDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Worker>> SearchByNameAsync(string nameQuery)
        {
            var query = nameQuery.Trim();

            return await DbSet
                .Include(w => w.Skills)
                    .ThenInclude(s => s.ProductionStage)
                        .ThenInclude(ps => ps.Product)
                .Where(w => w.IsActive && EF.Functions.Like(w.FullName, $"%{query}%"))
                .OrderBy(w => w.FullName)
                .ToListAsync();
        }

        public async Task<Worker?> GetWithSkillsAsync(int workerId)
        {
            return await DbSet
                .Include(w => w.Skills)
                    .ThenInclude(s => s.ProductionStage)
                        .ThenInclude(ps => ps.Product)
                .FirstOrDefaultAsync(w => w.Id == workerId);
        }
    }
}
