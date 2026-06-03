-- =============================================
-- Online Shop Database - SQL Server Setup
-- Database: Online_Shop
-- =============================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Online_Shop')
    CREATE DATABASE Online_Shop;
GO

USE Online_Shop;
GO

-- =============================================
-- DROP TABLES (if re-running)
-- =============================================
IF OBJECT_ID('OrderItems',     'U') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders',         'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('CartItems',      'U') IS NOT NULL DROP TABLE CartItems;
IF OBJECT_ID('ProductImages',  'U') IS NOT NULL DROP TABLE ProductImages;
IF OBJECT_ID('Reviews',        'U') IS NOT NULL DROP TABLE Reviews;
IF OBJECT_ID('Products',       'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Categories',     'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Customers',      'U') IS NOT NULL DROP TABLE Customers;
IF OBJECT_ID('Addresses',      'U') IS NOT NULL DROP TABLE Addresses;
IF OBJECT_ID('Coupons',        'U') IS NOT NULL DROP TABLE Coupons;
GO

-- =============================================
-- TABLE: Categories
-- =============================================
CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100)  NOT NULL,
    Description  NVARCHAR(500)  NULL,
    ImageUrl     NVARCHAR(300)  NULL,
    IsActive     BIT            NOT NULL DEFAULT 1,
    SortOrder    INT            NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- TABLE: Products
