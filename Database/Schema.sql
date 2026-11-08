-- ParsERP Schema v1.1 - Serial 10-digit + configurable paths
PRAGMA foreign_keys = ON;
PRAGMA encoding = 'UTF-8';

CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT,
    Description TEXT
);

CREATE TABLE IF NOT EXISTS Roles (
    RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName TEXT NOT NULL UNIQUE,
    Description TEXT,
    IsSystem INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Mobile TEXT,
    Email TEXT,
    RoleId INTEGER NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    LastLogin TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE IF NOT EXISTS Products (
    ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL,
    NameEn TEXT,
    Unit TEXT NOT NULL DEFAULT N'عدد',
    ProductType TEXT NOT NULL DEFAULT 'Finished',
    HasSerial INTEGER NOT NULL DEFAULT 1,
    HasWarranty INTEGER NOT NULL DEFAULT 0,
    WarrantyMonths INTEGER DEFAULT 12,
    MinStock REAL DEFAULT 0,
    Description TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS SerialNumbers (
    SerialId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    SerialNo TEXT NOT NULL UNIQUE,
    ProductTypeCode TEXT NOT NULL,
    SolarYear INTEGER NOT NULL,
    SerialNumber INTEGER NOT NULL,
    CheckDigit INTEGER NOT NULL,
    Status TEXT NOT NULL DEFAULT 'InStock',
    ProductionDate TEXT,
    SaleDate TEXT,
    CustomerId INTEGER,
    WarrantyStart TEXT,
    WarrantyEnd TEXT,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_SerialNo ON SerialNumbers(SerialNo);

CREATE TABLE IF NOT EXISTS SerialBatches (
    BatchId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    SolarYear INTEGER NOT NULL,
    StartNumber INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    GeneratedCount INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE IF NOT EXISTS Warehouses (
    WarehouseId INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL,
    Location TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Stock (
    StockId INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL DEFAULT 0,
    UNIQUE(WarehouseId, ProductId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE IF NOT EXISTS ProductionOrders (
    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNo TEXT NOT NULL UNIQUE,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Draft',
    PlannedStart TEXT,
    PlannedEnd TEXT,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE IF NOT EXISTS OutsourcingOrders (
    OutId INTEGER PRIMARY KEY AUTOINCREMENT,
    OutNo TEXT NOT NULL UNIQUE,
    ProductionOrderId INTEGER,
    SupplierName TEXT NOT NULL,
    Description TEXT,
    Quantity REAL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    Cost REAL,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(OrderId)
);

CREATE TABLE IF NOT EXISTS AuditLog (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Action TEXT NOT NULL,
    TableName TEXT,
    RecordId TEXT,
    OldValue TEXT,
    NewValue TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description, IsSystem) VALUES 
(1, N'مدیر سیستم', N'دسترسی کامل', 1),
(2, N'مدیر تولید', N'تولید و سریال', 0),
(3, N'انباردار', N'انبار', 0),
(4, N'حسابدار', N'مالی', 0);

INSERT OR IGNORE INTO Settings (Key, Value, Description) VALUES
('CompanyName', N'شرکت شما', N'نام شرکت'),
('DatabasePath', '', N'مسیر دیتابیس'),
('BackupPath', '', N'مسیر پشتیبان'),
('Theme', 'Fresh', N'پوسته'),
('Language', 'fa', N'زبان');
