using System;
using System.IO;
using System.Linq;
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
            NavigateDashboard();
        }

        private void NavigateDashboard()
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
                Text = "نسخه " + AppInfo.Version + " — بخش‌ها در همین صفحه و کنار منو باز می‌شوند.",
                FontSize = 14,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "جستجوی بالا: نام تأمین‌کننده/مشتری، سریال یا پارت را وارد کنید و Enter بزنید.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 22
            });
            return panel;
        }

        private void ShowContent(string title, UserControl view)
        {
            txtPageTitle.Text = title;
            MainContent.Content = view;
        }

        private void MenuDashboard_Click(object sender, RoutedEventArgs e) => NavigateDashboard();

        private void MenuProducts_Click(object sender, RoutedEventArgs e)
            => ShowContent("کالا و محصولات", new ProductsWindow());

        private void MenuParties_Click(object sender, RoutedEventArgs e)
            => ShowContent("اشخاص (دفترچه تماس)", new PartiesWindow());

        private void MenuPurchase_Click(object sender, RoutedEventArgs e)
            => ShowContent("خرید", new PurchaseWindow());

        private void MenuSales_Click(object sender, RoutedEventArgs e)
            => ShowContent("فروش", new SalesWindow());

        private void MenuSerials_Click(object sender, RoutedEventArgs e)
            => ShowContent("سریال‌ها", new SerialWindow());

        private void MenuWarehouse_Click(object sender, RoutedEventArgs e)
            => ShowContent("انبار", new WarehouseWindow());

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
            => ShowContent("تنظیمات", new SettingsWindow());

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
            => ShowContent("راهنما و درباره", new AboutWindow());

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
            => ShowContent("راهنما و درباره", new AboutWindow());

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

        private void MenuFocusSearch_Click(object sender, RoutedEventArgs e) => txtGlobalSearch.Focus();

        private void MenuStub_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("این بخش در نسخه بعدی تکمیل می‌شود.", "به‌زودی",
                MessageBoxButton.OK, MessageBoxImage.Information);
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
            try
            {
                var db = Database.FromConfig();
                // اول اشخاص
                var parties = db.SearchParties(q);
                if (parties.Count > 0)
                {
                    ShowContent("اشخاص — نتیجه جستجو", new PartiesWindow(q));
                    return;
                }
                // سریال
                var serials = db.SearchSerials(q, 20);
                if (serials.Count > 0)
                {
                    ShowContent("سریال‌ها — نتیجه جستجو", new SerialWindow(q));
                    return;
                }
                // پیش‌فرض: اشخاص با همان عبارت (حتی اگر خالی)
                ShowContent("جستجو", new PartiesWindow(q));
            }
            catch (Exception ex)
            {
                AppLogger.Error("Global search", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuExportSerial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = Database.FromConfig();
                var list = db.SearchSerials(null, 10000);
                var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "serials_export.csv" };
                if (dlg != true) return;
                var sb = new StringBuilder();
                sb.AppendLine("SerialNo,ProductCode,Year,Number,Part,Status,ProductName,CreatedAt");
                foreach (var s in list)
                    sb.AppendLine(string.Join(",", s.SerialNo, s.ProductTypeCode, s.SolarYear, s.SerialNumber,
                        Csv(s.PartNumber), Csv(s.Status), Csv(s.ProductName), Csv(s.CreatedAt)));
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("خروجی ذخیره شد.", "صادر کردن", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Export", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuImportSerial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "CSV (*.csv)|*.csv|متن (*.txt)|*.txt" };
                if (dlg != true) return;
                var db = Database.FromConfig();
                int ok = 0, skip = 0;
                foreach (var line in File.ReadAllLines(dlg.FileName, Encoding.UTF8))
                {
                    var serial = line.Trim().Split(',')[0].Trim().Trim('"');
                    if (!SerialService.Validate(serial)) { skip++; continue; }
                    if (db.SerialExists(serial)) { skip++; continue; }
                    var p = SerialService.Parse(serial)!;
                    db.InsertSerialWithPart(1, serial, p.Value.ProductType.ToString("D2"),
                        p.Value.Year, p.Value.Number, p.Value.CheckDigit, "IMPORT", null);
                    ok++;
                }
                MessageBox.Show($"وارد شد: {ok}\nرد شده: {skip}", "وارد کردن", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Import", ex);
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
