# 🔧 Fix Foreign Key Constraint Error

## Error Message
```
The ALTER TABLE statement conflicted with the FOREIGN KEY constraint 
"FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId". 
The conflict occurred in database "db32552", table "dbo.FlashSaleItems", column 'Id'.
```

## 🎯 Root Cause
The migration is trying to add a foreign key to `FlashSaleItems` table, but:
- The `FlashSaleItems` table might not exist yet, OR
- The migration order is incorrect

---

## ✅ Solution 1: Check Migration Order (RECOMMENDED)

### Step 1: Check if FlashSaleItems table exists

Open **SQL Server Management Studio** or **SQL Server Object Explorer** in Visual Studio and check if these tables exist:
- `FlashSales`
- `FlashSaleItems`

If they **DON'T EXIST**, you need to run the admin migration first!

### Step 2: Run Migrations in Correct Order

```powershell
# Open Package Manager Console
cd ../BulkyBook.DataAccess

# FIRST: Run the Flash Sale System migration
Add-Migration AddFlashSaleSystem
Update-Database

# SECOND: Run the Cart migration
Add-Migration AddFlashSaleToCart
Update-Database
```

---

## ✅ Solution 2: Remove Failed Migration and Retry

If the migration partially failed:

```powershell
# Open Package Manager Console
cd ../BulkyBook.DataAccess

# Remove the failed migration
Remove-Migration

# Check database state
Update-Database

# Try again
Add-Migration AddFlashSaleToCart
Update-Database
```

---

## ✅ Solution 3: Manual SQL Script (If automated migration fails)

### Step 1: Open SQL Server Management Studio

Connect to your database: `db32552`

### Step 2: Run the Manual Script

Execute the SQL script in `FLASH_SALE_MIGRATION_FIX.sql`:

```sql
-- Add columns without constraints first
ALTER TABLE [dbo].[ShoppingCarts]
ADD [FlashSaleItemId] INT NULL;

ALTER TABLE [dbo].[ShoppingCarts]
ADD [FlashSalePrice] DECIMAL(18, 2) NULL;

-- Add foreign key constraint
ALTER TABLE [dbo].[ShoppingCarts]
ADD CONSTRAINT [FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId]
FOREIGN KEY ([FlashSaleItemId])
REFERENCES [dbo].[FlashSaleItems] ([Id]);

-- Add index for performance
CREATE INDEX [IX_ShoppingCarts_FlashSaleItemId]
ON [dbo].[ShoppingCarts] ([FlashSaleItemId]);
```

### Step 3: Mark Migration as Applied

```powershell
# In Package Manager Console
Add-Migration AddFlashSaleToCart
Update-Database -Script

# This generates the SQL but doesn't execute it
# Since you already ran it manually, just mark it as done:
# (The tables are already updated, so EF will see that and move on)
```

---

## ✅ Solution 4: Skip Foreign Key Temporarily (Quick Workaround)

If you want to test without foreign key constraints:

### Step 1: Create Migration Without Foreign Key

Create a custom migration file:

```csharp
// In Migrations folder: YYYYMMDDHHMMSS_AddFlashSaleToCart.cs

public partial class AddFlashSaleToCart : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add columns only (no foreign key)
        migrationBuilder.AddColumn<int>(
            name: "FlashSaleItemId",
            table: "ShoppingCarts",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "FlashSalePrice",
            table: "ShoppingCarts",
            type: "decimal(18,2)",
            nullable: true);

        // Add index
        migrationBuilder.CreateIndex(
            name: "IX_ShoppingCarts_FlashSaleItemId",
            table: "ShoppingCarts",
            column: "FlashSaleItemId");

        // Foreign key will be added later manually or in production
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ShoppingCarts_FlashSaleItemId",
            table: "ShoppingCarts");

        migrationBuilder.DropColumn(
            name: "FlashSaleItemId",
            table: "ShoppingCarts");

        migrationBuilder.DropColumn(
            name: "FlashSalePrice",
            table: "ShoppingCarts");
    }
}
```

---

## 🔍 Diagnostic Steps

### Check Current Database State

```sql
-- Check if FlashSales table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'FlashSales';

-- Check if FlashSaleItems table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'FlashSaleItems';

-- Check if columns exist in ShoppingCarts
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ShoppingCarts'
AND COLUMN_NAME IN ('FlashSaleItemId', 'FlashSalePrice');

-- Check foreign keys on ShoppingCarts
SELECT 
    fk.name AS FK_Name,
    tp.name AS Parent_Table,
    cp.name AS Parent_Column,
    tr.name AS Referenced_Table,
    cr.name AS Referenced_Column
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name = 'ShoppingCarts';
```

---

## 📋 Migration Checklist

Follow this order:

- [ ] **Step 1:** Verify `FlashSales` table exists
- [ ] **Step 2:** Verify `FlashSaleItems` table exists
- [ ] **Step 3:** If NOT, run `AddFlashSaleSystem` migration first
- [ ] **Step 4:** Run `AddFlashSaleToCart` migration
- [ ] **Step 5:** Verify columns added to `ShoppingCarts`
- [ ] **Step 6:** Verify foreign key constraint exists
- [ ] **Step 7:** Test the application

---

## 🚨 Common Issues & Solutions

### Issue 1: "FlashSaleItems table doesn't exist"
**Solution:** Run the admin flash sale migration first:
```powershell
Add-Migration AddFlashSaleSystem
Update-Database
```

### Issue 2: "Migration already exists"
**Solution:** Remove it and try again:
```powershell
Remove-Migration
Add-Migration AddFlashSaleToCart
Update-Database
```

### Issue 3: "Columns already exist"
**Solution:** Just add the foreign key manually:
```sql
ALTER TABLE [dbo].[ShoppingCarts]
ADD CONSTRAINT [FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId]
FOREIGN KEY ([FlashSaleItemId])
REFERENCES [dbo].[FlashSaleItems] ([Id]);
```

### Issue 4: "Cannot drop table because it's referenced"
**Solution:** This is good! It means the foreign key already exists. You're done!

---

## ✅ Verify Success

After fixing, verify:

```sql
-- Should return 2 rows
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ShoppingCarts'
AND COLUMN_NAME IN ('FlashSaleItemId', 'FlashSalePrice');

-- Should return 1 row
SELECT name FROM sys.foreign_keys
WHERE name = 'FK_ShoppingCarts_FlashSaleItems_FlashSaleItemId';
```

If both queries return results, **YOU'RE GOOD TO GO!** ✅

---

## 🎯 Quick Fix (Most Common)

**Most likely you just need to run the admin migration first:**

```powershell
cd ../BulkyBook.DataAccess

# First migration (creates FlashSales & FlashSaleItems tables)
Add-Migration AddFlashSaleSystem
Update-Database

# Second migration (adds columns to ShoppingCarts)
Add-Migration AddFlashSaleToCart
Update-Database
```

That should fix it! 🚀

---

## 📞 Still Having Issues?

1. Check the diagnostic queries above
2. Look at `FLASH_SALE_MIGRATION_FIX.sql` for manual SQL
3. Verify both migrations exist in your Migrations folder
4. Check Package Manager Console for detailed error messages

The foreign key constraint is trying to reference `FlashSaleItems.Id`, so that table **MUST** exist first!



