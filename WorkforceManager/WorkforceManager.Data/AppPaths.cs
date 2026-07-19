namespace WorkforceManager.Data
{
    /// <summary>
    /// المسارات الثابتة لملفات البرنامج على الجهاز — كلها في مكان واحد
    /// عشان أي شاشة أو خدمة تستخدم نفس المسار من غير تكرار أو اختلاف.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>مجلد بيانات البرنامج الخاص بالمستخدم (مش داخل مجلد البرنامج نفسه)</summary>
        public static string DataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WorkforceManager");

        /// <summary>ملف قاعدة البيانات الرئيسي</summary>
        public static string DbPath => Path.Combine(DataFolder, "workforce.db");

        /// <summary>مجلد النسخ الاحتياطية المحلية (جنب قاعدة البيانات)</summary>
        public static string BackupsFolder => Path.Combine(DataFolder, "Backups");

        /// <summary>ملف إعدادات البرنامج (JSON)</summary>
        public static string SettingsPath => Path.Combine(DataFolder, "settings.json");
    }
}
