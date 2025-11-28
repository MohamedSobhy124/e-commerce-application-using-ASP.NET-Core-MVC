# Expiry Date Feature - Database Migration Required

## Summary
Added expiry date fields to Product and ProductVariant models. A database migration is required to add these columns.

## Changes Made

### 1. Product Model (`BulkyBook.Models/Product.cs`)
- Added `ExpiryDate` property (nullable DateTime)

### 2. ProductVariant Model (`BulkyBook.Models/ProductVariant.cs`)
- Added `ExpiryDate` property (nullable DateTime) - optional

### 3. Admin Views
- Added expiry date input field in product Upsert form
- Added expiry date column in variants table
- Updated JavaScript to handle variant expiry date updates

### 4. Customer Product Details View
- Displays product expiry date if it exists (for simple products)
- Displays variant expiry date if variant is selected and has expiry date
- Falls back to product expiry date if variant doesn't have expiry date
- Only shows expiry date if one exists (variant or product)

## Migration Steps

1. **Create Migration:**
   ```powershell
   dotnet ef migrations add AddExpiryDateToProductsAndVariants --project BulkyBook.DataAccess --startup-project WebApplication2
   ```

2. **Review Migration:**
   Check the generated migration file in `BulkyBook.DataAccess/Migrations/` to ensure:
   - `Products` table gets `ExpiryDate` column (nullable DateTime)
   - `ProductVariants` table gets `ExpiryDate` column (nullable DateTime)

3. **Apply Migration:**
   ```powershell
   dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
   ```

## Migration SQL (if needed manually)

```sql
ALTER TABLE Products
ADD ExpiryDate DATETIME NULL;

ALTER TABLE ProductVariants
ADD ExpiryDate DATETIME NULL;
```

## Features

### Admin Panel
- **Product Upsert Form**: Expiry date input field (optional)
- **Variants Table**: Expiry date column for each variant (optional)
- Both fields are optional - can be left empty

### Customer View
- **Display Logic:**
  1. If variant is selected AND variant has expiry date → Show variant expiry date
  2. Else if product has expiry date → Show product expiry date
  3. Else → Don't show expiry date section

- Expiry date appears below the price section with a calendar icon

## Translations Added

- **English (`SharedResources.en.resx`):**
  - `ExpiryDate`: "Expiry Date"
  - `ProductExpiryDateInfo`: "Enter the expiry date for this product (optional)"

- **Arabic (`SharedResources.ar.resx`):**
  - `ExpiryDate`: "تاريخ انتهاء الصلاحية"
  - `ProductExpiryDateInfo`: "أدخل تاريخ انتهاء الصلاحية لهذا المنتج (اختياري)"

## Notes

- All expiry date fields are optional (nullable)
- No validation required - dates can be in the past or future
- Expiry dates are stored as DateTime (date only, time ignored)
- The feature gracefully handles missing expiry dates

