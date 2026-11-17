using System.Windows;
using System.Windows.Controls;

namespace ParsERP.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // بارگذاری داشبورد پیش‌فرض
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            txtPageTitle.Text = "داشبورد";
            // بعداً UserControl داشبورد اینجا قرار می‌گیرد
            MainContent.Content = CreateWelcomePanel();
        }

        private UIElement CreateWelcomePanel()
        {
            var panel = new StackPanel { Margin = new Thickness(0) };

            var title = new TextBlock
            {
                Text = "خوش آمدید به پارس‌ئی‌آرپی",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var subtitle = new TextBlock
            {
                Text = "سیستم یکپارچه مدیریت تولید، انبار، سریال، گارانتی و حسابداری",
                FontSize = 15,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 32)
            };

            // کارت‌های خلاصه
            var cards = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };

            cards.Children.Add(CreateStatCard("کالاهای فعال", "۰", "#0D9488"));
            cards.Children.Add(CreateStatCard("سریال‌های موجود", "۰", "#F97316"));
            cards.Children.Add(CreateStatCard("سفارش تولید باز", "۰", "#0284C7"));
            cards.Children.Add(CreateStatCard("گارانتی فعال", "۰", "#059669"));

            panel.Children.Add(title);
            panel.Children.Add(subtitle);
            panel.Children.Add(cards);

            return panel;
        }

        private void OpenSerialWindow()
        {
            var w = new SerialWindow();
            w.Owner = this;
            w.ShowDialog();
        }



        private void MenuProducts_Click(object sender, RoutedEventArgs e)
        {
            var w = new ProductsWindow { Owner = this };
            w.ShowDialog();
        }

        private void MenuSerials_Click(object sender, RoutedEventArgs e)
        {
            var w = new SerialWindow { Owner = this };
            w.ShowDialog();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var w = new SettingsWindow { Owner = this };
            w.ShowDialog();
        }


        private void GlobalSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            var q = txtGlobalSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(q)) return;
            var w = new SerialWindow(q) { Owner = this };
            w.ShowDialog();
        }

        private Border CreateStatCard(string title, string value, string color)
        {
            var border = new Border
            {
                Width = 200,
                Height = 110,
                Margin = new Thickness(0, 0, 16, 16),
                Background = (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                CornerRadius = new CornerRadius(12),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 2,
                    Opacity = 0.06
                }
            };

            var stack = new StackPanel { Margin = new Thickness(20) };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                Foreground = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush")
            });
            stack.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
                Margin = new Thickness(0, 8, 0, 0)
            });

            border.Child = stack;
            return border;
        }
    }
}
