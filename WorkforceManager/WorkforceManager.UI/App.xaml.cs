using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkforceManager.Business.Services;
using WorkforceManager.Core.Interfaces;
using WorkforceManager.Core.Models;
using WorkforceManager.Data;
using WorkforceManager.Data.Repositories;

namespace WorkforceManager.UI
{
    public partial class App : Application
    {
        /// <summary>
        /// مضيف التطبيق (Host) اللي بيدير كل الاعتماديات (Dependency Injection).
        /// ده اللي بيخلي كل طبقة (UI / Business / Data) منفصلة ومربوطة ببعض
        /// بشكل نظيف من غير ما أي طبقة تعمل "new" لطبقة تانية يدويًا.
        /// </summary>
        public static IHost AppHost { get; private set; } = null!;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // مسار قاعدة البيانات: مجلد بيانات التطبيق الخاص بالمستخدم (مش داخل مجلد البرنامج نفسه)
                    var dbFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "WorkforceManager");
                    Directory.CreateDirectory(dbFolder);
                    var dbPath = Path.Combine(dbFolder, "workforce.db");

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));

                    // Repositories
                    services.AddScoped<IWorkerRepository, WorkerRepository>();
                    services.AddScoped<IProductRepository, ProductRepository>();
                    services.AddScoped<IDailyProductionRepository, DailyProductionRepository>();
                    services.AddScoped<IAttendanceRepository, AttendanceRepository>();
                    services.AddScoped<IPenaltyRepository, PenaltyRepository>();
                    services.AddScoped<IGenericRepository<ProductionStage>, GenericRepository<ProductionStage>>();
                    services.AddScoped<IGenericRepository<WorkerSkill>, GenericRepository<WorkerSkill>>();

                    // Business Services
                    services.AddScoped<WorkdayCalculationService>();
                    services.AddScoped<PerformanceEvaluationService>();
                    services.AddScoped<AttendanceService>();
                    services.AddScoped<WorkerProfileService>();
                    services.AddScoped<PenaltyService>();
                    services.AddScoped<WeeklySummaryService>();
                    services.AddScoped<WorkerManagementService>();

                    // Windows / Views
                    services.AddSingleton<MainWindow>();
                    // الشاشات الداخلية Transient: نسخة جديدة نظيفة مع كل تنقّل
                    services.AddTransient<Views.WorkersView>();
                    services.AddTransient<ViewModels.WorkersViewModel>();
                    services.AddTransient<Views.DailyEntryView>();
                    services.AddTransient<ViewModels.DailyEntryViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            using (var scope = AppHost.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // نسخة احتياطية يومية قبل أي تعديل على قاعدة البيانات، عشان لو
                // الـ Migration فشل لأي سبب تفضل عندنا نسخة سليمة من قبل التعديل
                var dbPath = db.Database.GetDbConnection().DataSource;
                DatabaseBackupService.RunDailyBackup(dbPath);

                // تطبيق أي Migration جديدة تلقائيًا (بيُنشئ قاعدة البيانات من الصفر
                // لو مش موجودة أصلاً) — بديل EnsureCreatedAsync عشان تحديثات
                // النماذج المستقبلية تتطبق على قاعدة بيانات العميل الحالية من
                // غير ما نمسح بياناته
                await db.Database.MigrateAsync();
                await DatabaseSeeder.SeedIfEmptyAsync(db);
            }

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
