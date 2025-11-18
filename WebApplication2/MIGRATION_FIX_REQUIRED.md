# 🔧 Database Migration Fix Required

## Error Explanation

**Error:** `SqlNullValueException: Data is Null`

This error occurs because:
1. The database migration hasn't been run yet, OR
2. Existing orders in the database have NULL values for new columns (`Email`, `IsGuestOrder`)

---

## Solution: Create and Run Migration with Default Values

### Step 1: Create the Migration

Run this command in Package Manager Console:

```powershell
Add-Migration GuestCheckoutSupport -Project BulkyBook.DataAccess
```

### Step 2: Modify the Migration File

After creating the migration, you need to modify it to handle existing data.

Find the migration file in `BulkyBook.DataAccess/Migrations/` folder (it will have a timestamp and name like `20231117_GuestCheckoutSupport.cs`)

**Modify the `Up` method:**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Make ApplicationUserId nullable
    migrationBuilder.AlterColumn<string>(
        name: "ApplicationUserId",
        table: "OrderHeaders",
        type: "nvarchar(450)",
        nullable: true,
        oldClrType: typeof(string),
        oldType: "nvarchar(450)");

    // Add Email column (nullable)
    migrationBuilder.AddColumn<string>(
        name: "Email",
        table: "OrderHeaders",
        type: "nvarchar(max)",
        nullable: true);

    // Add IsGuestOrder column with default value FALSE
    migrationBuilder.AddColumn<bool>(
        name: "IsGuestOrder",
        table: "OrderHeaders",
        type: "bit",
        nullable: false,
        defaultValue: false);

    // IMPORTANT: Update existing rows to have default values
    migrationBuilder.Sql(
        "UPDATE OrderHeaders SET IsGuestOrder = 0 WHERE IsGuestOrder IS NULL");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "Email",
        table: "OrderHeaders");

    migrationBuilder.DropColumn(
        name: "IsGuestOrder",
        table: "OrderHeaders");

    migrationBuilder.AlterColumn<string>(
        name: "ApplicationUserId",
        table: "OrderHeaders",
        type: "nvarchar(450)",
        nullable: false,
        defaultValue: "",
        oldClrType: typeof(string),
        oldType: "nvarchar(450)",
        oldNullable: true);
}
```

### Step 3: Apply the Migration

```powershell
Update-Database -Project BulkyBook.DataAccess
```

---

## Alternative: Quick Fix Using SQL

If the migration is already created but you're getting the error, run this SQL directly in SQL Server Management Studio or Azure Data Studio:

```sql
-- Check if columns exist
SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'OrderHeaders'
AND COLUMN_NAME IN ('Email', 'IsGuestOrder', 'ApplicationUserId');

-- If IsGuestOrder doesn't exist, add it with default value
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'OrderHeaders' AND COLUMN_NAME = 'IsGuestOrder')
BEGIN
    ALTER TABLE OrderHeaders 
    ADD IsGuestOrder BIT NOT NULL DEFAULT 0;
END

-- If Email doesn't exist, add it
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'OrderHeaders' AND COLUMN_NAME = 'Email')
BEGIN
    ALTER TABLE OrderHeaders 
    ADD Email NVARCHAR(MAX) NULL;
END

-- Make ApplicationUserId nullable if it isn't already
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'OrderHeaders' 
           AND COLUMN_NAME = 'ApplicationUserId' 
           AND IS_NULLABLE = 'NO')
BEGIN
    ALTER TABLE OrderHeaders 
    ALTER COLUMN ApplicationUserId NVARCHAR(450) NULL;
END

-- Update existing rows to have default values
UPDATE OrderHeaders 
SET IsGuestOrder = 0 
WHERE IsGuestOrder IS NULL;

-- Verify the changes
SELECT TOP 5 Id, ApplicationUserId, Email, IsGuestOrder, OrderTotal 
FROM OrderHeaders 
ORDER BY Id DESC;
```

---

## Verification Steps

After running the migration or SQL, verify the changes:

### 1. Check Table Structure

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'OrderHeaders'
ORDER BY ORDINAL_POSITION;
```

Expected results:
- `ApplicationUserId` → NULLABLE = YES
- `Email` → NULLABLE = YES  
- `IsGuestOrder` → NULLABLE = NO, DEFAULT = 0

### 2. Check Existing Data

```sql
SELECT Id, ApplicationUserId, Email, IsGuestOrder, Name, PhoneNumber
FROM OrderHeaders;
```

All rows should have:
- `IsGuestOrder` = 0 (for existing orders)
- `Email` = NULL (for existing orders)
- `ApplicationUserId` should have values for old orders

---

## If You Still Get Errors

### Check Your Connection String

Make sure your `appsettings.json` has the correct connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### Clear and Rebuild

```bash
# Clean the solution
dotnet clean

# Delete bin and obj folders
Remove-Item -Recurse -Force */bin, */obj

# Rebuild
dotnet build

# Run again
dotnet run --project WebApplication2
```

---

## Testing After Fix

1. ✅ Restart your application
2. ✅ Try guest checkout flow
3. ✅ Complete an order
4. ✅ Verify order confirmation page loads
5. ✅ Check database to see new guest order

---

## Prevention

To prevent this in future migrations:

1. **Always specify default values** for non-nullable columns
2. **Add SQL to update existing rows** in the Up method
3. **Test migrations on a copy of production data**
4. **Use nullable types** (`string?`, `bool?`) when appropriate

---

## Quick Diagnostic

Run this to check your current database state:

```sql
-- Check if migration was applied
SELECT TOP 1 MigrationId 
FROM __EFMigrationsHistory 
WHERE MigrationId LIKE '%GuestCheckout%'
ORDER BY MigrationId DESC;

-- If NULL, migration hasn't been run
-- If you see a value, check the table structure

-- Check OrderHeaders structure
EXEC sp_help 'OrderHeaders';

-- Check for NULL values
SELECT 
    COUNT(*) as TotalOrders,
    SUM(CASE WHEN IsGuestOrder IS NULL THEN 1 ELSE 0 END) as NullIsGuestOrder,
    SUM(CASE WHEN Email IS NULL THEN 1 ELSE 0 END) as NullEmail,
    SUM(CASE WHEN ApplicationUserId IS NULL THEN 1 ELSE 0 END) as NullAppUserId
FROM OrderHeaders;
```

---

## Contact Support

If you still have issues after following these steps:

1. Share the output of the diagnostic SQL above
2. Check if you have permission to alter the database
3. Verify the database connection is working
4. Check the __EFMigrationsHistory table

**Most Common Issue:** Migration not run yet. Solution: Run `Update-Database`

