using Microsoft.EntityFrameworkCore;
using WorkforceManager.Data.Seed;

namespace WorkforceManager.Data
{
    /// <summary>
    /// بيتشغل مرة واحدة بس، أول ما التطبيق يفتح ويلاقي قاعدة البيانات
    /// فاضية: بيزرع فيها بيانات العميل الحقيقية (17 منتج بمراحلها
    /// وكوتاتها من Salem.xlsx، و46 عامل بأسمائهم من ملف اسماء الصنفرة).
    /// لو قاعدة البيانات فيها بيانات بالفعل، بيتخطى العملية تمامًا
    /// (منعًا لتكرار البيانات في كل تشغيل).
    /// </summary>
    public static class DatabaseSeeder
    {
        public static async Task SeedIfEmptyAsync(AppDbContext db)
        {
            var hasProducts = await db.Products.AnyAsync();
            var hasWorkers = await db.Workers.AnyAsync();

            if (!hasProducts)
            {
                var products = RealDataSeed.BuildProducts();
                await db.Products.AddRangeAsync(products);
            }

            if (!hasWorkers)
            {
                var workers = RealDataSeed.BuildWorkers();
                await db.Workers.AddRangeAsync(workers);
            }

            if (!hasProducts || !hasWorkers)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
