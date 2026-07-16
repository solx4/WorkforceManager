using System.Windows;

namespace WorkforceManager.UI.Views
{
    /// <summary>
    /// نافذة إضافة/تعديل عامل. بتتحقق من الاسم بس (الإجباري الوحيد) —
    /// التحقق الأعمق (زي تفرد الكود الوظيفي) مسؤولية WorkerManagementService
    /// عشان القاعدة تتطبق من أي مكان مش من الشاشة دي بس.
    /// </summary>
    public partial class WorkerEditDialog : Window
    {
        public WorkerEditDialog()
        {
            InitializeComponent();
            // التركيز على خانة الاسم فورًا — أسرع في الإدخال المتكرر
            Loaded += (_, _) => NameBox.Focus();
        }

        // ------- القيم اللي الشاشة الأم بتقرأها بعد الحفظ -------

        public string WorkerName => NameBox.Text.Trim();
        public string? EmployeeCode => string.IsNullOrWhiteSpace(CodeBox.Text) ? null : CodeBox.Text.Trim();
        public string? PhoneNumber => string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim();
        public DateTime? HireDate => HireDatePicker.SelectedDate;
        public string? SkillsNotes => string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        /// <summary>تعبئة الفورم ببيانات عامل موجود (وضع التعديل)</summary>
        public void LoadWorker(string fullName, string? code, string? phone, DateTime? hireDate, string? notes)
        {
            NameBox.Text = fullName;
            CodeBox.Text = code ?? "";
            PhoneBox.Text = phone ?? "";
            HireDatePicker.SelectedDate = hireDate;
            NotesBox.Text = notes ?? "";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // الاسم هو الحقل الإجباري الوحيد — من غيره مفيش حفظ
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                ErrorText.Text = "اسم العامل مطلوب";
                ErrorText.Visibility = Visibility.Visible;
                NameBox.Focus();
                return;
            }

            DialogResult = true;
        }
    }
}
