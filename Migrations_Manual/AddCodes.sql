-- Migration: Add auto-generated code columns
-- Run ONCE against your SQL Server database
-- Safe to re-run (uses IF NOT EXISTS / IF COL_NOT_EXISTS checks)

-- 1. Categories.CategoryCode
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Categories' AND COLUMN_NAME = 'CategoryCode'
)
BEGIN
    ALTER TABLE Categories ADD CategoryCode NVARCHAR(10) NOT NULL DEFAULT '';
    PRINT 'Added Categories.CategoryCode';
END

-- 2. Products: rename SKU → ProductCode
--    Step 1: add new column
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'ProductCode'
)
BEGIN
    ALTER TABLE Products ADD ProductCode NVARCHAR(20) NOT NULL DEFAULT '';

    -- Copy existing SKU values into ProductCode so no data is lost
    UPDATE Products SET ProductCode = SKU WHERE ProductCode = '';

    -- Add unique index on ProductCode
    CREATE UNIQUE INDEX IX_Products_ProductCode ON Products(ProductCode);

    PRINT 'Added Products.ProductCode (copied from SKU)';
END

-- 3. Customers.CardCode
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Customers' AND COLUMN_NAME = 'CardCode'
)
BEGIN
    ALTER TABLE Customers ADD CardCode NVARCHAR(15) NOT NULL DEFAULT '';
    PRINT 'Added Customers.CardCode';
END

PRINT 'Migration AddCodes complete.';
