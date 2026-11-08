using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using ParsERP.Core.Services;

namespace ParsERP.Core.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(string databasePath)
    {
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={databasePath}";
        Initialize();
    }

    public static Database FromConfig()
    {
        var config = AppConfig.Load();
        return new Database(config.DatabasePath);
    }

    private void Initialize()
    {
        using var conn = Open();
        // اجرای اسکیما در صورت نبود جداول
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schema.sql");
        if (File.Exists(schemaPath))
        {
            var sql = File.ReadAllText(schemaPath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    public SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public bool SerialExists(string serialNo)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM SerialNumbers WHERE SerialNo = @s";
        cmd.Parameters.AddWithValue("@s", serialNo);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int InsertSerial(int productId, string serialNo, string productTypeCode,
        int solarYear, int serialNumber, int checkDigit, int? createdBy)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO SerialNumbers
            (ProductId, SerialNo, ProductTypeCode, SolarYear, SerialNumber, CheckDigit, Status, CreatedBy, CreatedAt)
            VALUES
            (@pid, @sno, @ptc, @year, @num, @chk, 'InStock', @by, datetime('now','localtime'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@sno", serialNo);
        cmd.Parameters.AddWithValue("@ptc", productTypeCode);
        cmd.Parameters.AddWithValue("@year", solarYear);
        cmd.Parameters.AddWithValue("@num", serialNumber);
        cmd.Parameters.AddWithValue("@chk", checkDigit);
        cmd.Parameters.AddWithValue("@by", (object?)createdBy ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
