-- Run this SQL to add the Color column to OrderItems table
-- Required after updating the OrderItem model with the Color field

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'OrderItems' AND COLUMN_NAME = 'Color'
)
BEGIN
    ALTER TABLE OrderItems ADD Color NVARCHAR(100) NULL;
    PRINT 'Color column added to OrderItems table.';
END
ELSE
BEGIN
    PRINT 'Color column already exists in OrderItems table.';
END
