namespace WorkforceManager.Data
{
    /// <summary>
    /// المسارات الثابتة لملفات البرنامج على الجهاز — كلها في مكان واحد
    /// عشان أي شاشة أو خدمة تستخدم نفس المسار من غير تكرار أو اختلاف.
    ///
    /// وضعان:
    /// - عادي (تطوير/مثبت): البيانات في مجلد المستخدم %LocalAppData%\WorkforceManager
    ///   — منفصلة عن ملفات البرنامج فمتتأثرش لو البرنامج اتحدّث أو اتنقل.
    /// - محمول (Portable): لو فيه ملف "portable.marker" جنب الـ exe، البيانات
    ///   بتبقى في مجلد "Data" جنب البرنامج نفسه — فالمجلد كامل ومستقل، تنقله
    ///   لأي جهاز أو فلاشة يشتغل ببياناته من غير تثبيت.
    /// </summary>
    public static class AppPaths
    {
        private static readonly Lazy<string> _dataFolder = new(ResolveDataFolder);

        private static string ResolveDataFolder()
        {
            var exeDir = AppContext.BaseDirectory;
            var portableMarker = Path.Combine(exeDir, "portable.marker");
            if (File.Exists(portableMarker))
                return Path.Combine(exeDir, "Data"); // الوضع المحمول: البيانات جنب البرنامج

            // الوضع العادي: مجلد بيانات المستخدم (مش داخل مجلد البرنامج)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WorkforceManager");
        }

        /// <summary>مجلد بيانات البرنامج (يتحدد حسب الوضع: عادي أو محمول)</summary>
        public static string DataFolder => _dataFolder.Value;

        /// <summary>ملف قاعدة البيانات الرئيسي</summary>
        public static string DbPath => Path.Combine(DataFolder, "workforce.db");

        /// <summary>مجلد النسخ الاحتياطية المحلية (جنب قاعدة البيانات)</summary>
        public static string BackupsFolder => Path.Combine(DataFolder, "Backups");

        /// <summary>ملف إعدادات البرنامج (JSON)</summary>
        public static string SettingsPath => Path.Combine(DataFolder, "settings.json");
    }
}
