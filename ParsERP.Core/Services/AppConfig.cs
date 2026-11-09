using System;
using System.IO;

namespace ParsERP.Core.Services;

/// <summary>
/// خواندن و نوشتن تنظیمات مسیر دیتابیس و بکاپ از فایل config.ini
/// </summary>
public class AppConfig
{
    private static string ConfigPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

    public string DatabasePath { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public string Language { get; set; } = "fa";

    public static AppConfig Load()
    {
        var config = new AppConfig();

        // مسیر پیش‌فرض
        string defaultData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ParsERP", "Data", "ParsERP.db");
        string defaultBackup = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ParsERP", "Backup");

        config.DatabasePath = defaultData;
        config.BackupPath = defaultBackup;

        if (!File.Exists(ConfigPath))
            return config;

        try
        {
            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    config.DatabasePath = trimmed.Substring(9).Trim();
                else if (trimmed.StartsWith("Backup=", StringComparison.OrdinalIgnoreCase))
                    config.BackupPath = trimmed.Substring(7).Trim();
                else if (trimmed.StartsWith("Language=", StringComparison.OrdinalIgnoreCase))
                    config.Language = trimmed.Substring(9).Trim();
            }
        }
        catch
        {
            // در صورت خطا از پیش‌فرض استفاده می‌شود
        }

        return config;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(ConfigPath,
            "[Paths]\r\n" +
            $"Database={DatabasePath}\r\n" +
            $"Backup={BackupPath}\r\n" +
            "\r\n[App]\r\n" +
            $"Language={Language}\r\n");
    }
}