-- =============================================
CREATE TABLE Products (
    ProductId    INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId   INT            NOT NULL REFERENCES Categories(CategoryId),
    Name         NVARCHAR(200)  NOT NULL,
    Description  NVARCHAR(2000) NULL,
    Price        DECIMAL(18,2)  NOT NULL,
    OldPrice     DECIMAL(18,2)  NULL,
    Stock        INT            NOT NULL DEFAULT 0,
    SKU          NVARCHAR(50)   NOT NULL UNIQUE,
    ImageUrl     NVARCHAR(300)  NULL,
    IsFeatured   BIT            NOT NULL DEFAULT 0,
    IsActive     BIT            NOT NULL DEFAULT 1,
    Rating       DECIMAL(3,2)   NOT NULL DEFAULT 0,
    SoldCount    INT            NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- TABLE: ProductImages
-- =============================================
CREATE TABLE ProductImages (
    ImageId    INT IDENTITY(1,1) PRIMARY KEY,
    ProductId  INT           NOT NULL REFERENCES Products(ProductId) ON DELETE CASCADE,
    ImageUrl   NVARCHAR(300) NOT NULL,
    SortOrder  INT           NOT NULL DEFAULT 0
);

-- =============================================
-- TABLE: Customers
-- =============================================
CREATE TABLE Customers (
    CustomerId   INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150)  NOT NULL,
    Email        NVARCHAR(200)  NOT NULL UNIQUE,
    Phone        NVARCHAR(20)   NULL,
    PasswordHash NVARCHAR(500)  NOT NULL,
    AvatarUrl    NVARCHAR(300)  NULL,
    IsActive     BIT            NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- TABLE: Addresses
-- =============================================
CREATE TABLE Addresses (
    AddressId    INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId   INT            NOT NULL REFERENCES Customers(CustomerId) ON DELETE CASCADE,
    FullName     NVARCHAR(150)  NOT NULL,
    Phone        NVARCHAR(20)   NOT NULL,
    Street       NVARCHAR(300)  NOT NULL,
    City         NVARCHAR(100)  NOT NULL,
    Province     NVARCHAR(100)  NOT NULL,
    Country      NVARCHAR(100)  NOT NULL DEFAULT 'Cambodia',
    IsDefault    BIT            NOT NULL DEFAULT 0
);

-- =============================================
-- TABLE: Coupons
-- =============================================
CREATE TABLE Coupons (
    CouponId      INT IDENTITY(1,1) PRIMARY KEY,
    Code          NVARCHAR(50)   NOT NULL UNIQUE,
    DiscountType  NVARCHAR(20)   NOT NULL CHECK (DiscountType IN ('Percent','Fixed')),
    DiscountValue DECIMAL(18,2)  NOT NULL,
    MinOrderAmt   DECIMAL(18,2)  NOT NULL DEFAULT 0,
    UsageLimit    INT            NULL,
    UsedCount     INT            NOT NULL DEFAULT 0,
    ExpiryDate    DATETIME2      NULL,
    IsActive      BIT            NOT NULL DEFAULT 1
);

-- =============================================
-- TABLE: Orders
-- =============================================
CREATE TABLE Orders (
    OrderId        INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId     INT            NULL REFERENCES Customers(CustomerId),
    OrderNumber    NVARCHAR(30)   NOT NULL UNIQUE,
    Status         NVARCHAR(30)   NOT NULL DEFAULT 'Pending'
                   CHECK (Status IN ('Pending','Processing','Shipped','Delivered','Cancelled','Refunded')),
    PaymentMethod  NVARCHAR(50)   NOT NULL DEFAULT 'Cash',
    PaymentStatus  NVARCHAR(20)   NOT NULL DEFAULT 'Unpaid'
                   CHECK (PaymentStatus IN ('Unpaid','Paid','Refunded')),
    SubTotal       DECIMAL(18,2)  NOT NULL,
    Discount       DECIMAL(18,2)  NOT NULL DEFAULT 0,
    ShippingFee    DECIMAL(18,2)  NOT NULL DEFAULT 0,
    Total          DECIMAL(18,2)  NOT NULL,
    CouponCode     NVARCHAR(50)   NULL,
    -- Shipping info snapshot
    ShipName       NVARCHAR(150)  NOT NULL,
    ShipPhone      NVARCHAR(20)   NOT NULL,
    ShipAddress    NVARCHAR(400)  NOT NULL,
    Note           NVARCHAR(500)  NULL,
    CreatedAt      DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt      DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- TABLE: OrderItems
-- =============================================
CREATE TABLE OrderItems (
    OrderItemId  INT IDENTITY(1,1) PRIMARY KEY,
    OrderId      INT            NOT NULL REFERENCES Orders(OrderId) ON DELETE CASCADE,
    ProductId    INT            NOT NULL REFERENCES Products(ProductId),
    ProductName  NVARCHAR(200)  NOT NULL,
    ProductImage NVARCHAR(300)  NULL,
    Quantity     INT            NOT NULL,
    UnitPrice    DECIMAL(18,2)  NOT NULL,
    TotalPrice   DECIMAL(18,2)  NOT NULL
);

-- =============================================
-- TABLE: CartItems  (session-based guest cart)
-- =============================================
CREATE TABLE CartItems (
    CartItemId   INT IDENTITY(1,1) PRIMARY KEY,
    SessionId    NVARCHAR(100)  NOT NULL,
    CustomerId   INT            NULL REFERENCES Customers(CustomerId),
    ProductId    INT            NOT NULL REFERENCES Products(ProductId),
    Quantity     INT            NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- TABLE: Reviews
-- =============================================
CREATE TABLE Reviews (
    ReviewId    INT IDENTITY(1,1) PRIMARY KEY,
    ProductId   INT            NOT NULL REFERENCES Products(ProductId) ON DELETE CASCADE,
    CustomerId  INT            NULL REFERENCES Customers(CustomerId),
    ReviewerName NVARCHAR(100) NOT NULL,
    Rating      INT            NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment     NVARCHAR(1000) NULL,
    IsApproved  BIT            NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2      NOT NULL DEFAULT GETDATE()
);
GO

-- =============================================
-- INDEXES
-- =============================================
CREATE INDEX IX_Products_CategoryId  ON Products(CategoryId);
CREATE INDEX IX_Products_IsFeatured  ON Products(IsFeatured);
CREATE INDEX IX_Orders_CustomerId    ON Orders(CustomerId);
CREATE INDEX IX_OrderItems_OrderId   ON OrderItems(OrderId);
CREATE INDEX IX_CartItems_SessionId  ON CartItems(SessionId);
CREATE INDEX IX_Reviews_ProductId    ON Reviews(ProductId);
GO

-- =============================================
-- SAMPLE DATA: Categories
-- =============================================
INSERT INTO Categories (Name, Description, ImageUrl, SortOrder) VALUES
('Electronics',    'Phones, Laptops, Gadgets & Accessories',   'https://images.unsplash.com/photo-1498049794561-7780e7231661?w=400', 1),
('Fashion',        'Men & Women Clothing, Shoes & Bags',       'https://images.unsplash.com/photo-1445205170230-053b83016050?w=400', 2),
('Home & Garden',  'Furniture, Decor & Kitchen Essentials',    'https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=400', 3),
('Sports',         'Fitness Equipment & Outdoor Gear',         'https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=400', 4),
('Beauty',         'Skincare, Makeup & Personal Care',         'https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=400', 5),
('Books',          'Bestsellers, Textbooks & Magazines',       'https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=400', 6),
('Toys & Kids',    'Educational Toys, Games & Baby Products',  'https://images.unsplash.com/photo-1558060370-d644479cb6f7?w=400', 7),
('Food & Beverage','Snacks, Drinks & Health Supplements',      'https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=400', 8);
GO

-- =============================================
-- SAMPLE DATA: Products
-- =============================================
INSERT INTO Products (CategoryId, Name, Description, Price, OldPrice, Stock, SKU, ImageUrl, IsFeatured, Rating, SoldCount) VALUES
-- Electronics
(1,'iPhone 15 Pro Max 256GB','Latest Apple flagship with titanium design, A17 Pro chip, 48MP camera system and USB-C.',1299.00,1499.00,50,'ELEC-IPH-001','https://images.unsplash.com/photo-1696446701796-da61225697cc?w=400',1,4.8,320),
(1,'Samsung Galaxy S24 Ultra','200MP camera, built-in S Pen, Snapdragon 8 Gen 3, 5000mAh battery.',1199.00,1299.00,40,'ELEC-SAM-002','https://images.unsplash.com/photo-1610945264803-c22b62831892?w=400',1,4.7,280),
(1,'MacBook Air M3 13"','Apple M3 chip, 8GB RAM, 256GB SSD, 18-hour battery life, fanless design.',1099.00,1199.00,30,'ELEC-MBA-003','https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=400',1,4.9,150),
(1,'Sony WH-1000XM5 Headphones','Industry-leading noise cancellation, 30-hour battery, multipoint connection.',349.00,399.00,80,'ELEC-SNY-004','https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?w=400',0,4.8,420),
(1,'iPad Pro 11" M4','Ultra-thin design, OLED display, Apple Pencil Pro support, Wi-Fi 6E.',999.00,NULL,25,'ELEC-IPD-005','https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=400',1,4.7,95),
(1,'DJI Mini 4 Pro Drone','4K/60fps video, obstacle sensing, 34-min flight time, under 249g.',759.00,799.00,20,'ELEC-DJI-006','https://images.unsplash.com/photo-1473968512647-3e447244af8f?w=400',0,4.6,65),

-- Fashion
(2,'Nike Air Max 270','Lightweight design with large Air unit for all-day comfort, multiple colors.',120.00,150.00,100,'FASH-NIK-001','https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400',1,4.5,890),
(2,'Levi''s 501 Original Jeans','Classic straight-leg fit, 100% cotton denim, timeless style.',79.00,99.00,150,'FASH-LEV-002','https://images.unsplash.com/photo-1542272604-787c3835535d?w=400',0,4.4,1200),
(2,'Adidas Ultraboost 22','Responsive Boost cushioning, Primeknit upper, Continental rubber outsole.',160.00,180.00,60,'FASH-ADI-003','https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=400',0,4.6,540),
(2,'Classic Leather Handbag','Genuine leather, multiple compartments, gold hardware, timeless design.',199.00,249.00,35,'FASH-BAG-004','https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=400',0,4.3,210),

-- Home & Garden
(3,'IKEA MALM Queen Bed Frame','Clean-line design with 4 storage boxes, solid birch, easy assembly.',329.00,399.00,15,'HOME-BED-001','https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=400',0,4.2,88),
(3,'Dyson V15 Detect Vacuum','Laser detects hidden dust, 60-min run time, HEPA filtration.',649.00,749.00,22,'HOME-DYS-002','https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400',1,4.7,175),
(3,'Instant Pot Duo 7-in-1','Pressure cooker, slow cooker, rice cooker, steamer, sauté, yogurt maker.',89.00,109.00,45,'HOME-INS-003','https://images.unsplash.com/photo-1585515320310-259814833e62?w=400',0,4.6,630),
(3,'Philips Hue Starter Kit','4 smart bulbs + Bridge, 16M colors, voice control, energy saving.',179.00,199.00,40,'HOME-PHI-004','https://images.unsplash.com/photo-1558618047-f4e60cde4383?w=400',0,4.5,290),

-- Sports
(4,'Peloton Bike+ Smart Exercise Bike','22" HD touchscreen, auto-resistance, Bluetooth, live & on-demand classes.',2495.00,2895.00,8,'SPRT-PEL-001','https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400',0,4.4,42),
(4,'Garmin Forerunner 965','AMOLED display, GPS, heart rate, VO2 max, 31-day battery.',599.00,649.00,30,'SPRT-GAR-002','https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=400',1,4.7,185),
(4,'Yoga Mat Premium 6mm','Eco-friendly TPE, non-slip surface, alignment lines, carrying strap.',45.00,59.00,120,'SPRT-YOG-003','https://images.unsplash.com/photo-1575052814086-f385e2e2ad1b?w=400',0,4.5,870),

-- Beauty
(5,'Fenty Beauty Foundation','40 shades, long-wearing formula, SPF 15, natural finish.',36.00,NULL,200,'BEAU-FEN-001','https://images.unsplash.com/photo-1631730486784-74757d38e27a?w=400',0,4.6,1450),
(5,'The Ordinary Niacinamide 10%','Reduces blemishes, pore size, brightens skin tone, 30ml serum.',6.90,NULL,500,'BEAU-TON-002','https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?w=400',1,4.7,3200),
(5,'Dyson Airwrap Multi-Styler','Curl, wave, smooth and dry with no extreme heat, multiple attachments.',599.00,649.00,18,'BEAU-DYS-003','https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=400',1,4.5,340),

-- Books
(6,'Atomic Habits by James Clear','Practical strategies to build good habits and break bad ones. Bestseller.',18.00,24.00,300,'BOOK-AHA-001','https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400',0,4.8,2100),
(6,'The Alchemist by Paulo Coelho','A magical fable about following your dreams. Over 65M copies sold.',12.00,16.00,250,'BOOK-ALC-002','https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=400',0,4.7,1800),

-- Toys & Kids
(7,'LEGO Technic Bugatti Chiron','3,599 pieces, 1:8 scale, functional gearbox, V16 engine. Age 16+.',449.00,499.00,12,'TOYS-LEG-001','https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400',0,4.9,78),
(7,'Nintendo Switch OLED','7" OLED screen, enhanced audio, 64GB storage, wide adjustable stand.',349.00,399.00,35,'TOYS-NIN-002','https://images.unsplash.com/photo-1578303512597-81e6cc155b3e?w=400',1,4.8,420),

-- Food & Beverage
(8,'Nescafe Gold Blend 200g','Premium instant coffee, smooth & balanced flavour, glass jar.',12.00,15.00,400,'FOOD-NES-001','https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400',0,4.4,1600),
(8,'Optimum Nutrition Whey Protein','5lb Gold Standard 100% Whey, 24g protein per serving, chocolate.',65.00,75.00,80,'FOOD-OPT-002','https://images.unsplash.com/photo-1579722820308-d74e571900a9?w=400',0,4.6,560);
GO

-- =============================================
-- SAMPLE DATA: Customers
-- =============================================
INSERT INTO Customers (FullName, Email, Phone, PasswordHash, IsActive) VALUES
('Sophea Chan',    'sophea@gmail.com',   '012345678', 'AQAAAAEAACcQAAAAENhash1==', 1),
('Dara Pich',      'dara@gmail.com',     '011234567', 'AQAAAAEAACcQAAAAENhash2==', 1),
('Lina Sok',       'lina@gmail.com',     '093456789', 'AQAAAAEAACcQAAAAENhash3==', 1),
('Borey Heng',     'borey@gmail.com',    '095678901', 'AQAAAAEAACcQAAAAENhash4==', 1),
('Sreymom Keo',    'sreymom@gmail.com',  '097890123', 'AQAAAAEAACcQAAAAENhash5==', 1);
GO

-- =============================================
-- SAMPLE DATA: Addresses
-- =============================================
INSERT INTO Addresses (CustomerId, FullName, Phone, Street, City, Province, IsDefault) VALUES
(1,'Sophea Chan',  '012345678','#12 St 271, Toul Tom Poung',    'Phnom Penh', 'Phnom Penh', 1),
(2,'Dara Pich',    '011234567','#45 Norodom Blvd',              'Phnom Penh', 'Phnom Penh', 1),
(3,'Lina Sok',     '093456789','#7 Samdech Pan Ave',            'Siem Reap',  'Siem Reap',  1),
(4,'Borey Heng',   '095678901','#22 Charles de Gaulle Blvd',    'Phnom Penh', 'Phnom Penh', 1),
(5,'Sreymom Keo',  '097890123','#88 Kampuchea Krom Blvd',       'Phnom Penh', 'Phnom Penh', 1);
GO

-- =============================================
-- SAMPLE DATA: Coupons
-- =============================================
INSERT INTO Coupons (Code, DiscountType, DiscountValue, MinOrderAmt, UsageLimit, ExpiryDate) VALUES
('WELCOME10',  'Percent', 10, 0,    1000, '2025-12-31'),
('SAVE20',     'Percent', 20, 100,  500,  '2025-06-30'),
('FLAT50',     'Fixed',   50, 200,  200,  '2025-09-30'),
('VIP30',      'Percent', 30, 500,  100,  '2025-12-31'),
('FREESHIP',   'Fixed',   15, 50,   NULL, '2025-12-31');
GO

-- =============================================
-- SAMPLE DATA: Orders
-- =============================================
INSERT INTO Orders (CustomerId, OrderNumber, Status, PaymentMethod, PaymentStatus, SubTotal, Discount, ShippingFee, Total, ShipName, ShipPhone, ShipAddress, CreatedAt) VALUES
(1,'ORD-20240101-0001','Delivered','Credit Card','Paid',  1299.00,129.90,5.00,1174.10,'Sophea Chan', '012345678','#12 St 271, Phnom Penh','2024-01-15'),
(2,'ORD-20240102-0002','Shipped',  'Cash',       'Unpaid',  199.00,  0.00,5.00, 204.00,'Dara Pich',   '011234567','#45 Norodom Blvd, Phnom Penh','2024-02-10'),
(3,'ORD-20240103-0003','Processing','ABA Pay',   'Paid',   406.90,  0.00,5.00, 411.90,'Lina Sok',    '093456789','#7 Samdech Pan Ave, Siem Reap','2024-03-05'),
(4,'ORD-20240104-0004','Pending',  'Cash',       'Unpaid',  160.00,  0.00,5.00, 165.00,'Borey Heng',  '095678901','#22 Charles de Gaulle, Phnom Penh','2024-03-20'),
(1,'ORD-20240105-0005','Delivered','Credit Card','Paid',  1298.00,259.60,0.00,1038.40,'Sophea Chan', '012345678','#12 St 271, Phnom Penh','2024-04-01'),
(5,'ORD-20240106-0006','Cancelled','Cash',       'Unpaid',   89.00,  0.00,5.00,  94.00,'Sreymom Keo', '097890123','#88 Kampuchea Krom, Phnom Penh','2024-04-15');
GO

-- =============================================
-- SAMPLE DATA: OrderItems
-- =============================================
INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductImage, Quantity, UnitPrice, TotalPrice) VALUES
(1,1,'iPhone 15 Pro Max 256GB','https://images.unsplash.com/photo-1696446701796-da61225697cc?w=400',1,1299.00,1299.00),
(2,10,'Classic Leather Handbag','https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=400',1,199.00,199.00),
(3,19,'The Ordinary Niacinamide 10%','https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?w=400',2,6.90,13.80),
(3,18,'Fenty Beauty Foundation','https://images.unsplash.com/photo-1631730486784-74757d38e27a?w=400',1,36.00,36.00),
(3,3,'MacBook Air M3 13"','https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=400',1,1099.00,1099.00),
(4,9,'Adidas Ultraboost 22','https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=400',1,160.00,160.00),
(5,2,'Samsung Galaxy S24 Ultra','https://images.unsplash.com/photo-1610945264803-c22b62831892?w=400',1,1199.00,1199.00),
(5,19,'The Ordinary Niacinamide 10%','https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?w=400',3,6.90,20.70),
(5,21,'Atomic Habits by James Clear','https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=400',2,18.00,36.00),
(6,13,'Instant Pot Duo 7-in-1','https://images.unsplash.com/photo-1585515320310-259814833e62?w=400',1,89.00,89.00);
GO

-- =============================================
-- SAMPLE DATA: Reviews
-- =============================================
INSERT INTO Reviews (ProductId, CustomerId, ReviewerName, Rating, Comment, IsApproved) VALUES
(1, 1,'Sophea Chan',  5,'Absolutely love this phone! The camera is incredible and battery lasts all day.', 1),
(1, 2,'Dara Pich',    4,'Great phone but very expensive. The titanium feel is premium though.', 1),
(3, 3,'Lina Sok',     5,'The M3 chip is blazing fast. Perfect for my design work.', 1),
(7, 4,'Borey Heng',   5,'Most comfortable shoes I have ever worn. Worth every penny!', 1),
(19,5,'Sreymom Keo',  5,'Amazing serum! My skin cleared up within 2 weeks. Will buy again.', 1),
(21,1,'Sophea Chan',  5,'This book changed my life. Highly recommend to everyone.', 1),
(4, 2,'Dara Pich',    4,'Noise cancellation is top notch. Bit pricey but worth it.', 1),
(12,3,'Lina Sok',     5,'Best vacuum ever. The laser mode is mind blowing!', 1);
GO

PRINT '=============================================';
PRINT 'Online_Shop database created successfully!';
PRINT 'Tables: 10 | Products: 26 | Orders: 6';
PRINT '=============================================';
