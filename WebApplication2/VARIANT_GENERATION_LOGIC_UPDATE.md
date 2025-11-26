# Variant Generation Logic Update

## Summary
Updated the `GenerateVariants` method to intelligently handle variant generation based on whether ProductOptions count has changed.

## Changes Made

### 1. Added `IsDeleted` Property to ProductVariant Model
- **File**: `BulkyBook.Models/ProductVariant.cs`
- Added `IsDeleted` boolean property (default: false)
- Used to mark variants as deleted when ProductOptions structure changes

### 2. Updated GenerateVariants Logic
- **File**: `WebApplication2/Areas/Admin/Controllers/ProductController.cs`

#### Logic Flow:
1. **Get Current ProductOptions Count**: Count the number of ProductOptions currently defined for the product
2. **Get Existing Options Count**: Count unique ProductOptionIds from existing variants' ProductVariantOptionValues
3. **Compare Counts**:
   - **If counts differ** (e.g., added new option like "Color" when only "Size" existed):
     - Mark ALL existing variants as `IsDeleted = true`
     - Clear existing variants list
     - Generate all new combinations from scratch
   - **If counts are same** (e.g., only added new option values like "Blue" to existing "Color" option):
     - Keep existing variants
     - Only generate new combinations that don't already exist
     - Map new option values to existing variants

### 3. Updated UpSert Method
- Filters out deleted variants when loading ProductVariants
- Only processes non-deleted variants when loading variant option values

## Example Scenarios

### Scenario 1: Adding New Option (Options Count Changes)
**Before:**
- ProductOptions: Size (XL, XXL)
- Variants: Size:XL, Size:XXL

**After adding Color option:**
- ProductOptions: Size (XL, XXL), Color (Red, Green)
- Old variants marked as `IsDeleted = true`
- New variants generated: Size:XL/Color:Red, Size:XL/Color:Green, Size:XXL/Color:Red, Size:XXL/Color:Green

### Scenario 2: Adding New Option Values (Options Count Same)
**Before:**
- ProductOptions: Size (XL, XXL), Color (Red)
- Variants: Size:XL/Color:Red, Size:XXL/Color:Red

**After adding "Green" to Color:**
- ProductOptions: Size (XL, XXL), Color (Red, Green)
- Old variants kept (not deleted)
- New variants added: Size:XL/Color:Green, Size:XXL/Color:Green

## Database Migration Required

You need to create a migration to add the `IsDeleted` column to the `ProductVariants` table:

```bash
dotnet ef migrations add AddIsDeletedToProductVariant --project BulkyBook.DataAccess --startup-project WebApplication2
dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
```

Or manually add the column:
```sql
ALTER TABLE ProductVariants ADD IsDeleted BIT NOT NULL DEFAULT 0;
```

## Important Notes

1. **Soft Delete**: Variants are marked as deleted, not physically removed from database
2. **Data Preservation**: Old variant data (price, stock, etc.) is preserved even when marked as deleted
3. **Filtering**: All queries for ProductVariants should filter out deleted ones using `!v.IsDeleted`
4. **Customer-Facing**: Deleted variants should not appear in customer-facing views

## Next Steps

1. Create and run database migration
2. Test the variant generation with both scenarios:
   - Adding new option
   - Adding new option values
3. Update any other places that query ProductVariants to filter out deleted ones

