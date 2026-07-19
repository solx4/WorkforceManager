using System.Security.Cryptography;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Business.Services
{
    /// <summary>
    /// مسؤولة عن تسجيل الدخول وإدارة كلمات المرور. كلمة المرور عمرها ما
    /// بتتخزن كنص — بنخزن ناتج تشفيرها بخوارزمية PBKDF2 (تكرار عالي +
    /// ملح عشوائي لكل مستخدم)، وهي نفس الطريقة المعتمدة في الأنظمة
    /// الاحترافية: حتى لو حد وصل لملف قاعدة البيانات مش هيعرف كلمات المرور.
    /// </summary>
    public class AuthService
    {
        /// <summary>بيانات الدخول الافتراضية لأول تشغيل (لازم تتغير بعد أول دخول)</summary>
        public const string DefaultUsername = "admin";
        public const string DefaultPassword = "admin";

        /// <summary>عدد تكرارات التشفير — رقم عالي بيخلي تخمين كلمة المرور بطيء جدًا</summary>
        private const int Pbkdf2Iterations = 100_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        private readonly IGenericRepository<AppUser> _users;

        public AuthService(IGenericRepository<AppUser> users)
        {
            _users = users;
        }

        /// <summary>
        /// أول تشغيل للبرنامج: لو مفيش أي مستخدمين، بينشئ حساب المدير
        /// الافتراضي (admin / admin) عشان الدخول ميتقفلش قدام المستخدم.
        /// </summary>
        public async Task EnsureDefaultUserAsync()
        {
            var users = await _users.GetAllAsync();
            if (users.Count > 0) return;

            var (hash, salt) = HashPassword(DefaultPassword);
            await _users.AddAsync(new AppUser
            {
                Username = DefaultUsername,
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = "مدير القسم"
            });
            await _users.SaveChangesAsync();
        }

        /// <summary>
        /// يتحقق من بيانات الدخول: بيرجع المستخدم لو صحيحة، أو null لو
        /// غلط — من غير ما يفرّق في الرسالة بين "الاسم غلط" و"الباسورد
        /// غلط" (معلومة زيادة للمتطفلين).
        /// </summary>
        public async Task<AppUser?> ValidateLoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            var trimmed = username.Trim();
            var user = (await _users.FindAsync(u => u.Username == trimmed)).FirstOrDefault();
            if (user is null) return null;

            return VerifyPassword(password, user.PasswordHash, user.PasswordSalt) ? user : null;
        }

        /// <summary>
        /// يغيّر كلمة مرور مستخدم بعد التحقق من كلمته الحالية.
        /// بيرمي استثناء برسالة واضحة لو الحالية غلط أو الجديدة ضعيفة.
        /// </summary>
        public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور الجديدة لازم تكون 4 حروف/أرقام على الأقل");

            var user = await ValidateLoginAsync(username, currentPassword)
                ?? throw new InvalidOperationException("اسم المستخدم أو كلمة المرور الحالية غير صحيحة");

            // ملح جديد مع كل تغيير — الـ Hash القديم بيبقى ملوش أي قيمة
            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            _users.Update(user);
            await _users.SaveChangesAsync();
        }

        // ------- التشفير (PBKDF2-SHA256) -------

        /// <summary>يشفّر كلمة مرور بملح عشوائي جديد، ويرجع الاتنين Base64 للتخزين</summary>
        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSize);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        /// <summary>يعيد حساب الـ Hash بنفس ملح المستخدم ويقارن بمقارنة ثابتة الوقت</summary>
        private static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var computed = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSize);

            // مقارنة ثابتة الوقت — بتمنع استنتاج كلمة المرور من زمن المقارنة
            return CryptographicOperations.FixedTimeEquals(computed, Convert.FromBase64String(storedHash));
        }
    }
}
