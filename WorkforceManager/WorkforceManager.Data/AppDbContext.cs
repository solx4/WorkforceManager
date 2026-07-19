using Microsoft.EntityFrameworkCore;
using WorkforceManager.Core.Models;

namespace WorkforceManager.Data
{
    /// <summary>
    /// نقطة الاتصال الوحيدة بقاعدة البيانات. كل الاستعلامات في المشروع
    /// بتمر من هنا عن طريق الـ Repositories، مفيش أي طبقة تانية بتتكلم
    /// مع SQLite مباشرة — ده بيضمن سهولة الصيانة والاختبار.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Worker> Workers => Set<Worker>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductionStage> ProductionStages => Set<ProductionStage>();
        public DbSet<WorkerSkill> WorkerSkills => Set<WorkerSkill>();
        public DbSet<DailyProduction> DailyProductions => Set<DailyProduction>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Penalty> Penalties => Set<Penalty>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Product -> ProductionStage (1-to-many) ----------
            modelBuilder.Entity<ProductionStage>()
                .HasOne(s => s.Product)
                .WithMany(p => p.Stages)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // حذف منتج يحذف مراحله (منطقي، مفيش مرحلة من غير منتج)

            // ---------- WorkerSkill: Worker <-> ProductionStage (many-to-many عبر جدول ربط) ----------
            modelBuilder.Entity<WorkerSkill>()
                .HasOne(ws => ws.Worker)
                .WithMany(w => w.Skills)
                .HasForeignKey(ws => ws.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkerSkill>()
                .HasOne(ws => ws.ProductionStage)
                .WithMany(s => s.QualifiedWorkers)
                .HasForeignKey(ws => ws.ProductionStageId)
                .OnDelete(DeleteBehavior.Cascade);

            // منع تكرار نفس المهارة لنفس العامل مرتين
            modelBuilder.Entity<WorkerSkill>()
                .HasIndex(ws => new { ws.WorkerId, ws.ProductionStageId })
                .IsUnique();

            // ---------- DailyProduction: Worker + ProductionStage ----------
            modelBuilder.Entity<DailyProduction>()
                .HasOne(dp => dp.Worker)
                .WithMany(w => w.ProductionRecords)
                .HasForeignKey(dp => dp.WorkerId)
                .OnDelete(DeleteBehavior.Restrict); // منع حذف عامل له سجلات إنتاج تاريخية (حماية البيانات)

            modelBuilder.Entity<DailyProduction>()
                .HasOne(dp => dp.ProductionStage)
                .WithMany(s => s.ProductionRecords)
                .HasForeignKey(dp => dp.ProductionStageId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- Attendance: Worker (1-to-many) ----------
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Worker)
                .WithMany(w => w.AttendanceRecords)
                .HasForeignKey(a => a.WorkerId)
                .OnDelete(DeleteBehavior.Cascade); // حذف عامل (نادر، عادة بيتعمل له IsActive=false) يحذف سجلات حضوره

            // ---------- Penalty: Worker (1-to-many) ----------
            modelBuilder.Entity<Penalty>()
                .HasOne(p => p.Worker)
                .WithMany(w => w.Penalties)
                .HasForeignKey(p => p.WorkerId)
                .OnDelete(DeleteBehavior.Cascade); // نفس قاعدة الحضور: حذف عامل (نادر) يحذف جزاءاته

            // ---------- AppUser: اسم المستخدم فريد (مفيش حسابين بنفس الاسم) ----------
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // ---------- فهارس لتسريع البحث بالاسم ----------
            modelBuilder.Entity<Worker>()
                .HasIndex(w => w.FullName);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);
        }
    }
}
