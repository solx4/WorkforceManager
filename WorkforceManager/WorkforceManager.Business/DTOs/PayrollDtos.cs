namespace WorkforceManager.Business.DTOs
{
    /// <summary>
    /// أجر عامل واحد عن فترة زمنية مخصصة (شهر مثلاً). بيجمّع كل الأيام في
    /// المدى مباشرة (مش أسابيع كاملة): إنتاج + شغل بالساعة − خصم الغياب
    /// بدون إذن − خصم الجزاءات، × سعر اليومية = الأجر النهائي بالجنيه.
    /// </summary>
    public class WorkerPayrollDto
    {
        public int WorkerId { get; init; }
        public string WorkerName { get; init; } = string.Empty;
        public string? EmployeeCode { get; init; }
        public bool IsHourly { get; init; }

        /// <summary>سعر اليومية بالجنيه (الحالي)</summary>
        public decimal DailyWageEgp { get; init; }

        /// <summary>يوميات الإنتاج والشغل بالساعة خلال الفترة (قبل الخصومات)</summary>
        public decimal ProducedWorkdays { get; init; }

        /// <summary>خصم الغياب بدون إذن باليوميات</summary>
        public decimal AbsenceDeduction { get; init; }

        /// <summary>خصم الجزاءات باليوميات</summary>
        public decimal PenaltyDeduction { get; init; }

        /// <summary>عدد أيام العمل الفعلية (اللي فيها إنتاج أو شغل ساعة)</summary>
        public int DaysWorked { get; init; }

        /// <summary>صافي يوميات الفترة = المنتج − الخصومات</summary>
        public decimal NetWorkdays => ProducedWorkdays - AbsenceDeduction - PenaltyDeduction;

        /// <summary>الأجر النهائي بالجنيه = صافي اليوميات × سعر اليومية</summary>
        public decimal NetWageEgp => NetWorkdays * DailyWageEgp;
    }

    /// <summary>ملخص كشف أجور فترة (كل العمال + الإجماليات)</summary>
    public class PeriodPayrollDto
    {
        public DateTime From { get; init; }
        public DateTime To { get; init; }

        /// <summary>كل العمال اللي لهم نشاط في الفترة، مرتبين بالأجر تنازليًا</summary>
        public List<WorkerPayrollDto> Workers { get; init; } = new();

        /// <summary>إجمالي أجور كل العمال في الفترة</summary>
        public decimal TotalWageEgp => Workers.Sum(w => w.NetWageEgp);

        /// <summary>إجمالي صافي اليوميات لكل العمال</summary>
        public decimal TotalNetWorkdays => Workers.Sum(w => w.NetWorkdays);
    }
}
