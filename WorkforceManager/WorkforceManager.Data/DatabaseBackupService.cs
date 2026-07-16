namespace WorkforceManager.Data
{
    /// <summary>
    /// مسؤولة عن أخذ نسخة احتياطية يومية تلقائية من ملف قاعدة البيانات (SQLite ملف
    /// واحد، فالنسخ الاحتياطي هنا ببساطة نسخ الملف)، وحذف النسخ القديمة تلقائيًا
    /// حفاظًا على مساحة القرص. بتتنفذ عند بدء التطبيق، قبل أي Migration، عشان لو
    /// حصل خطأ أثناء تحديث قاعدة البيانات تفضل عندنا نسخة سليمة قبل التعديل.
    /// </summary>
    public static class DatabaseBackupService
    {
        /// <summary>عدد أيام الاحتفاظ بالنسخ الاحتياطية قبل حذف الأقدم منها تلقائيًا</summary>
        private const int RetentionDays = 30;

        /// <summary>
        /// بياخد نسخة من قاعدة البيانات بتاريخ النهاردة لو مفيش نسخة اتاخدت
        /// بالفعل النهاردة (منعًا من تكرار نفس النسخة كذا مرة في نفس اليوم لو
        /// المستخدم قفل وفتح البرنامج أكتر من مرة).
        /// </summary>
        public static void RunDailyBackup(string dbPath)
        {
            if (!File.Exists(dbPath))
                return; // أول تشغيل للتطبيق: قاعدة البيانات لسه ما اتعملتش، مفيش حاجة نعمل لها باك أب

            var backupsFolder = Path.Combine(Path.GetDirectoryName(dbPath)!, "Backups");
            Directory.CreateDirectory(backupsFolder);

            var todayBackupPath = Path.Combine(backupsFolder, $"workforce_{DateTime.Today:yyyy-MM-dd}.db");
            if (!File.Exists(todayBackupPath))
            {
                File.Copy(dbPath, todayBackupPath);
            }

            CleanupOldBackups(backupsFolder);
        }

        /// <summary>بيمسح أي نسخة احتياطية أقدم من فترة الاحتفاظ المحددة (RetentionDays)</summary>
        private static void CleanupOldBackups(string backupsFolder)
        {
            var cutoffDate = DateTime.Today.AddDays(-RetentionDays);

            foreach (var file in Directory.GetFiles(backupsFolder, "workforce_*.db"))
            {
                if (File.GetCreationTime(file) < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
    }
}
