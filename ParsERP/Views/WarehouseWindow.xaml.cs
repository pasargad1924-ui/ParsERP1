using System;
using System.Linq;
using System.Windows;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class WarehouseWindow : Window
    {
        private Database? _db;

        private class ProdItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public WarehouseWindow()
        {
            InitializeComponent();
            try
            {
                _db = Database.FromConfig();
                _db.EnsureWarehouse("MAIN", "انبار اصلی");
                LoadCombos();
                ReloadStock();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Warehouse init", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCombos()
        {
            if (_db == null) return;
            cmbWarehouse.ItemsSource = _db.GetWarehouses().Where(w => w.IsActive).ToList();
            if (cmbWarehouse.Items.Count > 0) cmbWarehouse.SelectedIndex = 0;

            cmbProduct.ItemsSource = _db.GetActiveProducts()
                .Select(p => new ProdItem { Id = p.Id, Name = p.Code + " - " + p.NameFa })
                .ToList();
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
        }

        private void ReloadStock()
        {
            if (_db == null) return;
            var list = _db.GetStockList(null);
            dgStock.ItemsSource = list;
            txtStatus.Text = $"{list.Count} ردیف موجودی";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => ReloadStock();

        private void BtnIn_Click(object sender, RoutedEventArgs e) => Apply("In");
        private void BtnOut_Click(object sender, RoutedEventArgs e) => Apply("Out");

        private void Apply(string type)
        {
            try
            {
                if (_db == null) return;
                if (cmbWarehouse.SelectedItem is not WarehouseRow wh)
                {
                    MessageBox.Show("انبار را انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (cmbProduct.SelectedItem is not ProdItem pr)
                {
                    MessageBox.Show("کالا را انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!double.TryParse(txtQty.Text?.Trim(), out double qty) || qty <= 0)
                {
                    MessageBox.Show("تعداد معتبر وارد کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _db.ApplyStockTransaction(wh.WarehouseId, pr.Id, type, qty, type == "In" ? "رسید دستی" : "حواله دستی", null);
                ReloadStock();
                txtStatus.Text = (type == "In" ? "رسید ثبت شد: " : "حواله ثبت شد: ") + qty;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Stock " + type, ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
