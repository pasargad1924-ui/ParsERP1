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
CREATE TABLE IF NOT EXISTS Stock (
    StockId INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL DEFAULT 0,
    UNIQUE(WarehouseId, ProductId)
);
CREATE TABLE IF NOT EXISTS StockTransactions (
    TransId INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    TransType TEXT NOT NULL,
    Quantity REAL NOT NULL,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
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

    public HashSet<int> GetUsedSerialNumbers(string productTypeCode, int solarYear)
    {
        var set = new HashSet<int>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT SerialNumber FROM SerialNumbers
                            WHERE ProductTypeCode = @c AND SolarYear = @y";
        cmd.Parameters.AddWithValue("@c", productTypeCode);
        cmd.Parameters.AddWithValue("@y", solarYear);
        using var r = cmd.ExecuteReader();
        while (r.Read()) set.Add(r.GetInt32(0));
        return set;
    }

    public int InsertSerialWithPart(int productId, string serialNo, string productTypeCode,
        int solarYear, int serialNumber, int checkDigit, string? partNumber, int? createdBy)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO SerialNumbers
            (ProductId, SerialNo, ProductTypeCode, SolarYear, SerialNumber, CheckDigit, PartNumber, Status, CreatedBy, CreatedAt)
            VALUES (@pid, @sno, @ptc, @year, @num, @chk, @part, 'InStock', @by, datetime('now','localtime'));
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@pid", productId);
        cmd.Parameters.AddWithValue("@sno", serialNo);
        cmd.Parameters.AddWithValue("@ptc", productTypeCode);
        cmd.Parameters.AddWithValue("@year", solarYear);
        cmd.Parameters.AddWithValue("@num", serialNumber);
        cmd.Parameters.AddWithValue("@chk", checkDigit);
        cmd.Parameters.AddWithValue("@part", (object?)partNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@by", (object?)createdBy ?? DBNull.Value);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        AppLogger.Info($"Serial saved: {serialNo} part={partNumber}");
        return id;
    }

    public List<SerialListItem> SearchSerials(string? query, int limit = 500)
    {
        var list = new List<SerialListItem>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrWhiteSpace(query))
        {
            cmd.CommandText = @"SELECT s.SerialId, s.SerialNo, s.ProductTypeCode, s.SolarYear,
                                       s.SerialNumber, s.CheckDigit, s.Status, s.PartNumber, p.NameFa, s.CreatedAt
                                FROM SerialNumbers s
                                LEFT JOIN Products p ON p.ProductId = s.ProductId
                                ORDER BY s.SerialId DESC LIMIT @lim";
            cmd.Parameters.AddWithValue("@lim", limit);
        }
        else
        {
            cmd.CommandText = @"SELECT s.SerialId, s.SerialNo, s.ProductTypeCode, s.SolarYear,
                                       s.SerialNumber, s.CheckDigit, s.Status, s.PartNumber, p.NameFa, s.CreatedAt
                                FROM SerialNumbers s
                                LEFT JOIN Products p ON p.ProductId = s.ProductId
                                WHERE s.SerialNo LIKE @q OR IFNULL(s.PartNumber,'') LIKE @q OR IFNULL(p.NameFa,'') LIKE @q
                                      OR s.ProductTypeCode LIKE @q OR s.Status LIKE @q
                                ORDER BY s.SerialId DESC LIMIT @lim";
            cmd.Parameters.AddWithValue("@q", "%" + query.Trim() + "%");
            cmd.Parameters.AddWithValue("@lim", limit);
        }
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new SerialListItem
            {
                SerialId = r.GetInt32(0),
                SerialNo = r.GetString(1),
                ProductTypeCode = r.GetString(2),
                SolarYear = r.GetInt32(3),
                SerialNumber = r.GetInt32(4),
                CheckDigit = r.GetInt32(5),
                Status = r.GetString(6),
                PartNumber = r.IsDBNull(7) ? "" : r.GetString(7),
                ProductName = r.IsDBNull(8) ? "" : r.GetString(8),
                CreatedAt = r.IsDBNull(9) ? "" : r.GetString(9)
            });
        }
        return list;
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

    public List<WarehouseRow> GetWarehouses()
    {
        var list = new List<WarehouseRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT WarehouseId, Code, NameFa, IFNULL(Location,''), IsActive FROM Warehouses ORDER BY Code";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new WarehouseRow { WarehouseId = r.GetInt32(0), Code = r.GetString(1), NameFa = r.GetString(2), Location = r.GetString(3), IsActive = r.GetInt32(4) == 1 });
        return list;
    }

    public int EnsureWarehouse(string code, string nameFa)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Warehouses (Code, NameFa) VALUES (@c, @n); SELECT WarehouseId FROM Warehouses WHERE Code=@c;";
        cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@n", nameFa);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<StockRow> GetStockList(int? warehouseId = null)
    {
        var list = new List<StockRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (warehouseId == null)
        {
            cmd.CommandText = @"SELECT s.StockId, s.WarehouseId, w.NameFa, s.ProductId, p.Code, p.NameFa, s.Quantity
                                FROM Stock s
                                JOIN Warehouses w ON w.WarehouseId = s.WarehouseId
                                JOIN Products p ON p.ProductId = s.ProductId
                                ORDER BY w.Code, p.Code";
        }
        else
        {
            cmd.CommandText = @"SELECT s.StockId, s.WarehouseId, w.NameFa, s.ProductId, p.Code, p.NameFa, s.Quantity
                                FROM Stock s
                                JOIN Warehouses w ON w.WarehouseId = s.WarehouseId
                                JOIN Products p ON p.ProductId = s.ProductId
                                WHERE s.WarehouseId = @w
                                ORDER BY p.Code";
            cmd.Parameters.AddWithValue("@w", warehouseId.Value);
        }
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new StockRow
            {
                StockId = r.GetInt32(0),
                WarehouseId = r.GetInt32(1),
                WarehouseName = r.GetString(2),
                ProductId = r.GetInt32(3),
                ProductCode = r.GetString(4),
                ProductName = r.GetString(5),
                Quantity = r.GetDouble(6)
            });
        }
        return list;
    }

    public void ApplyStockTransaction(int warehouseId, int productId, string transType, double quantity, string? notes, int? createdBy)
    {
        if (quantity == 0) throw new ArgumentException("تعداد نمی‌تواند صفر باشد.");
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        // موجودی
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO Stock (WarehouseId, ProductId, Quantity) VALUES (@w, @p, 0)
                                ON CONFLICT(WarehouseId, ProductId) DO NOTHING;";
            cmd.Parameters.AddWithValue("@w", warehouseId);
            cmd.Parameters.AddWithValue("@p", productId);
            cmd.ExecuteNonQuery();
        }
        double delta = transType is "In" or "PurchaseIn" or "ProductionIn" or "AdjustUp" ? Math.Abs(quantity) : -Math.Abs(quantity);
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Stock SET Quantity = Quantity + @d WHERE WarehouseId=@w AND ProductId=@p;";
            cmd.Parameters.AddWithValue("@d", delta);
            cmd.Parameters.AddWithValue("@w", warehouseId);
            cmd.Parameters.AddWithValue("@p", productId);
            cmd.ExecuteNonQuery();
        }
        // جلوگیری از موجودی منفی در خروج
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT Quantity FROM Stock WHERE WarehouseId=@w AND ProductId=@p;";
            cmd.Parameters.AddWithValue("@w", warehouseId);
            cmd.Parameters.AddWithValue("@p", productId);
            var q = Convert.ToDouble(cmd.ExecuteScalar());
            if (q < -0.0001) { tx.Rollback(); throw new InvalidOperationException("موجودی کافی نیست."); }
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO StockTransactions (WarehouseId, ProductId, TransType, Quantity, Notes, CreatedBy, CreatedAt)
                               VALUES (@w, @p, @t, @q, @n, @by, datetime('now','localtime'));";
            cmd.Parameters.AddWithValue("@w", warehouseId);
            cmd.Parameters.AddWithValue("@p", productId);
            cmd.Parameters.AddWithValue("@t", transType);
            cmd.Parameters.AddWithValue("@q", Math.Abs(quantity));
            cmd.Parameters.AddWithValue("@n", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@by", (object?)createdBy ?? DBNull.Value);
            try { cmd.ExecuteNonQuery(); }
            catch
            {
                // اگر جدول تراکنش در bootstrap نبود، فقط موجودی مهم است
            }
        }
        tx.Commit();
        AppLogger.Info($"Stock {transType} w={warehouseId} p={productId} q={quantity}");
    }

    public void TransferStock(int fromWarehouseId, int toWarehouseId, int productId, double quantity, string? notes, int? createdBy)
    {
        if (fromWarehouseId == toWarehouseId)
            throw new ArgumentException("انبار مبدأ و مقصد نباید یکی باشد.");
        if (quantity <= 0) throw new ArgumentException("تعداد نامعتبر است.");
        ApplyStockTransaction(fromWarehouseId, productId, "TransferOut", quantity, notes ?? "انتقال خروجی", createdBy);
        ApplyStockTransaction(toWarehouseId, productId, "TransferIn", quantity, notes ?? "انتقال ورودی", createdBy);
    }

    public List<StockTxRow> GetRecentStockTransactions(int limit = 100)
    {
        var list = new List<StockTxRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT t.TransId, t.WarehouseId, IFNULL(w.NameFa,''), t.ProductId, IFNULL(p.Code,''), IFNULL(p.NameFa,''),
                                   t.TransType, t.Quantity, IFNULL(t.Notes,''), t.CreatedAt
                            FROM StockTransactions t
                            LEFT JOIN Warehouses w ON w.WarehouseId = t.WarehouseId
                            LEFT JOIN Products p ON p.ProductId = t.ProductId
                            ORDER BY t.TransId DESC LIMIT @lim";
        cmd.Parameters.AddWithValue("@lim", limit);
        try
        {
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StockTxRow
                {
                    TransId = r.GetInt32(0),
                    WarehouseId = r.GetInt32(1),
                    WarehouseName = r.GetString(2),
                    ProductId = r.GetInt32(3),
                    ProductCode = r.GetString(4),
                    ProductName = r.GetString(5),
                    TransType = r.GetString(6),
                    Quantity = r.GetDouble(7),
                    Notes = r.GetString(8),
                    CreatedAt = r.IsDBNull(9) ? "" : r.GetString(9)
                });
            }
        }
        catch { /* جدول ممکن است خالی/جدید باشد */ }
        return list;
    }

    public int SaveParty(string partyType, string nameFa, string? code, string? mobile)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Parties (PartyType, Code, NameFa, Mobile, IsActive, CreatedAt)
                            VALUES (@t, @c, @n, @m, 1, datetime('now','localtime'));
                            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@t", partyType);
        cmd.Parameters.AddWithValue("@c", (object?)code ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@n", nameFa.Trim());
        cmd.Parameters.AddWithValue("@m", (object?)mobile ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<PartyRow> GetParties(string? partyType = null)
    {
        var list = new List<PartyRow>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        if (string.IsNullOrEmpty(partyType))
            cmd.CommandText = "SELECT PartyId, PartyType, IFNULL(Code,''), NameFa, IFNULL(Mobile,'') FROM Parties WHERE IsActive=1 ORDER BY NameFa";
        else
        {
            cmd.CommandText = "SELECT PartyId, PartyType, IFNULL(Code,''), NameFa, IFNULL(Mobile,'') FROM Parties WHERE IsActive=1 AND PartyType=@t ORDER BY NameFa";
            cmd.Parameters.AddWithValue("@t", partyType);
        }
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new PartyRow { PartyId = r.GetInt32(0), PartyType = r.GetString(1), Code = r.GetString(2), NameFa = r.GetString(3), Mobile = r.GetString(4) });
        return list;
    }
}

public class StockTxRow
{
    public int TransId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string TransType { get; set; } = "";
    public double Quantity { get; set; }
    public string Notes { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class PartyRow
{
    public int PartyId { get; set; }
    public string PartyType { get; set; } = "";
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string Mobile { get; set; } = "";
}

public class WarehouseRow
{
    public int WarehouseId { get; set; }
    public string Code { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string Location { get; set; } = "";
    public bool IsActive { get; set; }
}

public class StockRow
{
    public int StockId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public double Quantity { get; set; }
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

public class SerialListItem
{
    public int SerialId { get; set; }
    public string SerialNo { get; set; } = "";
    public string ProductTypeCode { get; set; } = "";
    public int SolarYear { get; set; }
    public int SerialNumber { get; set; }
    public int CheckDigit { get; set; }
    public string Status { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}
