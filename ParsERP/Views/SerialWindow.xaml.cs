using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class SerialWindow : Window
    {
        public class SerialRow
        {
            public string SerialNo { get; set; } = "";
            public string ProductTypeCode { get; set; } = "";
            public int SolarYear { get; set; }
            public int SerialNumber { get; set; }
            public int CheckDigit { get; set; }
            public string Status { get; set; } = "InStock";
            public int ProductId { get; set; }
        }

        public class ProductItem
        {
            public int Id { get; set; }
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
        }

        private readonly ObservableCollection<SerialRow> _rows = new();
        private Database? _db;

        public SerialWindow()
        {
            InitializeComponent();
            dgSerials.ItemsSource = _rows;

            var now = DateTime.Now;
            int approxSolar = now.Year - 621;
            if (now.Month < 3 || (now.Month == 3 && now.Day < 21)) approxSolar--;
            txtSolarYear.Text = (approxSolar % 100).ToString("D2");
            txtStartNumber.Text = "1";
            txtCount.Text = "10";

            try
            {
                _db = Database.FromConfig();
                LoadProducts();
            }
            catch (Exception ex)
            {
                AppLogger.Error("SerialWindow init", ex);
                MessageBox.Show("خطا در اتصال دیتابیس:\n" + ex.Message +
                    "\n\nمسیر لاگ: " + AppLogger.GetLogFolder(),
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
            else
                txtStatus.Text = "ابتدا از منوی «کالا و محصولات» محصول تعریف کنید.";
        }

        private bool TryGetSelectedProduct(out int productId, out string code)
        {
            productId = 0;
            code = "";
            if (cmbProduct.SelectedItem is ProductItem pi)
            {
                productId = pi.Id;
                code = pi.Code;
                return true;
            }
            // اجازه ورود دستی کد از طریق انتخاب‌نشدن
            return false;
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetSelectedProduct(out int productId, out string code))
                {
                    MessageBox.Show("یک محصول انتخاب کنید. اگر لیست خالی است، اول محصول تعریف کنید.", "توجه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(code, out int productType) || productType < 0 || productType > 99)
                {
                    MessageBox.Show("کد محصول نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtSolarYear.Text?.Trim(), out int year) || year < 0 || year > 99)
                {
                    MessageBox.Show("سال باید دو رقم باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtStartNumber.Text?.Trim(), out int start) || start < 0 || start > 99999)
                {
                    MessageBox.Show("شماره شروع نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(txtCount.Text?.Trim(), out int count) || count <= 0 || count > 5000)
                {
                    MessageBox.Show("تعداد باید بین ۱ تا ۵۰۰۰ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var list = SerialService.GenerateBatch(productType, year, start, count);
                _rows.Clear();
                int skipped = 0;
                foreach (var s in list)
                {
                    if (_db != null && _db.SerialExists(s)) { skipped++; continue; }
                    var parsed = SerialService.Parse(s);
                    if (parsed == null) continue;
                    _rows.Add(new SerialRow
                    {
                        SerialNo = s,
                        ProductTypeCode = parsed.Value.ProductType.ToString("D2"),
                        SolarYear = parsed.Value.Year,
                        SerialNumber = parsed.Value.Number,
                        CheckDigit = parsed.Value.CheckDigit,
                        Status = "آماده ذخیره",
                        ProductId = productId
                    });
                }
                string msg = $"{_rows.Count} سریال آماده شد.";
                if (skipped > 0) msg += $" ({skipped} تکراری رد شد)";
                txtStatus.Text = msg;
                AppLogger.Info(msg);
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
                if (_rows.Any(r => r.SerialNo == serial))
                {
                    MessageBox.Show("در لیست فعلی وجود دارد.", "تکراری",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int productId = 0;
                if (cmbProduct.SelectedItem is ProductItem pi) productId = pi.Id;

                var parsed = SerialService.Parse(serial)!;
                _rows.Add(new SerialRow
                {
                    SerialNo = serial,
                    ProductTypeCode = parsed.Value.ProductType.ToString("D2"),
                    SolarYear = parsed.Value.Year,
                    SerialNumber = parsed.Value.Number,
                    CheckDigit = parsed.Value.CheckDigit,
                    Status = "ورود دستی",
                    ProductId = productId
                });
                txtManualSerial.Clear();
                txtStatus.Text = "سریال دستی اضافه شد.";
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
                if (_rows.Count == 0)
                {
                    MessageBox.Show("سریالی برای ذخیره وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int saved = 0, failed = 0;
                foreach (var row in _rows)
                {
                    try
                    {
                        if (_db.SerialExists(row.SerialNo)) { failed++; continue; }
                        int pid = row.ProductId > 0 ? row.ProductId : 1;
                        // اگر محصولی نیست، یک محصول موقت با کد سریال بساز
                        if (pid <= 0) pid = 1;
                        _db.InsertSerial(pid, row.SerialNo, row.ProductTypeCode,
                            row.SolarYear, row.SerialNumber, row.CheckDigit, null);
                        row.Status = "ذخیره شد";
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Insert serial " + row.SerialNo, ex);
                        row.Status = "خطا";
                        failed++;
                    }
                }
                dgSerials.Items.Refresh();
                txtStatus.Text = $"ذخیره شد: {saved} | ناموفق/تکراری: {failed}";
                MessageBox.Show($"تعداد {saved} سریال ذخیره شد.", "نتیجه", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppLogger.Error("SaveAll serials", ex);
                MessageBox.Show("خطا:\n" + ex.Message + "\n\nلاگ: " + AppLogger.GetLogFolder(),
                    "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
