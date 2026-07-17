using System.Globalization;

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

        /// <summary>بادئة اسم ملف النسخة الاحتياطية (يتبعها التاريخ بصيغة yyyy-MM-dd)</summary>
        private const string BackupPrefix = "workforce_";

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

            var todayBackupPath = Path.Combine(backupsFolder, $"{BackupPrefix}{DateTime.Today:yyyy-MM-dd}.db");
            if (!File.Exists(todayBackupPath))
            {
                File.Copy(dbPath, todayBackupPath);
            }

            CleanupOldBackups(backupsFolder);
        }

        /// <summary>
        /// بيمسح أي نسخة احتياطية أقدم من فترة الاحتفاظ المحددة (RetentionDays).
        /// بيعتمد على التاريخ المكتوب في اسم الملف نفسه (workforce_yyyy-MM-dd.db)
        /// مش على File.GetCreationTime — لأن تاريخ إنشاء الملف على ويندوز غير
        /// موثوق (بيتغير عند النسخ/الاستعادة، وفيه ظاهرة File System Tunneling
        /// اللي بتخلي ملف جديد يورث تاريخ ملف قديم بنفس الاسم).
        /// </summary>
        private static void CleanupOldBackups(string backupsFolder)
        {
            var cutoffDate = DateTime.Today.AddDays(-RetentionDays);

            foreach (var file in Directory.GetFiles(backupsFolder, $"{BackupPrefix}*.db"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var datePart = fileName.Substring(BackupPrefix.Length); // الجزء بعد البادئة = التاريخ

                // أي ملف اسمه مش متطابق مع الصيغة المتوقعة بنسيبه (مش بنمسحه) احتياطًا
                if (DateTime.TryParseExact(datePart, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var backupDate)
                    && backupDate < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
    }
}
