using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using ParsERP.Core.Services;

namespace ParsERP.Core.Data;

public class Database
{
    private readonly string _connectionString;
    public string DatabasePath { get; }

    public Database(string databasePath)
    {
        DatabasePath = databasePath;
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        Initialize();
    }

    public static Database FromConfig()
    {
        var config = AppConfig.Load();
        AppLogger.Info("Opening database: " + config.DatabasePath);
        return new Database(config.DatabasePath);
    }

    private void Initialize()
    {
        try
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = GetBootstrapSql();
            cmd.ExecuteNonQuery();
            AppLogger.Info("Database schema ensured.");
        }
        catch (Exception ex)
        {
            AppLogger.Error("Database Initialize failed", ex);
            throw;
        }
    }

    private static string GetBootstrapSql() => @"
PRAGMA foreign_keys = ON;
CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT, Description TEXT);
CREATE TABLE IF NOT EXISTS Roles (
    RoleId INTEGER PRIMARY KEY AUTOINCREMENT, RoleName TEXT NOT NULL UNIQUE,
    Description TEXT, IsSystem INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')));
CREATE TABLE IF NOT EXISTS Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL, FullName TEXT NOT NULL, Mobile TEXT, Email TEXT,
    RoleId INTEGER NOT NULL DEFAULT 1, IsActive INTEGER NOT NULL DEFAULT 1,
    LastLogin TEXT, CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')));
CREATE TABLE IF NOT EXISTS Products (
    ProductId INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL, NameEn TEXT, Unit TEXT NOT NULL DEFAULT 'عدد',
    ProductType TEXT NOT NULL DEFAULT 'Finished', HasSerial INTEGER NOT NULL DEFAULT 1,
    HasWarranty INTEGER NOT NULL DEFAULT 0, WarrantyMonths INTEGER DEFAULT 12,
    MinStock REAL DEFAULT 0, Description TEXT, IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')));
CREATE TABLE IF NOT EXISTS SerialNumbers (
    SerialId INTEGER PRIMARY KEY AUTOINCREMENT, ProductId INTEGER NOT NULL,
    SerialNo TEXT NOT NULL UNIQUE, ProductTypeCode TEXT NOT NULL,
    SolarYear INTEGER NOT NULL, SerialNumber INTEGER NOT NULL, CheckDigit INTEGER NOT NULL,
    PartNumber TEXT, Status TEXT NOT NULL DEFAULT 'InStock',
    ProductionDate TEXT, SaleDate TEXT, CustomerId INTEGER,
    WarrantyStart TEXT, WarrantyEnd TEXT, Notes TEXT, CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId));
CREATE UNIQUE INDEX IF NOT EXISTS IX_SerialNo ON SerialNumbers(SerialNo);
CREATE TABLE IF NOT EXISTS Warehouses (
    WarehouseId INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL, Location TEXT, IsActive INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS Parties (
    PartyId INTEGER PRIMARY KEY AUTOINCREMENT, PartyType TEXT NOT NULL,
    Code TEXT UNIQUE, NameFa TEXT NOT NULL, Mobile TEXT, Phone TEXT, Address TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1, CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')));
CREATE TABLE IF NOT EXISTS AuditLog (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, Action TEXT NOT NULL,
    Module TEXT, TableName TEXT, RecordId TEXT, Details TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')));
INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description, IsSystem) VALUES
(1, 'مدیر سیستم', 'دسترسی کامل', 1),
(2, 'مدیر تولید', 'تولید و سریال', 0),
(3, 'انباردار', 'انبار', 0);
INSERT OR IGNORE INTO Users (UserId, Username, PasswordHash, FullName, RoleId) VALUES
(1, 'admin', '1234', 'مدیر سیستم', 1);
INSERT OR IGNORE INTO Warehouses (WarehouseId, Code, NameFa) VALUES (1, 'MAIN', 'انبار اصلی');
INSERT OR IGNORE INTO Settings (Key, Value, Description) VALUES
('CompanyName', 'شرکت شما', 'نام شرکت'), ('Language', 'fa', 'زبان');
";

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
            VALUES (@pid, @sno, @ptc, @year, @num, @chk, 'InStock', @by, datetime('now','localtime'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@sno", serialNo);
        cmd.Parameters.AddWithValue("@ptc", productTypeCode);
        cmd.Parameters.AddWithValue("@year", solarYear);
        cmd.Parameters.AddWithValue("@num", serialNumber);
        cmd.Parameters.AddWithValue("@chk", checkDigit);
        cmd.Parameters.AddWithValue("@by", (object?)createdBy ?? DBNull.Value);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        AppLogger.Info($"Serial saved: {serialNo} (Id={id})");
        return id;
    }

    public List<(int Id, string Code, string NameFa)> GetActiveProducts()
    {
        var list = new List<(int, string, string)>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ProductId, Code, NameFa FROM Products WHERE IsActive = 1 ORDER BY Code";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetInt32(0), r.GetString(1), r.GetString(2)));
        return list;
    }

    public int SaveProduct(string code, string nameFa, string? nameEn, string unit,
        bool hasSerial, bool hasWarranty, int warrantyMonths, string? description, int? existingId = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (existingId == null)
        {
            cmd.CommandText = @"
                INSERT INTO Products (Code, NameFa, NameEn, Unit, HasSerial, HasWarranty, WarrantyMonths, Description)
                VALUES (@code, @name, @en, @unit, @hs, @hw, @wm, @desc);
                SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"
                UPDATE Products SET Code=@code, NameFa=@name, NameEn=@en, Unit=@unit,
                    HasSerial=@hs, HasWarranty=@hw, WarrantyMonths=@wm, Description=@desc
                WHERE ProductId=@id; SELECT @id;";
            cmd.Parameters.AddWithValue("@id", existingId.Value);
        }
        cmd.Parameters.AddWithValue("@code", code.Trim());
        cmd.Parameters.AddWithValue("@name", nameFa.Trim());
        cmd.Parameters.AddWithValue("@en", (object?)nameEn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@unit", string.IsNullOrWhiteSpace(unit) ? "عدد" : unit);
        cmd.Parameters.AddWithValue("@hs", hasSerial ? 1 : 0);
        cmd.Parameters.AddWithValue("@hw", hasWarranty ? 1 : 0);
        cmd.Parameters.AddWithValue("@wm", warrantyMonths);
        cmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<ProductRow> GetAllProducts()
    {
        var list = new List<ProductRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT ProductId, Code, NameFa, NameEn, Unit, HasSerial, HasWarranty, WarrantyMonths, IsActive
                            FROM Products ORDER BY Code";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ProductRow
            {
                ProductId = r.GetInt32(0),
                Code = r.GetString(1),
                NameFa = r.GetString(2),
                NameEn = r.IsDBNull(3) ? "" : r.GetString(3),
                Unit = r.GetString(4),
                HasSerial = r.GetInt32(5) == 1,
                HasWarranty = r.GetInt32(6) == 1,
                WarrantyMonths = r.GetInt32(7),
                IsActive = r.GetInt32(8) == 1
            });
        }
        return list;
    }
}

public class ProductRow
{
    public int ProductId { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string Unit { get; set; } = "عدد";
    public bool HasSerial { get; set; }
    public bool HasWarranty { get; set; }
    public int WarrantyMonths { get; set; }
    public bool IsActive { get; set; }
}
