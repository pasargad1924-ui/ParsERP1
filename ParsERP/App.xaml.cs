using System;
using System.Windows;
using System.Windows.Threading;
using ParsERP.Core.Services;

namespace ParsERP
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppLogger.Info("=== ParsERP starting ===");

            DispatcherUnhandledException += (_, args) =>
            {
                AppLogger.Error("DispatcherUnhandledException", args.Exception);
                MessageBox.Show(
                    "خطای پیش‌بینی‌نشده رخ داد.\n\n" + args.Exception.Message +
                    "\n\nجزئیات در پوشه لاگ ذخیره شد:\n" + AppLogger.GetLogFolder(),
                    "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    AppLogger.Error("UnhandledException", ex);
            };
        }
    }
}
