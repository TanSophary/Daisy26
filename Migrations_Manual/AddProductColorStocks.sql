-- Migration: Add ProductColorStocks table
-- Run this script once against your SQL Server database

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_NAME = 'ProductColorStocks'
)
BEGIN
    CREATE TABLE ProductColorStocks (
        Id          INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductId   INT NOT NULL,
        Color       NVARCHAR(50) NOT NULL,
        Stock       INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ProductColorStocks_Products
            FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
            ON DELETE CASCADE
    );

    PRINT 'ProductColorStocks table created.';
END
ELSE
BEGIN
    PRINT 'ProductColorStocks table already exists — skipped.';
END
