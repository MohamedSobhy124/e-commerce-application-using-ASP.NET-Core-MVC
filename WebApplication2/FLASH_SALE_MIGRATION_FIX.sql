-- ========================================
-- FLASH SALE CART - MANUAL SQL MIGRATION
-- ========================================
-- Run this in SQL Server Management Studio if migration fails

-- Step 1: Add columns to ShoppingCarts table (nullable, no constraints yet)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCarts]') AND name = 'FlashSaleItemId')
BEGIN
    ALTER TABLE [dbo].[ShoppingCarts]
    ADD [FlashSaleItemId] INT NULL;
    PRINT 'FlashSaleItemId column added successfully';
END
ELSE
BEGIN
    PRINT 'FlashSaleItemId column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ShoppingCarts]') AND name = 'FlashSalePrice')
BEGIN
    ALTER TABLE [dbo].[ShoppingCarts]
    ADD [FlashSalePrice] DECIMAL(18, 2) NULL;
    PRINT 'FlashSalePrice column added successfully';
END
ELSE
BEGIN
    PRINT 'FlashSalePrice column already exists';
END
GO

-- Step 2: Add foreign key constraint (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId')
BEGIN
    ALTER TABLE [dbo].[ShoppingCarts]
    ADD CONSTRAINT [FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId]
    FOREIGN KEY ([FlashSaleItemId])
    REFERENCES [dbo].[FlashSaleItems] ([Id])
    ON DELETE NO ACTION;
    PRINT 'Foreign key constraint added successfully';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists';
END
GO

-- Step 3: Add index for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ShoppingCarts_FlashSaleItemId' AND object_id = OBJECT_ID(N'[dbo].[ShoppingCarts]'))
BEGIN
    CREATE INDEX [IX_ShoppingCarts_FlashSaleItemId]
    ON [dbo].[ShoppingCarts] ([FlashSaleItemId]);
    PRINT 'Index created successfully';
END
ELSE
BEGIN
    PRINT 'Index already exists';
END
GO

PRINT 'Migration completed successfully!';



