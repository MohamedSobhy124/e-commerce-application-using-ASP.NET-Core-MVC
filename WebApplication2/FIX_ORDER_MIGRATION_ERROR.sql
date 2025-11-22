-- ========================================
-- FIX ORDER DETAIL FLASH SALE MIGRATION
-- ========================================
-- Run this in SQL Server Management Studio

-- Step 1: Add column without foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[orderDetails]') 
               AND name = 'FlashSaleItemId')
BEGIN
    ALTER TABLE [dbo].[orderDetails]
    ADD [FlashSaleItemId] INT NULL;
    PRINT '✅ FlashSaleItemId column added to orderDetails';
END
ELSE
BEGIN
    PRINT 'ℹ️ FlashSaleItemId column already exists';
END
GO

-- Step 2: Add foreign key constraint (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_orderDetails_FlashSaleItems_FlashSaleItemId')
BEGIN
    ALTER TABLE [dbo].[orderDetails]
    ADD CONSTRAINT [FK_orderDetails_FlashSaleItems_FlashSaleItemId]
    FOREIGN KEY ([FlashSaleItemId])
    REFERENCES [dbo].[FlashSaleItems] ([Id])
    ON DELETE NO ACTION;
    PRINT '✅ Foreign key constraint added';
END
ELSE
BEGIN
    PRINT 'ℹ️ Foreign key constraint already exists';
END
GO

-- Step 3: Add index for performance
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_orderDetails_FlashSaleItemId' 
               AND object_id = OBJECT_ID(N'[dbo].[orderDetails]'))
BEGIN
    CREATE INDEX [IX_orderDetails_FlashSaleItemId]
    ON [dbo].[orderDetails] ([FlashSaleItemId]);
    PRINT '✅ Index created';
END
ELSE
BEGIN
    PRINT 'ℹ️ Index already exists';
END
GO

-- Verify
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'orderDetails'
AND COLUMN_NAME = 'FlashSaleItemId';

PRINT '========================================';
PRINT '✅ Migration completed successfully!';
PRINT '========================================';




