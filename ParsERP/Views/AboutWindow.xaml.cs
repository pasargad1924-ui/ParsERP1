using System.Diagnostics;
using System.Windows;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            txtAboutTitle.Text = AppInfo.NameFa;
            txtAboutVersion.Text = "نسخه " + AppInfo.Version;
            txtHelp.Text =
                "راهنمای سریع پارس‌ئی‌آرپی\n\n" +
                "۱. کالا و محصولات: کد ۲ رقمی محصول را تعریف کنید.\n" +
                "۲. سریال‌ها: محصول، سال شمسی و پارت تولید را مشخص کنید و تعداد سریال یکتا بسازید.\n" +
                "۳. ثبت دستی: سریال ۱۰ رقمی معتبر را وارد و ذخیره کنید.\n" +
                "۴. جستجو: از نوار بالا یا داخل بخش سریال برای یافتن سریال/پارت استفاده کنید.\n" +
                "۵. خروجی: از منوی فایل یا دکمه خروجی، لیست را به CSV ذخیره کنید.\n" +
                "۶. تنظیمات: مسیر دیتابیس و پشتیبان را مشخص کنید.\n\n" +
                "ورود پیش‌فرض: admin / 1234\n" +
                "در صورت خطا، فایل لاگ را از بخش به‌روزرسانی باز کنید.";
        }

        private void BtnOpenLog_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start("explorer.exe", AppLogger.GetLogFolder()); }
            catch { MessageBox.Show(AppLogger.GetLogFolder()); }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
