-- =====================================================
-- ParsERP - اسکیما دیتابیس نسخه ۱.۰
-- پشتیبانی کامل از RTL، سریال، گارانتی، تولید، انبار، حسابداری پایه و دسترسی نقش‌محور
-- =====================================================

PRAGMA foreign_keys = ON;
PRAGMA encoding = 'UTF-8';

-- -----------------------------
-- ۱. کاربران و سطح دسترسی
-- -----------------------------
CREATE TABLE Roles (
    RoleId          INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName        TEXT NOT NULL UNIQUE,          -- مدیر سیستم، مدیر تولید، انباردار، حسابدار، بازرس، کارمند پروژه و ...
    Description     TEXT,
    IsSystem        INTEGER NOT NULL DEFAULT 0,    -- نقش‌های سیستمی قابل حذف نیستند
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE Users (
    UserId          INTEGER PRIMARY KEY AUTOINCREMENT,
    Username        TEXT NOT NULL UNIQUE,
    PasswordHash    TEXT NOT NULL,                 -- Argon2 یا BCrypt
    FullName        TEXT NOT NULL,
    Mobile          TEXT,
    Email           TEXT,
    RoleId          INTEGER NOT NULL,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    LastLogin       TEXT,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE Permissions (
    PermissionId    INTEGER PRIMARY KEY AUTOINCREMENT,
    PermissionKey   TEXT NOT NULL UNIQUE,          -- e.g. Production.View, Serial.Generate, Accounting.Post
    TitleFa         TEXT NOT NULL,
    Module          TEXT NOT NULL                  -- Production, Warehouse, Accounting, Project, Inspection, System
);

CREATE TABLE RolePermissions (
    RoleId          INTEGER NOT NULL,
    PermissionId    INTEGER NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId) ON DELETE CASCADE
);

-- -----------------------------
-- ۲. کالا، سریال و گارانتی
-- -----------------------------
CREATE TABLE ProductCategories (
    CategoryId      INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId        INTEGER,
    NameFa          TEXT NOT NULL,
    Code            TEXT,
    FOREIGN KEY (ParentId) REFERENCES ProductCategories(CategoryId)
);

CREATE TABLE Products (
    ProductId       INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryId      INTEGER,
    Code            TEXT NOT NULL UNIQUE,
    NameFa          TEXT NOT NULL,
    NameEn          TEXT,
    Unit            TEXT NOT NULL DEFAULT 'عدد',   -- عدد، متر، کیلو و ...
    ProductType     TEXT NOT NULL,                -- RawMaterial, SemiFinished, Finished, Service
    HasSerial       INTEGER NOT NULL DEFAULT 0,
    HasWarranty     INTEGER NOT NULL DEFAULT 0,
    WarrantyMonths  INTEGER DEFAULT 12,
    MinStock        REAL DEFAULT 0,
    Description     TEXT,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CategoryId) REFERENCES ProductCategories(CategoryId)
);

