using System.IO;
using System.Windows;
using Microsoft.Win32;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class SettingsWindow : Window
    {
        private AppConfig _config;

        public SettingsWindow()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            txtDbPath.Text = _config.DatabasePath;
            txtBackupPath.Text = _config.BackupPath;
        }

        private void BtnBrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "SQLite Database|*.db",
                FileName = Path.GetFileName(_config.DatabasePath) is { Length: > 0 } n ? n : "ParsERP.db",
                Title = "انتخاب مسیر فایل دیتابیس"
            };
            if (dlg.ShowDialog() == true)
                txtDbPath.Text = dlg.FileName;
        }

        private void BtnBrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            // استفاده از OpenFileDialog به‌عنوان انتخاب پوشه ساده نیست؛ مسیر را دستی یا از دیالوگ فایل می‌گیریم
            var dlg = new OpenFileDialog
            {
                CheckFileExists = false,
                FileName = "پوشه را انتخاب کنید",
                Title = "یک فایل داخل پوشه پشتیبان انتخاب کنید"
            };
            if (dlg.ShowDialog() == true)
            {
                var dir = Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(dir))
                    txtBackupPath.Text = dir;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDbPath.Text))
            {
                MessageBox.Show("مسیر دیتابیس را مشخص کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _config.DatabasePath = txtDbPath.Text.Trim();
            _config.BackupPath = txtBackupPath.Text.Trim();
            _config.Save();

            // اطمینان از وجود پوشه‌ها
            try
            {
                var dbDir = Path.GetDirectoryName(_config.DatabasePath);
                if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);
                if (!string.IsNullOrEmpty(_config.BackupPath)) Directory.CreateDirectory(_config.BackupPath);
            }
            catch { /* نادیده */ }

            MessageBox.Show("تنظیمات ذخیره شد.\nدر اجرای بعدی از مسیر جدید استفاده می‌شود.", "موفق",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
