using System;
using System.Collections.ObjectModel;
using System.Windows;
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

        public SerialWindow()
        {
            InitializeComponent();
            dgSerials.ItemsSource = _rows;

            // مقدار پیش‌فرض سال شمسی جاری (تقریبی)
            var now = DateTime.Now;
            int approxSolar = now.Year - 621;
            if (now.Month < 3 || (now.Month == 3 && now.Day < 21))
                approxSolar--;
            txtSolarYear.Text = (approxSolar % 100).ToString("D2");
            txtStartNumber.Text = "1";
            txtCount.Text = "10";
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtProductType.Text?.Trim(), out int productType) || productType < 0 || productType > 99)
                {
                    MessageBox.Show("کد نوع محصول باید عددی بین ۰۰ تا ۹۹ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (!int.TryParse(txtCount.Text?.Trim(), out int count) || count <= 0 || count > 10000)
                {
                    MessageBox.Show("تعداد باید بین ۱ تا ۱۰۰۰۰ باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var list = SerialService.GenerateBatch(productType, year, start, count);
                _rows.Clear();
                foreach (var s in list)
                {
                    var parsed = SerialService.Parse(s);
                    if (parsed == null) continue;
                    _rows.Add(new SerialRow
                    {
                        SerialNo = s,
                        ProductTypeCode = parsed.Value.ProductType.ToString("D2"),
                        SolarYear = parsed.Value.Year,
                        SerialNumber = parsed.Value.Number,
                        CheckDigit = parsed.Value.CheckDigit,
                        Status = "آماده ثبت"
                    });
                }
                txtStatus.Text = $"{list.Count} سریال تولید شد. (هنوز در دیتابیس ذخیره نشده)";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            string serial = txtManualSerial.Text?.Trim() ?? "";
            if (!SerialService.Validate(serial))
            {
                MessageBox.Show("سریال نامعتبر است. باید ۱۰ رقم و دارای رقم کنترل صحیح باشد.", "خطا",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var parsed = SerialService.Parse(serial)!;
            // جلوگیری از تکرار در لیست فعلی
            foreach (var r in _rows)
            {
                if (r.SerialNo == serial)
                {
                    MessageBox.Show("این سریال قبلاً در لیست وجود دارد.", "تکراری",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

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
    }
}
