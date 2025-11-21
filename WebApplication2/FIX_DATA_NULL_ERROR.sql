-- ========================================
-- FIX: NULL Value Reading Error
-- ========================================
-- This script checks for and fixes NULL values in flash sale tables

PRINT '========================================';
PRINT 'Checking for NULL values...';
PRINT '========================================';

-- Check FlashSales table for NULL values in required columns
PRINT '';
PRINT '1. Checking FlashSales table:';
SELECT 
    Id,
    Name,
    StartDate,
    EndDate,
    IsActive
FROM FlashSales
WHERE Name IS NULL 
   OR StartDate IS NULL 
   OR EndDate IS NULL;

IF @@ROWCOUNT > 0
BEGIN
    PRINT '❌ Found NULL values in FlashSales table!';
    PRINT 'Deleting invalid records...';
    DELETE FROM FlashSales 
    WHERE Name IS NULL 
       OR StartDate IS NULL 
       OR EndDate IS NULL;
    PRINT '✅ Invalid records deleted';
END
ELSE
BEGIN
    PRINT '✅ FlashSales table is clean';
END

-- Check FlashSaleItems table for NULL values in required columns
PRINT '';
PRINT '2. Checking FlashSaleItems table:';
SELECT 
    Id,
    FlashSaleId,
    ProductId,
    FlashSaleQuantity,
    FlashSalePrice
FROM FlashSaleItems
WHERE FlashSaleId IS NULL 
   OR ProductId IS NULL 
   OR FlashSaleQuantity IS NULL 
   OR FlashSalePrice IS NULL;

IF @@ROWCOUNT > 0
BEGIN
    PRINT '❌ Found NULL values in FlashSaleItems table!';
    PRINT 'Deleting invalid records...';
    DELETE FROM FlashSaleItems 
    WHERE FlashSaleId IS NULL 
       OR ProductId IS NULL 
       OR FlashSaleQuantity IS NULL 
       OR FlashSalePrice IS NULL;
    PRINT '✅ Invalid records deleted';
END
ELSE
BEGIN
    PRINT '✅ FlashSaleItems table is clean';
END

-- Check ShoppingCarts table
PRINT '';
PRINT '3. Checking ShoppingCarts table:';
SELECT 
    Id,
    ProductId,
    Count,
    FlashSaleItemId,
    FlashSalePrice
FROM ShoppingCarts
WHERE ProductId IS NULL 
   OR Count IS NULL;

IF @@ROWCOUNT > 0
BEGIN
    PRINT '❌ Found NULL values in ShoppingCarts table!';
    PRINT 'Deleting invalid records...';
    DELETE FROM ShoppingCarts 
    WHERE ProductId IS NULL 
       OR Count IS NULL;
    PRINT '✅ Invalid records deleted';
END
ELSE
BEGIN
    PRINT '✅ ShoppingCarts table is clean';
END

-- Check orderDetails table
PRINT '';
PRINT '4. Checking orderDetails table:';
SELECT 
    Id,
    OrderHeaderId,
    ProductId,
    Count,
    Price,
    FlashSaleItemId
FROM orderDetails
WHERE OrderHeaderId IS NULL 
   OR ProductId IS NULL 
   OR Count IS NULL 
   OR Price IS NULL;

IF @@ROWCOUNT > 0
BEGIN
    PRINT '❌ Found NULL values in orderDetails table!';
    PRINT 'Deleting invalid records...';
    DELETE FROM orderDetails 
    WHERE OrderHeaderId IS NULL 
       OR ProductId IS NULL 
       OR Count IS NULL 
       OR Price IS NULL;
    PRINT '✅ Invalid records deleted';
END
ELSE
BEGIN
    PRINT '✅ orderDetails table is clean';
END

PRINT '';
PRINT '========================================';
PRINT '✅ All tables checked and cleaned!';
PRINT '========================================';

-- Summary
PRINT '';
PRINT 'Summary:';
SELECT 'FlashSales' as TableName, COUNT(*) as RecordCount FROM FlashSales
UNION ALL
SELECT 'FlashSaleItems', COUNT(*) FROM FlashSaleItems
UNION ALL
SELECT 'ShoppingCarts', COUNT(*) FROM ShoppingCarts
UNION ALL
SELECT 'orderDetails', COUNT(*) FROM orderDetails;



