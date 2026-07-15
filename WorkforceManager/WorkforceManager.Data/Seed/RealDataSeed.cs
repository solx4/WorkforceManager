// ملف مولّد تلقائيًا من بيانات العميل الحقيقية (Salem.xlsx + اسماء الصنفرة)
// لا تعدل يدويًا — أعد توليده من ملفات الإكسل الأصلية لو البيانات اتغيرت
using WorkforceManager.Core.Models;

namespace WorkforceManager.Data.Seed
{
    public static class RealDataSeed
    {
        public static List<Product> BuildProducts()
        {
            var products = new List<Product>();

            products.Add(new Product { Name = "GRS", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "دبله", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "رقبه", PiecesPerWorkday = 5000, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه صغيره", PiecesPerWorkday = 5000, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "دبله قدام", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "بطن خشن", PiecesPerWorkday = 5000, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "ضربتين", PiecesPerWorkday = 3333, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "بطحتين", PiecesPerWorkday = 2500, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "لفه 400", PiecesPerWorkday = 5000, SortOrder = 7, IsActive = true },
                new ProductionStage { StageName = "بطن ناعم", PiecesPerWorkday = 5000, SortOrder = 8, IsActive = true },
                new ProductionStage { StageName = "لفه 600", PiecesPerWorkday = 5000, SortOrder = 9, IsActive = true },
                new ProductionStage { StageName = "لفه 800", PiecesPerWorkday = 5000, SortOrder = 10, IsActive = true },
            }});
            products.Add(new Product { Name = "MG", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش بطن", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "دبله", PiecesPerWorkday = 5000, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "ضربه", PiecesPerWorkday = 5000, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "رقبه", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لفه 400", PiecesPerWorkday = 5000, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "ديق 600", PiecesPerWorkday = 3333, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "لفه 800", PiecesPerWorkday = 5000, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "ديق 800", PiecesPerWorkday = 3333, SortOrder = 7, IsActive = true },
                new ProductionStage { StageName = "عريض 800", PiecesPerWorkday = 5000, SortOrder = 8, IsActive = true },
                new ProductionStage { StageName = "بطن 400", PiecesPerWorkday = 5000, SortOrder = 9, IsActive = true },
                new ProductionStage { StageName = "بطن 600", PiecesPerWorkday = 5000, SortOrder = 10, IsActive = true },
                new ProductionStage { StageName = "بطن 800", PiecesPerWorkday = 5000, SortOrder = 11, IsActive = true },
            }});
            products.Add(new Product { Name = "ماكس فتيل", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رقبه", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "ضربتين", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "ديق", PiecesPerWorkday = 3333, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "بطن خشن", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لفه 400", PiecesPerWorkday = 5000, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "دبله 600", PiecesPerWorkday = 5000, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "بطن ناعم", PiecesPerWorkday = 5000, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "لقه 600", PiecesPerWorkday = 5000, SortOrder = 7, IsActive = true },
                new ProductionStage { StageName = "لفه 800", PiecesPerWorkday = 5000, SortOrder = 8, IsActive = true },
            }});
            products.Add(new Product { Name = "ماجيك", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 2500, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "بطحتين 400", PiecesPerWorkday = 2000, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "رقبه صغيره خشن", PiecesPerWorkday = 3333, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "رقبه كبيره خشن", PiecesPerWorkday = 3333, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لفه صغيره 600", PiecesPerWorkday = 5000, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "بطن خشن", PiecesPerWorkday = 5000, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "رقبه ضربتين", PiecesPerWorkday = 2500, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "بطحتين ناعم", PiecesPerWorkday = 2000, SortOrder = 7, IsActive = true },
                new ProductionStage { StageName = "رقبه صغيره ناعم", PiecesPerWorkday = 3333, SortOrder = 8, IsActive = true },
                new ProductionStage { StageName = "رقبه كبيره ناعم", PiecesPerWorkday = 3333, SortOrder = 9, IsActive = true },
                new ProductionStage { StageName = "رقبه صغيره ديق", PiecesPerWorkday = 2500, SortOrder = 10, IsActive = true },
                new ProductionStage { StageName = "رقبه ناعم ديق", PiecesPerWorkday = 2500, SortOrder = 11, IsActive = true },
                new ProductionStage { StageName = "بطن ناعم", PiecesPerWorkday = 5000, SortOrder = 12, IsActive = true },
                new ProductionStage { StageName = "لفه ناعم", PiecesPerWorkday = 5000, SortOrder = 13, IsActive = true },
            }});
            products.Add(new Product { Name = "طقم عقله", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "600", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "800", PiecesPerWorkday = 5000, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "1000", PiecesPerWorkday = 5000, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "1000", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لمعه", PiecesPerWorkday = 5000, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "تربيط", PiecesPerWorkday = 5000, SortOrder = 5, IsActive = true },
            }});
            products.Add(new Product { Name = "كوع بسن", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "عريض خشن", PiecesPerWorkday = 2600, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "ديق خشن", PiecesPerWorkday = 2600, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "عريض ناعم", PiecesPerWorkday = 2600, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "ديق ناعم", PiecesPerWorkday = 2600, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "عريض لمعه", PiecesPerWorkday = 1666, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "وصله تي", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "بطن خشن", PiecesPerWorkday = 3333, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "3 لفات خشن", PiecesPerWorkday = 1666, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "ديق خشن", PiecesPerWorkday = 1666, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "3 لفات ناعم", PiecesPerWorkday = 1666, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "ديق ناعم", PiecesPerWorkday = 1666, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "بطن 600", PiecesPerWorkday = 5000, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "بطن 800", PiecesPerWorkday = 5000, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "بطن 1000", PiecesPerWorkday = 5000, SortOrder = 7, IsActive = true },
            }});
            products.Add(new Product { Name = "حنفيه بزبوز", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "مسدس", PiecesPerWorkday = 1666, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "ضربتين ولفه", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "ديق رقبه", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "ديق بوز", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "بطن", PiecesPerWorkday = 2500, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "عريض", PiecesPerWorkday = 2500, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "بوز", PiecesPerWorkday = 2500, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "لفه لمعه", PiecesPerWorkday = 1666, SortOrder = 7, IsActive = true },
                new ProductionStage { StageName = "عريض لمعه", PiecesPerWorkday = 1666, SortOrder = 8, IsActive = true },
                new ProductionStage { StageName = "بوز لمعه", PiecesPerWorkday = 1666, SortOrder = 9, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه ستار", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "6 ضربات", PiecesPerWorkday = 1000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "لفه", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "وش", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه مروحه", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "رايش وش", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "صوبع", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "بطن", PiecesPerWorkday = 3333, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه مثلثه", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "رايش وش", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "بطن", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "وش", PiecesPerWorkday = 2500, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه تاج", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "لفه خشن", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه ناعم", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "بطن", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "وش", PiecesPerWorkday = 2000, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه مشتمل", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "رايش وش", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه 400", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "لفه 600", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لفه 800", PiecesPerWorkday = 2500, SortOrder = 4, IsActive = true },
                new ProductionStage { StageName = "بطن 400", PiecesPerWorkday = 2500, SortOrder = 5, IsActive = true },
                new ProductionStage { StageName = "بطن 600", PiecesPerWorkday = 2500, SortOrder = 6, IsActive = true },
                new ProductionStage { StageName = "وش", PiecesPerWorkday = 2500, SortOrder = 7, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه جلو", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "رايش وش", PiecesPerWorkday = 2500, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه", PiecesPerWorkday = 2500, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "بطن", PiecesPerWorkday = 2500, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "وش", PiecesPerWorkday = 2500, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "كبشه الماني", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "الكبشه كامله", PiecesPerWorkday = 1000, SortOrder = 1, IsActive = true },
            }});
            products.Add(new Product { Name = "طبق مشتمل", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "رايش", PiecesPerWorkday = 5000, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "وش 400", PiecesPerWorkday = 5000, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "وش 600", PiecesPerWorkday = 5000, SortOrder = 2, IsActive = true },
                new ProductionStage { StageName = "وش 600 زيت", PiecesPerWorkday = 5000, SortOrder = 3, IsActive = true },
                new ProductionStage { StageName = "لمعه+تطويق", PiecesPerWorkday = 600, SortOrder = 4, IsActive = true },
            }});
            products.Add(new Product { Name = "وصله مشتمل", IsActive = true, Stages = new List<ProductionStage> {
                new ProductionStage { StageName = "لفه 400", PiecesPerWorkday = 6400, SortOrder = 0, IsActive = true },
                new ProductionStage { StageName = "لفه 600", PiecesPerWorkday = 6400, SortOrder = 1, IsActive = true },
                new ProductionStage { StageName = "لفه 600 زيت", PiecesPerWorkday = 6400, SortOrder = 2, IsActive = true },
            }});

            return products;
        }

        public static List<Worker> BuildWorkers()
        {
            var workers = new List<Worker>();

            workers.Add(new Worker { FullName = "مصطفى محمد مهدي عبد الحفيظ محمود", EmployeeCode = "W001", SkillsNotes = null, IsActive = true });
            workers.Add(new Worker { FullName = "حسن حسين حسن حسين على", EmployeeCode = "W002", SkillsNotes = "جميع مراحل صنفره المحابس و اللوازم  /لمعه لوازم", IsActive = true });
            workers.Add(new Worker { FullName = "ابوزيد عبدالله السيد عبدالله", EmployeeCode = "W003", SkillsNotes = "جميع مراحل صنفره المحابس و اللوازم", IsActive = true });
            workers.Add(new Worker { FullName = "اشرف على بدوى", EmployeeCode = "W004", SkillsNotes = "جميع مراحل صنفره المحابس و اللوازم", IsActive = true });
            workers.Add(new Worker { FullName = "ياسر كامل صوفان", EmployeeCode = "W005", SkillsNotes = "محبس GRS لفه صغيره/رقبه/دبله /بطن/بطحتين/كوع بسن كامل/وصله تي عريض خشن وناعم/محبس MG بطن ناعم /رقبه/دبله/حنفيه خلفي/محبس ماجيك كامل", IsActive = true });
            workers.Add(new Worker { FullName = "علي بسيوني السيد بسيوني", EmployeeCode = "W006", SkillsNotes = "طقم عقله/دبله محبس", IsActive = true });
            workers.Add(new Worker { FullName = "تامر جاد محمد علي", EmployeeCode = "W007", SkillsNotes = "جميع مراحل المحبس GRS ماعدا البطحتين/محبس MG ضيق / بطن/ضربه/دبلتين/رقبه/جميع مراحل اللوازم/ لمعه/محبس ماجيك لفات", IsActive = true });
            workers.Add(new Worker { FullName = "حسام محمد عبد الجواد عبد الله", EmployeeCode = "W008", SkillsNotes = "جميع مراحل اللوازم/جميع مراحل المحبس GRS ماعدا البطحتين/محبس ماجيك لفات /بطن", IsActive = true });
            workers.Add(new Worker { FullName = "خيري فراج احمد شحاتة", EmployeeCode = "W009", SkillsNotes = "جميع مراحل المحبس GRS ماعدا البطحتين/محبس MG ضيق / بطن/ضربه/دبلتين/رقبه/جميع مراحل الكوع/ وصله تي بطن و لفات/جميع مراحل ماجيك ماعدا البطحتين", IsActive = true });
            workers.Add(new Worker { FullName = "عمرو عبد المنعم عبد القادر محمد", EmployeeCode = "W010", SkillsNotes = "جميع مراحل الكوبشه جيد", IsActive = true });
            workers.Add(new Worker { FullName = "يوسف محمد علي عبد العاطي", EmployeeCode = "W011", SkillsNotes = "جميع مراحل الكوبشه جيد جدا", IsActive = true });
            workers.Add(new Worker { FullName = "اسامة محمد الحسيني احمد الرفاعي", EmployeeCode = "W012", SkillsNotes = "جميع مراحل الكوبشه ممتاز", IsActive = true });
            workers.Add(new Worker { FullName = "احمد محمد محمود الصاوي", EmployeeCode = "W013", SkillsNotes = "جميع مراحل الكوبشه ممتاز", IsActive = true });
            workers.Add(new Worker { FullName = "صابر عبد المنعم عبد الحافظ حراز", EmployeeCode = "W014", SkillsNotes = "جميع مراحل الكوبشه جيد", IsActive = true });
            workers.Add(new Worker { FullName = "محمد عادل ابراهيم محمد", EmployeeCode = "W015", SkillsNotes = "جميع مراحل صنفره المحابس و اللوازم", IsActive = true });
            workers.Add(new Worker { FullName = "عبدالسلام عابدين عبد السلام", EmployeeCode = "W016", SkillsNotes = "دبله جميع المحبس/كوع بسن رايش/لمعه كتان/محبس ماجيك لفات", IsActive = true });
            workers.Add(new Worker { FullName = "اسلام سعد الدين محمد", EmployeeCode = "W017", SkillsNotes = "جميع مراحل محبس GRS ماعدا البطحتين/ محبس MG لفه /رقبه/ضربه/محبس ماجيك بطم/ لفات", IsActive = true });
            workers.Add(new Worker { FullName = "محمود محمد محمود", EmployeeCode = "W018", SkillsNotes = "لمعه /محبس GRS بطن خشن وناعم/دبلتين/ضربتين/لفه 400/600", IsActive = true });
            workers.Add(new Worker { FullName = "محمد جمال مصطفى", EmployeeCode = "W019", SkillsNotes = "جميع مراحل محبس ماجيك/كوع بسن رايش/وصله تي 3 لفات/محبس GRS  دبله/رقبه/بطن/ضربتين/لفه 400/600/محبس MG دبله/ضربه/عريض/بطن 600", IsActive = true });
            workers.Add(new Worker { FullName = "اشرف محمد اسماعيل", EmployeeCode = "W020", SkillsNotes = "لمعه /محبس GRS بطن خشن وناعم/دبله/كوع بسن رايش/تي بطن خشن /3لفات", IsActive = true });
            workers.Add(new Worker { FullName = "محمد مصطفى احمد", EmployeeCode = "W021", SkillsNotes = "عامل تحت التدريب", IsActive = true });
            workers.Add(new Worker { FullName = "عبدالله احمد محمد", EmployeeCode = "W022", SkillsNotes = "جميع المحابس رقبه /دبله", IsActive = true });
            workers.Add(new Worker { FullName = "رجب حسان محمد", EmployeeCode = "W023", SkillsNotes = "لمعه لوازم/ كوع بسن رايش", IsActive = true });
            workers.Add(new Worker { FullName = "يوسف احمد عبدالصمد", EmployeeCode = "W024", SkillsNotes = "جميع مراحل اللوازم/جميع مراحل محبس MG /محبس GRS  دبله/رقبه/بطن/ضربتين/لفه 400/600 /بطحتين", IsActive = true });
            workers.Add(new Worker { FullName = "حمدي عبداللاه موسى", EmployeeCode = "W025", SkillsNotes = "طقم عقله/دبله محبس", IsActive = true });
            workers.Add(new Worker { FullName = "جمال الصدام جمال", EmployeeCode = "W026", SkillsNotes = "رايش كبشه/رايش وش", IsActive = true });
            workers.Add(new Worker { FullName = "محمود مصطفى احمد", EmployeeCode = "W027", SkillsNotes = "طقم عقله", IsActive = true });
            workers.Add(new Worker { FullName = "خالد سعيد عوض", EmployeeCode = "W028", SkillsNotes = "طقم عقله", IsActive = true });
            workers.Add(new Worker { FullName = "علي عادل عبدالغفار", EmployeeCode = "W029", SkillsNotes = "جميع مراحل الكوبشه جيد جدا", IsActive = true });
            workers.Add(new Worker { FullName = "احمد عبدالعليم محمد", EmployeeCode = "W030", SkillsNotes = "جميع مراحل الكوبشه جيد جدا", IsActive = true });
            workers.Add(new Worker { FullName = "ابراهيم علي محمد", EmployeeCode = "W031", SkillsNotes = "جميع مراحل الكوبشه جيد جدا", IsActive = true });
            workers.Add(new Worker { FullName = "اسماعيل محمد اسماعيل", EmployeeCode = "W032", SkillsNotes = "جميع مراحل محبس ماجيك/كوع بسن رايش/وصله تي 3 لفات/محبس GRS  دبله/بطن", IsActive = true });
            workers.Add(new Worker { FullName = "وليد احمد محمد", EmployeeCode = "W033", SkillsNotes = "جميع مراحل اللوازم/محبس GRS لفه 800/جميع مراحل محبس ماجيك", IsActive = true });
            workers.Add(new Worker { FullName = "بدر عبدالعزيز السيد", EmployeeCode = "W034", SkillsNotes = "جميع مراحل اللوازم/محبس GRS لفه 600/جميع مراحل محبس ماجيك", IsActive = true });
            workers.Add(new Worker { FullName = "احمد عاطف خيري", EmployeeCode = "W035", SkillsNotes = "محبس GRS لفه صغيره/رقبه/دبله /بطن/محبس MG بطن 400 /رقبه/دبله", IsActive = true });
            workers.Add(new Worker { FullName = "عماد شعبان حريمز", EmployeeCode = "W036", SkillsNotes = "طقم عقله", IsActive = true });
            workers.Add(new Worker { FullName = "عبدالرحمن صابر عبدالمنعم", EmployeeCode = "W037", SkillsNotes = "عامل تحت التدريب", IsActive = true });
            workers.Add(new Worker { FullName = "احمد مرزوق", EmployeeCode = "W038", SkillsNotes = "عامل تحت التدريب", IsActive = true });
            workers.Add(new Worker { FullName = "رمضان خميس", EmployeeCode = "W039", SkillsNotes = "عامل تحت التدريب", IsActive = true });
            workers.Add(new Worker { FullName = "يوسف محمد حسب", EmployeeCode = "W040", SkillsNotes = "عامل رص", IsActive = true });
            workers.Add(new Worker { FullName = "مروان سالم شحات", EmployeeCode = "W041", SkillsNotes = "عامل جوده", IsActive = true });
            workers.Add(new Worker { FullName = "الحسن علي الجنبيهي", EmployeeCode = "W042", SkillsNotes = "عامل تحت التدريب", IsActive = true });
            workers.Add(new Worker { FullName = "مصطفى محمود فهيم", EmployeeCode = "W043", SkillsNotes = "كوع بسن عريض/ رايش/وصله تي بطن/3لفات/جميع المحابس دبله/رقبه/ضربتين/محبس ماجيك بطن/لفات", IsActive = true });
            workers.Add(new Worker { FullName = "سلامه تامر سلامه", EmployeeCode = "W044", SkillsNotes = "طقم عقله/دبله محبس", IsActive = true });
            workers.Add(new Worker { FullName = "زياد عبدالرازق", EmployeeCode = "W045", SkillsNotes = "جميع مراحل محبس GRS ماعدا البطحتين/كوع بسن رايش/لمعه/وصله تي 3لفات /محبس ماجيك لفات/ بطن/محبس MG ضربه / دبله/لفه", IsActive = true });
            workers.Add(new Worker { FullName = "زياد محمود محمد", EmployeeCode = "W046", SkillsNotes = "طقم عقله/دبله محبس", IsActive = true });

            return workers;
        }
    }
}