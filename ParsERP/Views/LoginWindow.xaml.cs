using System.Windows;
using System.Windows.Input;

namespace ParsERP.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e) => AttemptLogin();

        private void AttemptLogin()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("نام کاربری و رمز عبور را وارد کنید.");
                return;
            }

            if (username.Equals("admin", System.StringComparison.OrdinalIgnoreCase) && password == "1234")
            {
                var main = new MainWindow();
                main.Show();
                Close();
            }
            else
            {
                ShowError("نام کاربری یا رمز عبور اشتباه است.");
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AttemptLogin();
            base.OnKeyDown(e);
        }
    }
}