CREATE TABLE SerialNumbers (
    SerialId        INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId       INTEGER NOT NULL,
    SerialNo        TEXT NOT NULL UNIQUE,
    Status          TEXT NOT NULL DEFAULT 'InStock', -- InStock, Sold, InProduction, UnderWarranty, Returned, Scrapped
    ProductionDate  TEXT,
    SaleDate        TEXT,
    CustomerId      INTEGER,
    WarrantyStart   TEXT,
    WarrantyEnd     TEXT,
    Notes           TEXT,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE WarrantyClaims (
    ClaimId         INTEGER PRIMARY KEY AUTOINCREMENT,
    SerialId        INTEGER NOT NULL,
    ClaimDate       TEXT NOT NULL,
    Description     TEXT,
    Status          TEXT NOT NULL DEFAULT 'Open',  -- Open, InProgress, Resolved, Rejected
    Resolution      TEXT,
    ResolvedAt      TEXT,
    CreatedBy       INTEGER,
    FOREIGN KEY (SerialId) REFERENCES SerialNumbers(SerialId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -----------------------------
-- ۳. انبار
-- -----------------------------
CREATE TABLE Warehouses (
    WarehouseId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Code            TEXT NOT NULL UNIQUE,
    NameFa          TEXT NOT NULL,
    Location        TEXT,
    IsActive        INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE Stock (
    StockId         INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId     INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    Quantity        REAL NOT NULL DEFAULT 0,
    UNIQUE(WarehouseId, ProductId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE StockTransactions (
    TransId         INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId     INTEGER NOT NULL,
    ProductId       INTEGER NOT NULL,
    SerialId        INTEGER,                       -- اگر کالا سریالی باشد
    TransType       TEXT NOT NULL,                -- In, Out, Transfer, Adjust, ProductionIn, ProductionOut
    Quantity        REAL NOT NULL,
    ReferenceType   TEXT,                         -- Purchase, Sale, ProductionOrder, ...
    ReferenceId     INTEGER,
    Notes           TEXT,
    CreatedBy       INTEGER,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (SerialId) REFERENCES SerialNumbers(SerialId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -----------------------------
-- ۴. تولید و برون‌سپاری
-- -----------------------------
CREATE TABLE BillOfMaterials (
    BomId           INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId       INTEGER NOT NULL,             -- محصول نهایی یا نیمه‌ساخته
    Version         TEXT DEFAULT '1.0',
    IsActive        INTEGER NOT NULL DEFAULT 1,
    Notes           TEXT,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE BomItems (
    BomItemId       INTEGER PRIMARY KEY AUTOINCREMENT,
    BomId           INTEGER NOT NULL,
    ComponentId     INTEGER NOT NULL,             -- ماده اولیه یا قطعه
    Quantity        REAL NOT NULL,
    Unit            TEXT,
    WastePercent    REAL DEFAULT 0,
    FOREIGN KEY (BomId) REFERENCES BillOfMaterials(BomId) ON DELETE CASCADE,
    FOREIGN KEY (ComponentId) REFERENCES Products(ProductId)
);

CREATE TABLE ProductionOrders (
    OrderId         INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNo         TEXT NOT NULL UNIQUE,
    ProductId       INTEGER NOT NULL,
    Quantity        REAL NOT NULL,
    Status          TEXT NOT NULL DEFAULT 'Draft', -- Draft, Released, InProgress, Completed, Cancelled
    PlannedStart    TEXT,
    PlannedEnd      TEXT,
    ActualStart     TEXT,
    ActualEnd       TEXT,
    WarehouseId     INTEGER,                      -- انبار مقصد محصول
    Notes           TEXT,
    CreatedBy       INTEGER,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE OutsourcingOrders (
    OutId           INTEGER PRIMARY KEY AUTOINCREMENT,
    OutNo           TEXT NOT NULL UNIQUE,
    ProductionOrderId INTEGER,
    SupplierName    TEXT NOT NULL,
    Description     TEXT,
    Quantity        REAL,
    Status          TEXT NOT NULL DEFAULT 'Pending',
    Cost            REAL,
    StartDate       TEXT,
    EndDate         TEXT,
    Notes           TEXT,
    CreatedBy       INTEGER,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(OrderId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -----------------------------
-- ۵. حسابداری پایه (آماده برای انطباق با سامانه مودیان)
-- -----------------------------
CREATE TABLE AccountGroups (
    GroupId         INTEGER PRIMARY KEY AUTOINCREMENT,
    Code            TEXT NOT NULL UNIQUE,
    NameFa          TEXT NOT NULL,
    Type            TEXT NOT NULL                  -- Asset, Liability, Equity, Revenue, Expense
);

CREATE TABLE Accounts (
    AccountId       INTEGER PRIMARY KEY AUTOINCREMENT,
    GroupId         INTEGER NOT NULL,
    Code            TEXT NOT NULL UNIQUE,
    NameFa          TEXT NOT NULL,
    IsDetail        INTEGER NOT NULL DEFAULT 1,
    ParentId        INTEGER,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (GroupId) REFERENCES AccountGroups(GroupId),
    FOREIGN KEY (ParentId) REFERENCES Accounts(AccountId)
);

CREATE TABLE JournalEntries (
    EntryId         INTEGER PRIMARY KEY AUTOINCREMENT,
    EntryNo         TEXT NOT NULL UNIQUE,
    EntryDate       TEXT NOT NULL,
    Description     TEXT,
    Status          TEXT NOT NULL DEFAULT 'Draft', -- Draft, Posted
    CreatedBy       INTEGER,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE JournalEntryLines (
    LineId          INTEGER PRIMARY KEY AUTOINCREMENT,
    EntryId         INTEGER NOT NULL,
    AccountId       INTEGER NOT NULL,
    Debit           REAL NOT NULL DEFAULT 0,
    Credit          REAL NOT NULL DEFAULT 0,
    Description     TEXT,
    FOREIGN KEY (EntryId) REFERENCES JournalEntries(EntryId) ON DELETE CASCADE,
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId)
);

-- -----------------------------
-- ۶. تنظیمات سیستم و لاگ
-- -----------------------------
CREATE TABLE Settings (
    Key             TEXT PRIMARY KEY,
    Value           TEXT,
    Description     TEXT
);

CREATE TABLE AuditLog (
    LogId           INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId          INTEGER,
    Action          TEXT NOT NULL,
    TableName       TEXT,
    RecordId        TEXT,
    OldValue        TEXT,
    NewValue        TEXT,
    IpAddress       TEXT,
    CreatedAt       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- -----------------------------
-- داده اولیه
-- -----------------------------
INSERT INTO Roles (RoleName, Description, IsSystem) VALUES 
('مدیر سیستم', 'دسترسی کامل به تمام بخش‌ها', 1),
('مدیر تولید', 'مدیریت تولید، سریال و برون‌سپاری', 0),
('انباردار', 'مدیریت انبار و موجودی', 0),
('حسابدار', 'اسناد حسابداری و گزارش‌های مالی', 0),
('کاربر عادی', 'دسترسی محدود مشاهده', 0);

INSERT INTO Settings (Key, Value, Description) VALUES
('CompanyName', 'شرکت شما', 'نام شرکت'),
('SerialPrefix', 'PRS', 'پیشوند شماره سریال'),
('SerialLength', '8', 'طول بخش عددی سریال'),
('Theme', 'Fresh', 'پوسته رنگی برنامه'),
('Language', 'fa', 'زبان رابط کاربری');
