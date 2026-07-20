namespace WorkforceManager.Core.Enums
{
    /// <summary>
    /// دور العامل اللي بيتحاسب بالساعة (مش بالقطعة). العامل اللي ليه
    /// دور من دول بيتحسب أجره بعدد الساعات مش بالإنتاج — زي عمال الرص
    /// والجودة والتدريب اللي بيشتغلوا شيفت (8 ساعات = يومية) مش على
    /// مراحل إنتاج محددة.
    /// </summary>
    public enum HourlyRole
    {
        /// <summary>عامل تحت التدريب</summary>
        Training = 1,

        /// <summary>عامل رص</summary>
        Racking = 2,

        /// <summary>عامل جودة</summary>
        Quality = 3,

        /// <summary>دور آخر بالساعة (يُوضّح في ملاحظة العامل)</summary>
        Other = 4
    }

    /// <summary>الاسم العربي المعروض لكل دور بالساعة</summary>
    public static class HourlyRoleExtensions
    {
        public static string ToArabicName(this HourlyRole role) => role switch
        {
            HourlyRole.Training => "عامل تحت التدريب",
            HourlyRole.Racking => "عامل رص",
            HourlyRole.Quality => "عامل جودة",
            HourlyRole.Other => "دور آخر (بالساعة)",
            _ => "بالساعة"
        };
    }
}
