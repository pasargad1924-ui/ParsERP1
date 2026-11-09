-- ============================================================
-- ParsERP - اسکیمای کامل v2.0
-- رهگیری سرتاسری: خرید → تولید → فروش → گارانتی/تعمیر
-- ============================================================
PRAGMA foreign_keys = ON;
PRAGMA encoding = 'UTF-8';

-- -------------------- تنظیمات و امنیت --------------------
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
    Department TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    LastLogin TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE IF NOT EXISTS Permissions (
    PermissionId INTEGER PRIMARY KEY AUTOINCREMENT,
    PermissionKey TEXT NOT NULL UNIQUE,
    TitleFa TEXT NOT NULL,
    Module TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS RolePermissions (
    RoleId INTEGER NOT NULL,
    PermissionId INTEGER NOT NULL,
    CanView INTEGER NOT NULL DEFAULT 1,
    CanCreate INTEGER NOT NULL DEFAULT 0,
    CanEdit INTEGER NOT NULL DEFAULT 0,
    CanDelete INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(PermissionId) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AuditLog (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Action TEXT NOT NULL,
    Module TEXT,
    TableName TEXT,
    RecordId TEXT,
    OldValue TEXT,
    NewValue TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- -------------------- اشخاص: تأمین‌کننده و مشتری --------------------
CREATE TABLE IF NOT EXISTS Parties (
    PartyId INTEGER PRIMARY KEY AUTOINCREMENT,
    PartyType TEXT NOT NULL,              -- Supplier / Customer / Both
    Code TEXT UNIQUE,
    NameFa TEXT NOT NULL,
    NationalId TEXT,
    EconomicCode TEXT,
    Mobile TEXT,
    Phone TEXT,
    Email TEXT,
    Address TEXT,
    City TEXT,
    PostalCode TEXT,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- -------------------- کالا و BOM --------------------
CREATE TABLE IF NOT EXISTS ProductCategories (
    CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId INTEGER,
    NameFa TEXT NOT NULL,
    Code TEXT,
    FOREIGN KEY (ParentId) REFERENCES ProductCategories(CategoryId)
);

CREATE TABLE IF NOT EXISTS Products (
    ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryId INTEGER,
    Code TEXT NOT NULL UNIQUE,            -- کد نوع محصول (۲ رقم سریال)
    NameFa TEXT NOT NULL,
    NameEn TEXT,
    Unit TEXT NOT NULL DEFAULT N'عدد',
    ProductType TEXT NOT NULL DEFAULT 'Finished', -- RawMaterial, SemiFinished, Finished, Service
    HasSerial INTEGER NOT NULL DEFAULT 0,
    HasWarranty INTEGER NOT NULL DEFAULT 0,
    WarrantyMonths INTEGER DEFAULT 12,
    MinStock REAL DEFAULT 0,
    Description TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CategoryId) REFERENCES ProductCategories(CategoryId)
);

CREATE TABLE IF NOT EXISTS BillOfMaterials (
    BomId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    Version TEXT DEFAULT '1.0',
    IsActive INTEGER NOT NULL DEFAULT 1,
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE IF NOT EXISTS BomItems (
    BomItemId INTEGER PRIMARY KEY AUTOINCREMENT,
    BomId INTEGER NOT NULL,
    ComponentId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    Unit TEXT,
    WastePercent REAL DEFAULT 0,
    FOREIGN KEY (BomId) REFERENCES BillOfMaterials(BomId) ON DELETE CASCADE,
    FOREIGN KEY (ComponentId) REFERENCES Products(ProductId)
);

-- -------------------- انبار --------------------
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

CREATE TABLE IF NOT EXISTS StockTransactions (
    TransId INTEGER PRIMARY KEY AUTOINCREMENT,
    WarehouseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    SerialId INTEGER,
    TransType TEXT NOT NULL,             -- PurchaseIn, SaleOut, ProductionIn, ProductionOut, Transfer, Adjust, Return
    Quantity REAL NOT NULL,
    UnitCost REAL,
    ReferenceType TEXT,                   -- PurchaseOrder, SalesOrder, ProductionOrder, ...
    ReferenceId INTEGER,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -------------------- خرید --------------------
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    PurchaseId INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseNo TEXT NOT NULL UNIQUE,
    SupplierId INTEGER NOT NULL,
    OrderDate TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Draft', -- Draft, Confirmed, Received, Cancelled
    TotalAmount REAL DEFAULT 0,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (SupplierId) REFERENCES Parties(PartyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS PurchaseOrderLines (
    LineId INTEGER PRIMARY KEY AUTOINCREMENT,
    PurchaseId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    UnitPrice REAL NOT NULL,
    WarehouseId INTEGER,
    Notes TEXT,
    FOREIGN KEY (PurchaseId) REFERENCES PurchaseOrders(PurchaseId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId)
);

-- -------------------- تولید --------------------
CREATE TABLE IF NOT EXISTS ProductionShifts (
    ShiftId INTEGER PRIMARY KEY AUTOINCREMENT,
    ShiftName TEXT NOT NULL,              -- صبح، عصر، شب
    StartTime TEXT,
    EndTime TEXT
);

CREATE TABLE IF NOT EXISTS ProductionOrders (
    OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderNo TEXT NOT NULL UNIQUE,
    ProductId INTEGER NOT NULL,
    Quantity REAL NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Draft', -- Draft, Released, InProgress, Completed, Cancelled
    PlannedStart TEXT,
    PlannedEnd TEXT,
    ActualStart TEXT,
    ActualEnd TEXT,
    ShiftId INTEGER,
    WarehouseId INTEGER,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (ShiftId) REFERENCES ProductionShifts(ShiftId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS ProductionWorkers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    RoleInOrder TEXT,                    -- اپراتور، سرپرست، کنترل کیفیت
    HoursWorked REAL,
    FOREIGN KEY (OrderId) REFERENCES ProductionOrders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS OutsourcingOrders (
    OutId INTEGER PRIMARY KEY AUTOINCREMENT,
    OutNo TEXT NOT NULL UNIQUE,
    ProductionOrderId INTEGER,
    SupplierId INTEGER,
    SupplierName TEXT,
    Description TEXT,
    Quantity REAL,
    Status TEXT NOT NULL DEFAULT 'Pending',
    Cost REAL,
    StartDate TEXT,
    EndDate TEXT,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(OrderId),
    FOREIGN KEY (SupplierId) REFERENCES Parties(PartyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -------------------- سریال (قلب رهگیری) --------------------
CREATE TABLE IF NOT EXISTS SerialNumbers (
    SerialId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    SerialNo TEXT NOT NULL UNIQUE,        -- ۱۰ رقم
    ProductTypeCode TEXT NOT NULL,
    SolarYear INTEGER NOT NULL,
    SerialNumber INTEGER NOT NULL,
    CheckDigit INTEGER NOT NULL,
    PartNumber TEXT,                      -- پارت‌نامبر داخلی/مشتری
    Status TEXT NOT NULL DEFAULT 'InStock',
    -- منشأ
    PurchaseId INTEGER,
    PurchaseLineId INTEGER,
    ProductionOrderId INTEGER,
    -- فروش
    SalesOrderId INTEGER,
    CustomerId INTEGER,
    SaleDate TEXT,
    SalePrice REAL,
    -- گارانتی
    WarrantyStart TEXT,
    WarrantyEnd TEXT,
    ProductionDate TEXT,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (PurchaseId) REFERENCES PurchaseOrders(PurchaseId),
    FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(OrderId),
    FOREIGN KEY (CustomerId) REFERENCES Parties(PartyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_SerialNo ON SerialNumbers(SerialNo);

CREATE TABLE IF NOT EXISTS SerialBatches (
    BatchId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    SolarYear INTEGER NOT NULL,
    StartNumber INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    GeneratedCount INTEGER NOT NULL DEFAULT 0,
    ProductionOrderId INTEGER,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (ProductionOrderId) REFERENCES ProductionOrders(OrderId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- -------------------- فروش و ارسال --------------------
CREATE TABLE IF NOT EXISTS SalesOrders (
    SalesId INTEGER PRIMARY KEY AUTOINCREMENT,
    SalesNo TEXT NOT NULL UNIQUE,
    CustomerId INTEGER NOT NULL,
    OrderDate TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Draft', -- Draft, Confirmed, Shipped, Delivered, Cancelled
    TotalAmount REAL DEFAULT 0,
    ShippingMethod TEXT,
    ShippingTrackingNo TEXT,
    ShippingDate TEXT,
    ShippingAddress TEXT,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CustomerId) REFERENCES Parties(PartyId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS SalesOrderLines (
    LineId INTEGER PRIMARY KEY AUTOINCREMENT,
    SalesId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    SerialId INTEGER,
    Quantity REAL NOT NULL,
    UnitPrice REAL NOT NULL,
    WarehouseId INTEGER,
    Notes TEXT,
    FOREIGN KEY (SalesId) REFERENCES SalesOrders(SalesId) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (SerialId) REFERENCES SerialNumbers(SerialId),
    FOREIGN KEY (WarehouseId) REFERENCES Warehouses(WarehouseId)
);

-- -------------------- گارانتی و تعمیر --------------------
CREATE TABLE IF NOT EXISTS WarrantyClaims (
    ClaimId INTEGER PRIMARY KEY AUTOINCREMENT,
    SerialId INTEGER NOT NULL,
    ClaimDate TEXT NOT NULL,
    ClaimType TEXT,                       -- Warranty, OutOfWarranty, Repair
    Description TEXT,
    Status TEXT NOT NULL DEFAULT 'Open',  -- Open, InProgress, Resolved, Rejected
    Resolution TEXT,
    RepairCount INTEGER DEFAULT 1,
    Cost REAL DEFAULT 0,
    ResolvedAt TEXT,
    ReceivedBy INTEGER,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (SerialId) REFERENCES SerialNumbers(SerialId),
    FOREIGN KEY (ReceivedBy) REFERENCES Users(UserId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS RepairHistory (
    RepairId INTEGER PRIMARY KEY AUTOINCREMENT,
    ClaimId INTEGER NOT NULL,
    SerialId INTEGER NOT NULL,
    RepairDate TEXT NOT NULL,
    TechnicianId INTEGER,
    ActionTaken TEXT,
    PartsUsed TEXT,
    Cost REAL DEFAULT 0,
    Result TEXT,
    Notes TEXT,
    FOREIGN KEY (ClaimId) REFERENCES WarrantyClaims(ClaimId),
    FOREIGN KEY (SerialId) REFERENCES SerialNumbers(SerialId),
    FOREIGN KEY (TechnicianId) REFERENCES Users(UserId)
);

-- -------------------- حسابداری پایه --------------------
CREATE TABLE IF NOT EXISTS AccountGroups (
    GroupId INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL,
    Type TEXT NOT NULL                     -- Asset, Liability, Equity, Revenue, Expense
);

CREATE TABLE IF NOT EXISTS Accounts (
    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
    GroupId INTEGER NOT NULL,
    Code TEXT NOT NULL UNIQUE,
    NameFa TEXT NOT NULL,
    IsDetail INTEGER NOT NULL DEFAULT 1,
    ParentId INTEGER,
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (GroupId) REFERENCES AccountGroups(GroupId),
    FOREIGN KEY (ParentId) REFERENCES Accounts(AccountId)
);

CREATE TABLE IF NOT EXISTS JournalEntries (
    EntryId INTEGER PRIMARY KEY AUTOINCREMENT,
    EntryNo TEXT NOT NULL UNIQUE,
    EntryDate TEXT NOT NULL,
    Description TEXT,
    Status TEXT NOT NULL DEFAULT 'Draft',
    ReferenceType TEXT,
    ReferenceId INTEGER,
    CreatedBy INTEGER,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS JournalEntryLines (
    LineId INTEGER PRIMARY KEY AUTOINCREMENT,
    EntryId INTEGER NOT NULL,
    AccountId INTEGER NOT NULL,
    Debit REAL NOT NULL DEFAULT 0,
    Credit REAL NOT NULL DEFAULT 0,
    Description TEXT,
    FOREIGN KEY (EntryId) REFERENCES JournalEntries(EntryId) ON DELETE CASCADE,
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId)
);

-- -------------------- داده اولیه --------------------
INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description, IsSystem) VALUES
(1, N'مدیر سیستم', N'دسترسی کامل به همه بخش‌ها و رهگیری کامل', 1),
(2, N'مدیر تولید', N'تولید، سریال، برون‌سپاری، شیفت', 0),
(3, N'انباردار', N'انبار، ورود و خروج کالا', 0),
(4, N'کارشناس خرید', N'سفارش خرید و تأمین‌کنندگان', 0),
(5, N'کارشناس فروش', N'فروش، مشتری، ارسال', 0),
(6, N'حسابدار', N'اسناد مالی و گزارش‌ها', 0),
(7, N'خدمات پس از فروش', N'گارانتی و تعمیرات', 0),
(8, N'کاربر تولید', N'ثبت کارکرد شیفت (محدود)', 0);

INSERT OR IGNORE INTO Permissions (PermissionKey, TitleFa, Module) VALUES
('Dashboard.View', N'مشاهده داشبورد', 'System'),
('Products.View', N'مشاهده کالا', 'Products'),
('Products.Edit', N'ویرایش کالا', 'Products'),
('Serial.View', N'مشاهده سریال', 'Serial'),
('Serial.Generate', N'تولید سریال', 'Serial'),
('Serial.FullTrace', N'رهگیری کامل محصول', 'Serial'),
('Purchase.View', N'مشاهده خرید', 'Purchase'),
('Purchase.Edit', N'ثبت/ویرایش خرید', 'Purchase'),
('Production.View', N'مشاهده تولید', 'Production'),
('Production.Edit', N'ثبت تولید', 'Production'),
('Sales.View', N'مشاهده فروش', 'Sales'),
('Sales.Edit', N'ثبت فروش', 'Sales'),
('Warehouse.View', N'مشاهده انبار', 'Warehouse'),
('Warehouse.Edit', N'عملیات انبار', 'Warehouse'),
('Warranty.View', N'مشاهده گارانتی', 'Warranty'),
('Warranty.Edit', N'ثبت ادعای گارانتی', 'Warranty'),
('Accounting.View', N'مشاهده حسابداری', 'Accounting'),
('Accounting.Edit', N'ثبت سند', 'Accounting'),
('Users.Manage', N'مدیریت کاربران و دسترسی', 'System'),
('Settings.Manage', N'تنظیمات سیستم', 'System'),
('Search.Global', N'جستجوی سراسری', 'System');

INSERT OR IGNORE INTO ProductionShifts (ShiftId, ShiftName, StartTime, EndTime) VALUES
(1, N'صبح', '07:00', '15:00'),
(2, N'عصر', '15:00', '23:00'),
(3, N'شب', '23:00', '07:00');

INSERT OR IGNORE INTO Warehouses (WarehouseId, Code, NameFa) VALUES
(1, 'MAIN', N'انبار اصلی');

INSERT OR IGNORE INTO Products (ProductId, Code, NameFa, HasSerial, HasWarranty, WarrantyMonths, IsActive) VALUES
(1, '01', N'محصول پیش‌فرض', 1, 1, 12, 1);

INSERT OR IGNORE INTO Settings (Key, Value, Description) VALUES
('CompanyName', N'شرکت شما', N'نام شرکت'),
('DatabasePath', '', N'مسیر دیتابیس'),
('BackupPath', '', N'مسیر پشتیبان'),
('Theme', 'Fresh', N'پوسته'),
('Language', 'fa', N'زبان');
