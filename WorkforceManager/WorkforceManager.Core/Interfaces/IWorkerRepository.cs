using WorkforceManager.Core.Models;

namespace WorkforceManager.Core.Interfaces
{
    public interface IWorkerRepository : IGenericRepository<Worker>
    {
        /// <summary>بحث بالاسم (جزء من الاسم) لإحضار العمال المطابقين مع مهاراتهم محمّلة</summary>
        Task<IReadOnlyList<Worker>> SearchByNameAsync(string nameQuery);

        /// <summary>إحضار عامل واحد مع كل مهاراته المرتبطة (المراحل التي يجيدها)</summary>
        Task<Worker?> GetWithSkillsAsync(int workerId);

        /// <summary>
        /// بحث بالمهارة/المرحلة: يرجع العمال النشطين اللي عندهم مهارة اسم
        /// مرحلتها أو اسم منتجها بيطابق الكلمة المكتوبة (مثال: "دبله" أو "GRS").
        /// أساس ميزة "اكتب اسم مرحلة، شوف مين بيعرف يعملها".
        /// </summary>
        Task<IReadOnlyList<Worker>> SearchBySkillAsync(string skillQuery);

        /// <summary>كل العمال النشطين بمهاراتهم — أساس شاشة العمال الرئيسية</summary>
        Task<IReadOnlyList<Worker>> GetActiveWithSkillsAsync();

        /// <summary>هل الكود الوظيفي مستخدم بالفعل لعامل آخر؟ (لمنع تكرار الأكواد عند الإضافة/التعديل)</summary>
        Task<bool> EmployeeCodeExistsAsync(string employeeCode, int? excludeWorkerId = null);

        /// <summary>العمال النشطون المؤهلون لمرحلة معينة (أساس شاشة الإدخال السريع لليوميات)</summary>
        Task<IReadOnlyList<Worker>> GetQualifiedForStageAsync(int productionStageId);
    }
}
