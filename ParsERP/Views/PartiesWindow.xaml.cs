using System;
using System.Windows;
using System.Windows.Controls;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class PartiesWindow : Window
    {
        private Database? _db;
        public PartiesWindow()
        {
            InitializeComponent();
            try { _db = Database.FromConfig(); Reload(); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void Reload()
        {
            if (_db == null) return;
            dgParties.ItemsSource = _db.GetParties(null);
            txtStatus.Text = "تأمین‌کننده برای خرید؛ مشتری برای فروش.";
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("نام الزامی است."); return; }
                var type = (cmbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Supplier";
                _db.SaveParty(type, txtName.Text.Trim(), txtCode.Text?.Trim(), txtMobile.Text?.Trim());
                txtName.Clear(); txtCode.Clear(); txtMobile.Clear();
                Reload();
                txtStatus.Text = "ذخیره شد.";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Save party", ex);
                MessageBox.Show(ex.Message);
            }
        }
    }
}
