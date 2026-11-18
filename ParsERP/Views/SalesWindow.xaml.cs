using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class SalesWindow : UserControl
    {
        private Database? _db;
        private readonly ObservableCollection<Line> _lines = new();
        private class Prod { public int Id { get; set; } public string Name { get; set; } = ""; }
        public class Line { public int ProductId { get; set; } public string Name { get; set; } = ""; public double Qty { get; set; } public double Price { get; set; } public string Serial { get; set; } = ""; }

        public SalesWindow()
        {
            InitializeComponent();
            dgLines.ItemsSource = _lines;
            try
            {
                _db = Database.FromConfig();
                var customers = _db.GetParties("Customer").Concat(_db.GetParties("Both")).ToList();
                foreach (var c in customers) c.PartyTypeFa = Database.PartyTypeToFa(c.PartyType);
                cmbCustomer.ItemsSource = customers;
                if (cmbCustomer.Items.Count > 0) cmbCustomer.SelectedIndex = 0;
                cmbWarehouse.ItemsSource = _db.GetWarehouses().Where(w => w.IsActive).ToList();
                if (cmbWarehouse.Items.Count > 0) cmbWarehouse.SelectedIndex = 0;
                cmbProduct.ItemsSource = _db.GetActiveProducts().Select(p => new Prod { Id = p.Id, Name = p.Code + " - " + p.NameFa }).ToList();
                if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProduct.SelectedItem is not Prod pr) return;
            if (!double.TryParse(txtQty.Text, out var q) || q <= 0) { MessageBox.Show("تعداد نامعتبر"); return; }
            if (!double.TryParse(txtPrice.Text, out var p) || p < 0) { MessageBox.Show("قیمت نامعتبر"); return; }
            _lines.Add(new Line { ProductId = pr.Id, Name = pr.Name, Qty = q, Price = p, Serial = txtSerial.Text?.Trim() ?? "" });
            txtStatus.Text = $"جمع: {_lines.Sum(x => x.Qty * x.Price):N0}";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                if (cmbCustomer.SelectedItem is not PartyRow cu) { MessageBox.Show("مشتری را انتخاب کنید (اشخاص)."); return; }
                if (_lines.Count == 0) { MessageBox.Show("ردیفی نیست."); return; }
                int? wh = cmbWarehouse.SelectedItem is WarehouseRow w ? w.WarehouseId : 1;
                var lines = _lines.Select(l => (l.ProductId, l.Qty, l.Price, wh, (string?)l.Serial)).ToList();
                _db.CreateSalesOrder(cu.PartyId, DateTime.Now.ToString("yyyy-MM-dd"), txtNotes.Text, null, lines, chkStock.IsChecked == true);
                MessageBox.Show("فروش ثبت شد.", "موفق");
                _lines.Clear();
                txtStatus.Text = "ثبت شد.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Sales", ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
