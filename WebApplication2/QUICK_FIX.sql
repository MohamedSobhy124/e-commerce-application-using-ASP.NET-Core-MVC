-- ============================================
-- QUICK FIX FOR GUEST CHECKOUT ERROR
-- Run this in SQL Server Management Studio
-- or Azure Data Studio
-- ============================================

USE [YOUR_DATABASE_NAME];  -- CHANGE THIS to your actual database name
GO

PRINT 'Starting Guest Checkout Database Fix...';
GO

-- Step 1: Check if columns already exist
PRINT 'Step 1: Checking existing columns...';
GO

-- Step 2: Add IsGuestOrder column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'OrderHeaders' 
    AND COLUMN_NAME = 'IsGuestOrder'
)
BEGIN
    PRINT 'Adding IsGuestOrder column...';
    ALTER TABLE OrderHeaders 
    ADD IsGuestOrder BIT NOT NULL DEFAULT 0;
    PRINT '✓ IsGuestOrder column added successfully';
END
ELSE
BEGIN
    PRINT '✓ IsGuestOrder column already exists';
    
    -- Make sure it has default value for existing rows
    UPDATE OrderHeaders 
    SET IsGuestOrder = 0 
    WHERE IsGuestOrder IS NULL;
    PRINT '✓ Updated NULL values to 0';
END
GO

-- Step 3: Add Email column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'OrderHeaders' 
    AND COLUMN_NAME = 'Email'
)
BEGIN
    PRINT 'Adding Email column...';
    ALTER TABLE OrderHeaders 
    ADD Email NVARCHAR(MAX) NULL;
    PRINT '✓ Email column added successfully';
END
ELSE
BEGIN
    PRINT '✓ Email column already exists';
END
GO

-- Step 4: Make ApplicationUserId nullable
PRINT 'Checking ApplicationUserId nullability...';

DECLARE @IsNullable NVARCHAR(3);
SELECT @IsNullable = IS_NULLABLE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'OrderHeaders' 
AND COLUMN_NAME = 'ApplicationUserId';

IF @IsNullable = 'NO'
BEGIN
    PRINT 'Making ApplicationUserId nullable...';
    ALTER TABLE OrderHeaders 
    ALTER COLUMN ApplicationUserId NVARCHAR(450) NULL;
    PRINT '✓ ApplicationUserId is now nullable';
END
ELSE
BEGIN
    PRINT '✓ ApplicationUserId is already nullable';
END
GO

-- Step 5: Verify the changes
PRINT '';
PRINT '======================================';
PRINT 'VERIFICATION - Table Structure:';
PRINT '======================================';

SELECT 
    COLUMN_NAME as [Column],
    DATA_TYPE as [Type],
    CASE WHEN IS_NULLABLE = 'YES' THEN 'NULL' ELSE 'NOT NULL' END as [Nullable],
    ISNULL(COLUMN_DEFAULT, 'No Default') as [Default]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'OrderHeaders'
AND COLUMN_NAME IN ('ApplicationUserId', 'Email', 'IsGuestOrder')
ORDER BY ORDINAL_POSITION;
GO

-- Step 6: Check data integrity
PRINT '';
PRINT '======================================';
PRINT 'DATA INTEGRITY CHECK:';
PRINT '======================================';

DECLARE @TotalOrders INT, @NullIsGuestOrder INT, @NullEmail INT, @NullAppUserId INT;

SELECT 
    @TotalOrders = COUNT(*),
    @NullIsGuestOrder = SUM(CASE WHEN IsGuestOrder IS NULL THEN 1 ELSE 0 END),
    @NullEmail = SUM(CASE WHEN Email IS NULL THEN 1 ELSE 0 END),
    @NullAppUserId = SUM(CASE WHEN ApplicationUserId IS NULL THEN 1 ELSE 0 END)
FROM OrderHeaders;

PRINT 'Total Orders: ' + CAST(@TotalOrders AS VARCHAR(10));
PRINT 'Orders with NULL IsGuestOrder: ' + CAST(@NullIsGuestOrder AS VARCHAR(10));
PRINT 'Orders with NULL Email: ' + CAST(@NullEmail AS VARCHAR(10));
PRINT 'Orders with NULL ApplicationUserId: ' + CAST(@NullAppUserId AS VARCHAR(10));

IF @NullIsGuestOrder > 0
BEGIN
    PRINT '';
    PRINT '⚠ WARNING: Found NULL values in IsGuestOrder! This should not happen.';
    PRINT 'Running cleanup...';
    UPDATE OrderHeaders SET IsGuestOrder = 0 WHERE IsGuestOrder IS NULL;
    PRINT '✓ Cleaned up NULL values';
END

PRINT '';
PRINT '======================================';
PRINT 'SAMPLE DATA (Last 5 Orders):';
PRINT '======================================';

SELECT TOP 5 
    Id as OrderId,
    Name,
    ISNULL(Email, 'N/A') as Email,
    IsGuestOrder,
    CASE WHEN ApplicationUserId IS NULL THEN 'NULL' ELSE 'Has Value' END as HasAppUserId,
    OrderTotal,
    OrderDate
FROM OrderHeaders 
ORDER BY Id DESC;
GO

PRINT '';
PRINT '======================================';
PRINT '✓✓✓ DATABASE UPDATE COMPLETE! ✓✓✓';
PRINT '======================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Restart your application';
PRINT '2. Try guest checkout again';
PRINT '3. Order confirmation should now work!';
PRINT '';
GO

