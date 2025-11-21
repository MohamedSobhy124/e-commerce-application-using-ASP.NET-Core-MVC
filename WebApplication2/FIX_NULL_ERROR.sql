-- ========================================
-- FIX: Make FlashSaleItemId NULLABLE
-- ========================================
-- The column was created as NOT NULL by mistake
-- We need it to be NULL because not all orders are from flash sales

-- Check current state
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'orderDetails'
AND COLUMN_NAME = 'FlashSaleItemId';

-- Make the column nullable
ALTER TABLE [dbo].[orderDetails]
ALTER COLUMN [FlashSaleItemId] INT NULL;

PRINT '✅ FlashSaleItemId is now NULLABLE!';
GO

-- Verify the change
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'orderDetails'
AND COLUMN_NAME = 'FlashSaleItemId';

PRINT '✅ Fixed! Should show IS_NULLABLE = YES';

