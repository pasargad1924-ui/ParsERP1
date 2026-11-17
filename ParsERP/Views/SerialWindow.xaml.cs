using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class SerialWindow : Window
    {
        public class ProductItem
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
        }

        // ردیف‌های تازه تولیدشده (هنوز ممکن است ذخیره نشده باشند)
        private readonly ObservableCollection<SerialListItem> _newBatch = new();
        private Database? _db;
        private bool _showingNewBatch;

        public SerialWindow(string? initialSearch = null)
        {
            InitializeComponent();

            var now = DateTime.Now;
            int approxSolar = now.Year - 621;
            if (now.Month < 3 || (now.Month == 3 && now.Day < 21)) approxSolar--;
            txtSolarYear.Text = (approxSolar % 100).ToString("D2");
            txtCount.Text = "5";
            txtPart.Text = DateTime.Now.ToString("yyyyMMdd");

            try
            {
                _db = Database.FromConfig();
                LoadProducts();
                if (!string.IsNullOrWhiteSpace(initialSearch))
                {
                    txtSearch.Text = initialSearch;
                    LoadSavedSerials(initialSearch);
                }
                else
                    LoadSavedSerials(null);
            }
            catch (Exception ex)
            {
                AppLogger.Error("SerialWindow init", ex);
                MessageBox.Show("خطا در اتصال دیتابیس:\n" + ex.Message +
                    "\n\nلاگ: " + AppLogger.GetLogFolder(),
                    "دیتابیس", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadProducts()
        {
            if (_db == null) return;
            var items = _db.GetActiveProducts()
                .Select(p => new ProductItem { Id = p.Id, Code = p.Code, Name = p.Code + " - " + p.NameFa })
                .ToList();
            cmbProduct.ItemsSource = items;
            if (items.Count > 0) cmbProduct.SelectedIndex = 0;
            else txtStatus.Text = "ابتدا از منوی «کالا و محصولات» محصول تعریف کنید.";
        }

        private void LoadSavedSerials(string? query)
        {
            if (_db == null) return;
            _showingNewBatch = false;
            var list = _db.SearchSerials(query, 500);
            dgSerials.ItemsSource = list;
            txtStatus.Text = string.IsNullOrWhiteSpace(query)
                ? $"نمایش آخرین {list.Count} سریال ثبت‌شده"
                : $"{list.Count} نتیجه برای «{query}»";
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { LoadSavedSerials(txtSearch.Text); }
            catch (Exception ex) { AppLogger.Error("Search serials", ex); }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadSavedSerials(null);
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbProduct.SelectedItem is not ProductItem pi)
                {
                    MessageBox.Show("یک محصول انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(pi.Code, out int productType) || productType < 0 || productType > 99)
                {
                    MessageBox.Show("کد محصول نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtSolarYear.Text?.Trim(), out int year) || year < 0 || year > 99)
                {
                    MessageBox.Show("سال باید دو رقم باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtCount.Text?.Trim(), out int count) || count <= 0 || count > 5000)
                {
                    MessageBox.Show("تعداد باید بین ۱ تا ۵۰۰۰ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string part = txtPart.Text?.Trim() ?? "";
                var used = _db?.GetUsedSerialNumbers(pi.Code, year) ?? new System.Collections.Generic.HashSet<int>();
                // شماره‌هایی که در بچ فعلی هم هست را هم در نظر بگیر
                foreach (var x in _newBatch)
                    if (x.ProductTypeCode == pi.Code && x.SolarYear == year)
                        used.Add(x.SerialNumber);

                var serials = SerialService.GenerateUniqueRandom(productType, year, count, used);
                _newBatch.Clear();
                foreach (var s in serials)
                {
                    var parsed = SerialService.Parse(s)!;
                    _newBatch.Add(new SerialListItem
                    {
                        SerialNo = s,
                        ProductTypeCode = parsed.Value.ProductType.ToString("D2"),
                        SolarYear = parsed.Value.Year,
                        SerialNumber = parsed.Value.Number,
                        CheckDigit = parsed.Value.CheckDigit,
                        Status = "آماده ذخیره",
                        PartNumber = part,
                        ProductName = pi.Name,
                        CreatedAt = ""
                    });
                }
                _showingNewBatch = true;
                dgSerials.ItemsSource = null;
                dgSerials.ItemsSource = _newBatch;
                txtStatus.Text = $"{count} سریال یکتا (تصادفی) آماده شد. پارت: {part}. برای ثبت دائمی «ذخیره» را بزنید.";
                AppLogger.Info($"Generated {count} unique serials for {pi.Code}/{year} part={part}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Generate serials", ex);
                MessageBox.Show("خطا:\n" + ex.Message + "\n\nلاگ: " + AppLogger.GetLogFolder(),
                    "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string serial = txtManualSerial.Text?.Trim() ?? "";
                if (!SerialService.Validate(serial))
                {
                    MessageBox.Show("سریال نامعتبر است (۱۰ رقم + رقم کنترل صحیح).", "خطا",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_db != null && _db.SerialExists(serial))
                {
                    MessageBox.Show("این سریال قبلاً در دیتابیس ثبت شده.", "تکراری",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int productId = 1;
                string productName = "";
                if (cmbProduct.SelectedItem is ProductItem pi)
                {
                    productId = pi.Id;
                    productName = pi.Name;
                }

                var parsed = SerialService.Parse(serial)!;
                // ذخیره مستقیم
                if (_db != null)
                {
                    _db.InsertSerialWithPart(productId, serial, parsed.Value.ProductType.ToString("D2"),
                        parsed.Value.Year, parsed.Value.Number, parsed.Value.CheckDigit,
                        txtPart.Text?.Trim(), null);
                }
                txtManualSerial.Clear();
                LoadSavedSerials(txtSearch.Text);
                txtStatus.Text = "سریال دستی ذخیره شد: " + serial;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Manual serial", ex);
                MessageBox.Show("خطا:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null)
                {
                    MessageBox.Show("اتصال دیتابیس برقرار نیست.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!_showingNewBatch || _newBatch.Count == 0)
                {
                    MessageBox.Show("ابتدا سریال جدید تولید کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int productId = 1;
                if (cmbProduct.SelectedItem is ProductItem pi) productId = pi.Id;

                int saved = 0, failed = 0;
                foreach (var row in _newBatch.ToList())
                {
                    try
                    {
                        if (_db.SerialExists(row.SerialNo))
                        {
                            // جایگزین یکتا بساز
                            var used = _db.GetUsedSerialNumbers(row.ProductTypeCode, row.SolarYear);
                            var replacement = SerialService.GenerateUniqueRandom(
                                int.Parse(row.ProductTypeCode), row.SolarYear, 1, used)[0];
                            var p = SerialService.Parse(replacement)!;
                            row.SerialNo = replacement;
                            row.SerialNumber = p.Value.Number;
                            row.CheckDigit = p.Value.CheckDigit;
                        }
                        _db.InsertSerialWithPart(productId, row.SerialNo, row.ProductTypeCode,
                            row.SolarYear, row.SerialNumber, row.CheckDigit, row.PartNumber, null);
                        row.Status = "ذخیره شد";
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Insert " + row.SerialNo, ex);
                        row.Status = "خطا";
                        failed++;
                    }
                }
                dgSerials.Items.Refresh();
                txtStatus.Text = $"ذخیره شد: {saved} | ناموفق: {failed}";
                MessageBox.Show($"تعداد {saved} سریال ذخیره شد.", "نتیجه", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSavedSerials(null);
            }
            catch (Exception ex)
            {
                AppLogger.Error("SaveAll", ex);
                MessageBox.Show("خطا:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e) => CopySelected(false);
        private void MenuCopySerial_Click(object sender, RoutedEventArgs e) => CopySelected(true);
        private void MenuCopyRow_Click(object sender, RoutedEventArgs e) => CopySelected(false);

        private void CopySelected(bool serialOnly)
        {
            try
            {
                var items = dgSerials.SelectedItems.Cast<SerialListItem>().ToList();
                if (items.Count == 0 && dgSerials.CurrentItem is SerialListItem one)
                    items.Add(one);
                if (items.Count == 0)
                {
                    MessageBox.Show("ردیفی انتخاب نشده است.", "کپی", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var sb = new StringBuilder();
                if (serialOnly)
                {
                    foreach (var i in items) sb.AppendLine(i.SerialNo);
                }
                else
                {
                    foreach (var i in items)
                        sb.AppendLine($"{i.SerialNo}\t{i.ProductName}\t{i.PartNumber}\t{i.Status}\t{i.CreatedAt}");
                }
                Clipboard.SetText(sb.ToString().TrimEnd());
                txtStatus.Text = $"{items.Count} مورد در کلیپ‌بورد کپی شد.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Copy", ex);
                MessageBox.Show("کپی انجام نشد:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
