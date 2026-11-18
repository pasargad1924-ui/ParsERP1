using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class PurchaseWindow : UserControl
    {
        private Database? _db;
        private readonly ObservableCollection<LineRow> _lines = new();

        private class ProdItem { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; }
        public class LineRow
        {
            public int ProductId { get; set; }
            public string ProductCode { get; set; } = "";
            public string ProductName { get; set; } = "";
            public double Qty { get; set; }
            public double UnitPrice { get; set; }
            public double LineTotal => Qty * UnitPrice;
        }

        public PurchaseWindow()
        {
            InitializeComponent();
            dgLines.ItemsSource = _lines;
            try
            {
                _db = Database.FromConfig();
                _db.EnsureWarehouse("MAIN", "انبار اصلی");
                LoadCombos();
                RefreshListHint();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Purchase init", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCombos()
        {
            if (_db == null) return;
            var suppliers = _db.GetParties("Supplier");
            var both = _db.GetParties("Both");
            cmbSupplier.ItemsSource = suppliers.Concat(both).ToList();
            if (cmbSupplier.Items.Count > 0) cmbSupplier.SelectedIndex = 0;

            cmbWarehouse.ItemsSource = _db.GetWarehouses().Where(w => w.IsActive).ToList();
            if (cmbWarehouse.Items.Count > 0) cmbWarehouse.SelectedIndex = 0;

            cmbProduct.ItemsSource = _db.GetActiveProducts()
                .Select(p => new ProdItem { Id = p.Id, Code = p.Code, Name = p.Code + " - " + p.NameFa }).ToList();
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
        }

        private void RefreshListHint()
        {
            if (_db == null) return;
            var recent = _db.GetPurchases(5);
            txtStatus.Text = recent.Count == 0
                ? "هنوز سفارشی ثبت نشده. ابتدا تأمین‌کننده را در «اشخاص» تعریف کنید."
                : $"آخرین خرید: {recent[0].PurchaseNo} — {recent[0].SupplierName} — {recent[0].TotalAmount:N0}";
        }

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProduct.SelectedItem is not ProdItem pr)
            {
                MessageBox.Show("کالا را انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(txtQty.Text?.Trim(), out double qty) || qty <= 0)
            {
                MessageBox.Show("تعداد نامعتبر است.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(txtPrice.Text?.Trim(), out double price) || price < 0)
            {
                MessageBox.Show("قیمت نامعتبر است.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _lines.Add(new LineRow
            {
                ProductId = pr.Id,
                ProductCode = pr.Code,
                ProductName = pr.Name,
                Qty = qty,
                UnitPrice = price
            });
            txtStatus.Text = $"{_lines.Count} ردیف — جمع کل: {_lines.Sum(x => x.LineTotal):N0}";
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _lines.Clear();
            txtStatus.Text = "ردیف‌ها پاک شد.";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                if (cmbSupplier.SelectedItem is not PartyRow sup)
                {
                    MessageBox.Show("تأمین‌کننده را انتخاب کنید.\nاگر لیست خالی است از منوی اشخاص یک Supplier بسازید.",
                        "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_lines.Count == 0)
                {
                    MessageBox.Show("حداقل یک ردیف کالا اضافه کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int? whId = null;
                if (cmbWarehouse.SelectedItem is WarehouseRow wh) whId = wh.WarehouseId;

                var lines = _lines.Select(l => (l.ProductId, l.Qty, l.UnitPrice, whId)).ToList();
                bool receive = chkReceive.IsChecked == true;
                int id = _db.CreatePurchaseOrder(sup.PartyId, DateTime.Now.ToString("yyyy-MM-dd"),
                    txtNotes.Text?.Trim(), null, lines, receive);

                MessageBox.Show(
                    receive
                        ? "سفارش خرید ثبت و رسید انبار انجام شد."
                        : "سفارش خرید ثبت شد (بدون رسید انبار).",
                    "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                _lines.Clear();
                RefreshListHint();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Save purchase", ex);
                MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
