using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class WarehouseWindow : UserControl
    {
        private Database? _db;
        private class ProdItem { public int Id { get; set; } public string Name { get; set; } = ""; }

        public WarehouseWindow()
        {
            InitializeComponent();
            try
            {
                _db = Database.FromConfig();
                _db.EnsureWarehouse("MAIN", "انبار اصلی");
                _db.EnsureWarehouse("PROD", "انبار تولید");
                LoadCombos();
                Reload();
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
            var wh = _db.GetWarehouses().Where(w => w.IsActive).ToList();
            cmbWarehouse.ItemsSource = wh;
            cmbWarehouseTo.ItemsSource = wh.ToList();
            if (wh.Count > 0) { cmbWarehouse.SelectedIndex = 0; cmbWarehouseTo.SelectedIndex = Math.Min(1, wh.Count - 1); }
            cmbProduct.ItemsSource = _db.GetActiveProducts()
                .Select(p => new ProdItem { Id = p.Id, Name = p.Code + " - " + p.NameFa }).ToList();
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
        }

        private void Reload()
        {
            if (_db == null) return;
            var stock = _db.GetStockList(null);
            dgStock.ItemsSource = stock;
            dgTx.ItemsSource = _db.GetRecentStockTransactions(200);
            txtStatus.Text = $"{stock.Count} ردیف موجودی";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => Reload();
        private void BtnIn_Click(object sender, RoutedEventArgs e) => Apply("In");
        private void BtnOut_Click(object sender, RoutedEventArgs e) => Apply("Out");

        private void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                if (cmbWarehouse.SelectedItem is not WarehouseRow from || cmbWarehouseTo.SelectedItem is not WarehouseRow to)
                { MessageBox.Show("انبار مبدأ و مقصد را انتخاب کنید."); return; }
                if (cmbProduct.SelectedItem is not ProdItem pr) { MessageBox.Show("کالا را انتخاب کنید."); return; }
                if (!double.TryParse(txtQty.Text?.Trim(), out double qty) || qty <= 0) { MessageBox.Show("تعداد نامعتبر."); return; }
                _db.TransferStock(from.WarehouseId, to.WarehouseId, pr.Id, qty, "انتقال دستی", null);
                Reload();
                txtStatus.Text = $"انتقال {qty} از {from.NameFa} به {to.NameFa}";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Transfer", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Apply(string type)
        {
            try
            {
                if (_db == null) return;
                if (cmbWarehouse.SelectedItem is not WarehouseRow wh) { MessageBox.Show("انبار را انتخاب کنید."); return; }
                if (cmbProduct.SelectedItem is not ProdItem pr) { MessageBox.Show("کالا را انتخاب کنید."); return; }
                if (!double.TryParse(txtQty.Text?.Trim(), out double qty) || qty <= 0) { MessageBox.Show("تعداد نامعتبر."); return; }
                _db.ApplyStockTransaction(wh.WarehouseId, pr.Id, type, qty, type == "In" ? "رسید دستی" : "حواله دستی", null);
                Reload();
                txtStatus.Text = (type == "In" ? "رسید: " : "حواله: ") + qty;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Stock " + type, ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "stock.csv" };
                if (dlg.ShowDialog() != true) return;
                var sb = new StringBuilder();
                sb.AppendLine("Warehouse,ProductCode,ProductName,Quantity");
                foreach (var s in _db.GetStockList(null))
                    sb.AppendLine($"{s.WarehouseName},{s.ProductCode},{s.ProductName},{s.Quantity}");
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                txtStatus.Text = "خروجی ذخیره شد.";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
