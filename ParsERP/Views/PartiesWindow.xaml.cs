using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ParsERP.Core.Data;
using ParsERP.Core.Services;

namespace ParsERP.Views
{
    public partial class PartiesWindow : UserControl
    {
        private Database? _db;
        private readonly string? _initialQuery;
        private PartyRow? _selected;

        public PartiesWindow(string? initialQuery = null)
        {
            _initialQuery = initialQuery;
            InitializeComponent();
            try
            {
                _db = Database.FromConfig();
                if (!string.IsNullOrWhiteSpace(_initialQuery))
                    txtFilter.Text = _initialQuery;
                Reload();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Parties init", ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void Reload()
        {
            if (_db == null) return;
            var q = txtFilter.Text?.Trim() ?? "";
            var list = string.IsNullOrEmpty(q) ? _db.GetParties(null) : _db.SearchParties(q);
            foreach (var p in list)
                if (string.IsNullOrEmpty(p.PartyTypeFa))
                    p.PartyTypeFa = Database.PartyTypeToFa(p.PartyType);
            dgParties.ItemsSource = list;
            txtStatus.Text = $"{list.Count} مورد";
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e) => Reload();

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_db == null) return;
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("نام الزامی است."); return; }
                var typeFa = (cmbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "تأمین‌کننده";
                var type = Database.PartyTypeFromFa(typeFa);
                double? lat = double.TryParse(txtLat.Text, out var la) ? la : null;
                double? lng = double.TryParse(txtLng.Text, out var ln) ? ln : null;
                int id = _db.SavePartyFull(type, txtName.Text.Trim(), txtCode.Text?.Trim(), txtMobile.Text?.Trim(),
                    txtAddress.Text?.Trim(), null, null, lat, lng, null);
                if (!string.IsNullOrWhiteSpace(txtExtraPhone.Text) && txtExtraPhone.Text.Contains("|"))
                {
                    var parts = txtExtraPhone.Text.Split('|');
                    _db.AddPartyPhone(id, parts[0].Trim(), parts[1].Trim());
                }
                else if (!string.IsNullOrWhiteSpace(txtExtraPhone.Text))
                    _db.AddPartyPhone(id, "اضافی", txtExtraPhone.Text.Trim());
                if (!string.IsNullOrWhiteSpace(txtAddress.Text))
                    _db.AddPartyAddress(id, "اصلی", txtAddress.Text.Trim(), lat, lng);
                Reload();
                txtStatus.Text = "ذخیره شد: " + txtName.Text;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Save party", ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void DgParties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = dgParties.SelectedItem as PartyRow;
            if (_selected == null) return;
            txtName.Text = _selected.NameFa;
            txtCode.Text = _selected.Code;
            txtMobile.Text = _selected.Mobile;
            txtAddress.Text = _selected.PrimaryAddress;
            txtLat.Text = _selected.Latitude?.ToString() ?? "";
            txtLng.Text = _selected.Longitude?.ToString() ?? "";
            foreach (ComboBoxItem it in cmbType.Items)
                if ((it.Content?.ToString() ?? "") == (_selected.PartyTypeFa != "" ? _selected.PartyTypeFa : Database.PartyTypeToFa(_selected.PartyType)))
                    cmbType.SelectedItem = it;
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private string? CurrentMobile() =>
            !string.IsNullOrWhiteSpace(txtMobile.Text) ? txtMobile.Text.Trim() : _selected?.Mobile;

        private void BtnSms_Click(object sender, RoutedEventArgs e)
        {
            var m = CurrentMobile();
            if (string.IsNullOrEmpty(m)) { MessageBox.Show("شماره موبایل نیست."); return; }
            OpenUrl("sms:" + m);
        }

        private void BtnCall_Click(object sender, RoutedEventArgs e)
        {
            var m = CurrentMobile();
            if (string.IsNullOrEmpty(m)) { MessageBox.Show("شماره نیست."); return; }
            OpenUrl("tel:" + m);
        }

        private void BtnWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            var m = CurrentMobile()?.Replace(" ", "").Replace("-", "") ?? "";
            if (m.StartsWith("0")) m = "98" + m.Substring(1);
            if (string.IsNullOrEmpty(m)) { MessageBox.Show("شماره نیست."); return; }
            OpenUrl("https://wa.me/" + m);
        }

        private void BtnGoogle_Click(object sender, RoutedEventArgs e) => OpenMap("google");
        private void BtnNeshan_Click(object sender, RoutedEventArgs e) => OpenMap("neshan");
        private void BtnBalad_Click(object sender, RoutedEventArgs e) => OpenMap("balad");

        private void OpenMap(string provider)
        {
            double? lat = double.TryParse(txtLat.Text, out var la) ? la : _selected?.Latitude;
            double? lng = double.TryParse(txtLng.Text, out var ln) ? ln : _selected?.Longitude;
            string addr = txtAddress.Text?.Trim() ?? _selected?.PrimaryAddress ?? "";
            string url;
            if (lat != null && lng != null)
            {
                url = provider switch
                {
                    "neshan" => $"https://neshan.org/maps/@{lat},{lng},17z",
                    "balad" => $"https://balad.ir/location?latitude={lat}&longitude={lng}",
                    _ => $"https://www.google.com/maps/search/?api=1&query={lat},{lng}"
                };
            }
            else if (!string.IsNullOrWhiteSpace(addr))
            {
                var q = Uri.EscapeDataString(addr);
                url = provider switch
                {
                    "neshan" => "https://neshan.org/maps/search/" + q,
                    "balad" => "https://balad.ir/?q=" + q,
                    _ => "https://www.google.com/maps/search/?api=1&query=" + q
                };
            }
            else
            {
                MessageBox.Show("آدرس یا مختصات وارد نشده است.");
                return;
            }
            OpenUrl(url);
        }
    }
}
