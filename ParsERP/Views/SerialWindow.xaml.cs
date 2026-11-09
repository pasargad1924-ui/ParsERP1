using System;
using System.Collections.ObjectModel;
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
        }

        private readonly ObservableCollection<SerialRow> _rows = new();
        private Database? _db;

        public SerialWindow()
        {
            InitializeComponent();
            dgSerials.ItemsSource = _rows;

            var now = DateTime.Now;
            int approxSolar = now.Year - 621;
            if (now.Month < 3 || (now.Month == 3 && now.Day < 21))
                approxSolar--;
            txtSolarYear.Text = (approxSolar % 100).ToString("D2");
            txtStartNumber.Text = "1";
            txtCount.Text = "10";

            try { _db = Database.FromConfig(); }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در اتصال به دیتابیس:\n" + ex.Message, "دیتابیس",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtProductType.Text?.Trim(), out int productType) || productType < 0 || productType > 99)
                { MessageBox.Show("کد نوع محصول باید بین ۰۰ تا ۹۹ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!int.TryParse(txtSolarYear.Text?.Trim(), out int year) || year < 0 || year > 99)
                { MessageBox.Show("سال باید دو رقم باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!int.TryParse(txtStartNumber.Text?.Trim(), out int start) || start < 0 || start > 99999)
                { MessageBox.Show("شماره شروع نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (!int.TryParse(txtCount.Text?.Trim(), out int count) || count <= 0 || count > 5000)
                { MessageBox.Show("تعداد باید بین ۱ تا ۵۰۰۰ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

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
                        Status = "آماده ذخیره"
                    });
                }
                string msg = $"{_rows.Count} سریال آماده شد.";
                if (skipped > 0) msg += $" ({skipped} تکراری رد شد)";
                txtStatus.Text = msg;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            string serial = txtManualSerial.Text?.Trim() ?? "";
            if (!SerialService.Validate(serial))
            {
                MessageBox.Show("سریال نامعتبر است (۱۰ رقم + رقم کنترل صحیح).", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_db != null && _db.SerialExists(serial))
            {
                MessageBox.Show("این سریال قبلاً در دیتابیس ثبت شده.", "تکراری", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (var r in _rows)
                if (r.SerialNo == serial) { MessageBox.Show("در لیست فعلی وجود دارد.", "تکراری", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            var parsed = SerialService.Parse(serial)!;
            _rows.Add(new SerialRow
            {
                SerialNo = serial,
                ProductTypeCode = parsed.Value.ProductType.ToString("D2"),
                SolarYear = parsed.Value.Year,
                SerialNumber = parsed.Value.Number,
                CheckDigit = parsed.Value.CheckDigit,
                Status = "ورود دستی"
            });
            txtManualSerial.Clear();
            txtStatus.Text = "سریال دستی اضافه شد.";
        }

        private void BtnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null)
            {
                MessageBox.Show("اتصال دیتابیس برقرار نیست. از منوی تنظیمات مسیر را بررسی کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_rows.Count == 0)
            {
                MessageBox.Show("سریالی برای ذخیره وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int productId = 1; // موقت تا ماژول کالا کامل شود
            int saved = 0, failed = 0;
            foreach (var row in _rows)
            {
                try
                {
                    if (_db.SerialExists(row.SerialNo)) { failed++; continue; }
                    _db.InsertSerial(productId, row.SerialNo, row.ProductTypeCode,
                        row.SolarYear, row.SerialNumber, row.CheckDigit, null);
                    row.Status = "ذخیره شد";
                    saved++;
                }
                catch { row.Status = "خطا"; failed++; }
            }
            dgSerials.Items.Refresh();
            txtStatus.Text = $"ذخیره شد: {saved} | ناموفق/تکراری: {failed}";
            MessageBox.Show($"تعداد {saved} سریال ذخیره شد.", "نتیجه", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
