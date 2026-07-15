using WorkforceManager.Core.Models;

namespace WorkforceManager.Core.Interfaces
{
    public interface IWorkerRepository : IGenericRepository<Worker>
    {
        /// <summary>بحث بالاسم (جزء من الاسم) لإحضار العمال المطابقين مع مهاراتهم محمّلة</summary>
        Task<IReadOnlyList<Worker>> SearchByNameAsync(string nameQuery);

        /// <summary>إحضار عامل واحد مع كل مهاراته المرتبطة (المراحل التي يجيدها)</summary>
        Task<Worker?> GetWithSkillsAsync(int workerId);
    }
}
