using System;
using System.IO;

namespace ParsERP.Core.Services;

/// <summary>
/// ثبت لاگ در فایل متنی کنار برنامه برای گزارش خطا
/// </summary>
public static class AppLogger
{
    private static readonly object _lock = new();
    private static string LogDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ParsERP", "Logs");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string LogFile =>
        Path.Combine(LogDirectory, $"ParsERP_{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        var full = message;
        if (ex != null)
            full += " | " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace;
        Write("ERROR", full);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // نادیده — لاگ نباید برنامه را بخواباند
        }
    }

    public static string GetLogFolder() => LogDirectory;
}
