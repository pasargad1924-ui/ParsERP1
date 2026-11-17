using System;
using System.Windows;
using System.Windows.Input;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class ProductsWindow : Window
    {
        private Database? _db;
        private int? _editId;

        public ProductsWindow()
        {
            InitializeComponent();
            try
            {
                _db = Database.FromConfig();
                Reload();
            }
            catch (Exception ex)
            {
                AppLogger.Error("ProductsWindow init", ex);
                MessageBox.Show("خطا در اتصال دیتابیس:\n" + ex.Message, "خطا",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reload()
        {
            if (_db == null) return;
            dgProducts.ItemsSource = _db.GetAllProducts();
            txtStatus.Text = "برای ویرایش روی ردیف دوبار کلیک کنید.";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                var code = txtCode.Text?.Trim() ?? "";
                if (code.Length != 2 || !int.TryParse(code, out _))
                {
                    MessageBox.Show("کد محصول باید دقیقاً ۲ رقم باشد (مثلاً 01).", "خطا",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtNameFa.Text))
                {
                    MessageBox.Show("نام فارسی الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int.TryParse(txtWarranty.Text, out int wm);
                if (wm <= 0) wm = 12;

                _db.SaveProduct(code, txtNameFa.Text.Trim(), null, txtUnit.Text?.Trim() ?? "عدد",
                    true, wm > 0, wm, null, _editId);
                AppLogger.Info($"Product saved: {code} - {txtNameFa.Text}");
                _editId = null;
                txtCode.Clear();
                txtNameFa.Clear();
                txtUnit.Text = "عدد";
                txtWarranty.Text = "12";
                Reload();
                txtStatus.Text = "محصول ذخیره شد.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Save product", ex);
                MessageBox.Show("خطا در ذخیره:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProducts.SelectedItem is ProductRow row)
            {
                _editId = row.ProductId;
                txtCode.Text = row.Code;
                txtNameFa.Text = row.NameFa;
                txtUnit.Text = row.Unit;
                txtWarranty.Text = row.WarrantyMonths.ToString();
                txtStatus.Text = $"در حال ویرایش: {row.NameFa}";
            }
        }
    }
}
