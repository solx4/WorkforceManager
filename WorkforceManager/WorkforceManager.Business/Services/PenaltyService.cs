using WorkforceManager.Core.Enums;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤولة عن تسجيل وإدارة الجزاءات: جزاء بسبب معين (شرب سجاير،
    /// لبس هاندفري أثناء الشغل، ...) وخصم محدد (نص يوم / يوم / 3 أيام /
    /// أسبوع). الجزاء مستقل عن الحضور — بيتسجل لعامل حاضر أو غايب عادي —
    /// وأثره الحسابي (الخصم من يوميات الأسبوع) بيتطبق في WeeklySummaryService.
    /// </summary>
    public class PenaltyService
    {
        private readonly IPenaltyRepository _penaltyRepo;

        public PenaltyService(IPenaltyRepository penaltyRepo)
        {
            _penaltyRepo = penaltyRepo;
        }

        /// <summary>يسجل جزاء جديد على عامل في تاريخ معين بسبب وخصم محددين</summary>
        public async Task<Penalty> RecordPenaltyAsync(
            int workerId, DateTime date, string reason, PenaltyDeduction deduction, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("سبب الجزاء مطلوب", nameof(reason));

            var penalty = new Penalty
            {
                WorkerId = workerId,
                Date = date.Date,
                Reason = reason.Trim(),
                Deduction = deduction,
                Notes = notes
            };

            await _penaltyRepo.AddAsync(penalty);
            await _penaltyRepo.SaveChangesAsync();
            return penalty;
        }

        /// <summary>يحذف جزاء مسجّل بالخطأ (حذف فعلي، مش Soft Delete، لأن الجزاء الغلط مالوش قيمة تاريخية)</summary>
        public async Task RemovePenaltyAsync(int penaltyId)
        {
            var penalty = await _penaltyRepo.GetByIdAsync(penaltyId)
                ?? throw new InvalidOperationException("الجزاء المحدد غير موجود");

            _penaltyRepo.Remove(penalty);
            await _penaltyRepo.SaveChangesAsync();
        }

        /// <summary>كل جزاءات عامل خلال فترة زمنية (لعرضها في بروفايله وتقريره)</summary>
        public Task<IReadOnlyList<Penalty>> GetWorkerPenaltiesAsync(int workerId, DateTime from, DateTime to)
            => _penaltyRepo.GetByWorkerAndRangeAsync(workerId, from, to);

        /// <summary>كل جزاءات يوم معين لكل العمال (لعرضها في شاشة التسجيل اليومي)</summary>
        public Task<IReadOnlyList<Penalty>> GetPenaltiesByDateAsync(DateTime date)
            => _penaltyRepo.GetByRangeAsync(date, date);
    }
}
