using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤولة عن كل عمليات "الكتابة" على العمال: إضافة عامل جديد، تعديل
    /// بياناته، إيقافه (Soft Delete)، إعادة تفعيله، وإدارة مهاراته
    /// (ربطه/فك ربطه بمراحل الإنتاج) — القراءة والعرض مسؤولية الاستعلامات
    /// في IWorkerRepository مباشرة.
    /// </summary>
    public class WorkerManagementService
    {
        private readonly IWorkerRepository _workerRepo;
        private readonly IGenericRepository<WorkerSkill> _skillRepo;

        public WorkerManagementService(
            IWorkerRepository workerRepo,
            IGenericRepository<WorkerSkill> skillRepo)
        {
            _workerRepo = workerRepo;
            _skillRepo = skillRepo;
        }

        /// <summary>
        /// يضيف عامل جديد. الاسم إجباري، والكود الوظيفي (لو اتكتب) لازم
        /// يكون فريد — منع تكرار الأكواد بيمنع لخبطة كبيرة في التقارير.
        /// </summary>
        public async Task<Worker> CreateWorkerAsync(
            string fullName, string? employeeCode = null, string? phoneNumber = null,
            DateTime? hireDate = null, string? skillsNotes = null, HourlyRole? hourlyRole = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("اسم العامل مطلوب", nameof(fullName));

            if (!string.IsNullOrWhiteSpace(employeeCode) &&
                await _workerRepo.EmployeeCodeExistsAsync(employeeCode))
                throw new InvalidOperationException($"الكود الوظيفي '{employeeCode.Trim()}' مستخدم بالفعل لعامل آخر");

            var worker = new Worker
            {
                FullName = fullName.Trim(),
                EmployeeCode = string.IsNullOrWhiteSpace(employeeCode) ? null : employeeCode.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
                HireDate = hireDate,
                SkillsNotes = string.IsNullOrWhiteSpace(skillsNotes) ? null : skillsNotes.Trim(),
                HourlyRole = hourlyRole
            };

            await _workerRepo.AddAsync(worker);
            await _workerRepo.SaveChangesAsync();
            return worker;
        }

        /// <summary>يعدّل البيانات الأساسية لعامل موجود (نفس قواعد التحقق بتاعة الإضافة)</summary>
        public async Task<Worker> UpdateWorkerAsync(
            int workerId, string fullName, string? employeeCode = null,
            string? phoneNumber = null, DateTime? hireDate = null, string? skillsNotes = null,
            HourlyRole? hourlyRole = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("اسم العامل مطلوب", nameof(fullName));

            var worker = await _workerRepo.GetByIdAsync(workerId)
                ?? throw new InvalidOperationException("العامل المحدد غير موجود");

            // التحقق من تفرد الكود مع استثناء العامل نفسه (عشان حفظ التعديل من غير تغيير الكود ميترفضش)
            if (!string.IsNullOrWhiteSpace(employeeCode) &&
                await _workerRepo.EmployeeCodeExistsAsync(employeeCode, excludeWorkerId: workerId))
                throw new InvalidOperationException($"الكود الوظيفي '{employeeCode.Trim()}' مستخدم بالفعل لعامل آخر");

            worker.FullName = fullName.Trim();
            worker.EmployeeCode = string.IsNullOrWhiteSpace(employeeCode) ? null : employeeCode.Trim();
            worker.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            worker.HireDate = hireDate;
            worker.SkillsNotes = string.IsNullOrWhiteSpace(skillsNotes) ? null : skillsNotes.Trim();
            worker.HourlyRole = hourlyRole;

            _workerRepo.Update(worker);
            await _workerRepo.SaveChangesAsync();
            return worker;
        }

        /// <summary>
        /// إيقاف عامل (Soft Delete): بيختفي من القوائم النشطة لكن كل
        /// سجلاته التاريخية (إنتاج/حضور/جزاءات) بتفضل محفوظة — القرار
        /// المتفق عليه بدل الحذف النهائي.
        /// </summary>
        public async Task DeactivateWorkerAsync(int workerId)
        {
            var worker = await _workerRepo.GetByIdAsync(workerId)
                ?? throw new InvalidOperationException("العامل المحدد غير موجود");

            worker.IsActive = false;
            _workerRepo.Update(worker);
            await _workerRepo.SaveChangesAsync();
        }

        /// <summary>إعادة تفعيل عامل موقوف (رجع للشغل مثلاً) — سجله القديم كله بيرجع معاه</summary>
        public async Task ReactivateWorkerAsync(int workerId)
        {
            var worker = await _workerRepo.GetByIdAsync(workerId)
                ?? throw new InvalidOperationException("العامل المحدد غير موجود");

            worker.IsActive = true;
            _workerRepo.Update(worker);
            await _workerRepo.SaveChangesAsync();
        }

        /// <summary>
        /// يضيف مهارة لعامل (يربطه بمرحلة إنتاج معينة بمستوى إتقان).
        /// لو المهارة موجودة بالفعل بيحدّث مستوى الإتقان بس بدل ما يرمي
        /// خطأ — أسهل في الاستخدام من الواجهة.
        /// </summary>
        public async Task<WorkerSkill> AssignSkillAsync(
            int workerId, int productionStageId, SkillLevel level = SkillLevel.Proficient)
        {
            var existing = (await _skillRepo.FindAsync(
                s => s.WorkerId == workerId && s.ProductionStageId == productionStageId))
                .FirstOrDefault();

            if (existing is not null)
            {
                existing.Level = level;
                _skillRepo.Update(existing);
                await _skillRepo.SaveChangesAsync();
                return existing;
            }

            var skill = new WorkerSkill
            {
                WorkerId = workerId,
                ProductionStageId = productionStageId,
                Level = level
            };

            await _skillRepo.AddAsync(skill);
            await _skillRepo.SaveChangesAsync();
            return skill;
        }

        /// <summary>يشيل مهارة من عامل (حذف فعلي لسطر الربط — مش بيأثر على سجلات الإنتاج التاريخية)</summary>
        public async Task RemoveSkillAsync(int workerId, int productionStageId)
        {
            var existing = (await _skillRepo.FindAsync(
                s => s.WorkerId == workerId && s.ProductionStageId == productionStageId))
                .FirstOrDefault()
                ?? throw new InvalidOperationException("المهارة المحددة غير مرتبطة بهذا العامل");

            _skillRepo.Remove(existing);
            await _skillRepo.SaveChangesAsync();
        }
    }
}
