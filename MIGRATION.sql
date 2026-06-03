-- Run this SQL script on your database to apply the new columns
-- Database: Online_Shop

-- Add TelegramChatId, PasswordResetToken, PasswordResetExpiry to Customers
ALTER TABLE Customers ADD TelegramChatId NVARCHAR(50) NULL;
ALTER TABLE Customers ADD PasswordResetToken NVARCHAR(200) NULL;
ALTER TABLE Customers ADD PasswordResetExpiry DATETIME2 NULL;

-- Add LocationLat, LocationLng to Orders
ALTER TABLE Orders ADD LocationLat FLOAT NULL;
ALTER TABLE Orders ADD LocationLng FLOAT NULL;

-- Verify
SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Customers', 'Orders')
  AND COLUMN_NAME IN ('TelegramChatId','PasswordResetToken','PasswordResetExpiry','LocationLat','LocationLng');
