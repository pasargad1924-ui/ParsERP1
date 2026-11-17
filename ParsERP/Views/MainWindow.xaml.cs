using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = AppInfo.FullTitle;
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            txtPageTitle.Text = "داشبورد";
            MainContent.Content = CreateWelcomePanel();
        }

        private UIElement CreateWelcomePanel()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "خوش آمدید به " + AppInfo.NameFa,
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "نسخه " + AppInfo.Version + " — از منوی کناری یا نوار فایل استفاده کنید.",
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 20)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "راهنما: ابتدا «کالا و محصولات» را تعریف کنید، سپس در «سریال‌ها» پارت تولید و تعداد را وارد و سریال یکتا بسازید. جستجو از نوار بالا با Enter یا دکمه ذره‌بین انجام می‌شود.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 22,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
            });
            return panel;
        }

        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => LoadDashboard();

        private void MenuProducts_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "کالا و محصولات";
            new ProductsWindow { Owner = this }.ShowDialog();
        }

        private void MenuSerials_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "سریال‌ها";
            new SerialWindow { Owner = this }.ShowDialog();
        }

        private void MenuWarehouse_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "انبار";
            new WarehouseWindow { Owner = this }.ShowDialog();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow { Owner = this }.ShowDialog();
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

        private void MenuFocusSearch_Click(object sender, RoutedEventArgs e)
        {
            txtGlobalSearch.Focus();
        }

        private void MenuStub_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("این بخش در نسخه بعدی تکمیل می‌شود.\nاسکلت منو برای استاندارد شدن نرم‌افزار اضافه شده است.",
                "به‌زودی", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GlobalSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) DoSearch();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => DoSearch();

        private void DoSearch()
        {
            var q = txtGlobalSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(q))
            {
                MessageBox.Show("عبارت جستجو را وارد کنید.", "جستجو", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new SerialWindow(q) { Owner = this }.ShowDialog();
        }

        private void MenuExportSerial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = Database.FromConfig();
                var list = db.SearchSerials(null, 10000);
                var dlg = new SaveFileDialog
                {
                    Filter = "CSV (*.csv)|*.csv|متن (*.txt)|*.txt",
                    FileName = "serials_export.csv"
                };
                if (dlg.ShowDialog() != true) return;

                var sb = new StringBuilder();
                sb.AppendLine("SerialNo,ProductCode,Year,Number,Part,Status,ProductName,CreatedAt");
                foreach (var s in list)
                {
                    sb.AppendLine(string.Join(",",
                        s.SerialNo, s.ProductTypeCode, s.SolarYear, s.SerialNumber,
                        Csv(s.PartNumber), Csv(s.Status), Csv(s.ProductName), Csv(s.CreatedAt)));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                AppLogger.Info("Exported serials to " + dlg.FileName);
                MessageBox.Show("خروجی ذخیره شد.\n(برای Excel فایل CSV را باز کنید؛ Word/PDF در نسخه‌های بعد)",
                    "صادر کردن", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Export serials", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuImportSerial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "CSV (*.csv)|*.csv|متن (*.txt)|*.txt"
                };
                if (dlg.ShowDialog() != true) return;

                var db = Database.FromConfig();
                var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8);
                int ok = 0, skip = 0;
                foreach (var line in lines)
                {
                    var t = line.Trim();
                    if (t.Length < 10) continue;
                    // اگر خط CSV است ستون اول را بگیر
                    var serial = t.Split(',')[0].Trim().Trim('"');
                    if (!SerialService.Validate(serial)) { skip++; continue; }
                    if (db.SerialExists(serial)) { skip++; continue; }
                    var p = SerialService.Parse(serial)!;
                    db.InsertSerialWithPart(1, serial, p.Value.ProductType.ToString("D2"),
                        p.Value.Year, p.Value.Number, p.Value.CheckDigit, "IMPORT", null);
                    ok++;
                }
                MessageBox.Show($"وارد شد: {ok}\nرد/تکراری/نامعتبر: {skip}", "وارد کردن",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Import serials", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Csv(string? s)
        {
            s ??= "";
            if (s.Contains(',') || s.Contains('"')) return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
