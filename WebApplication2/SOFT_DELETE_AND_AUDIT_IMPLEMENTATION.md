# Soft Delete and Audit Fields Implementation

## Summary
Implemented soft delete and audit fields (CreatedDate, ModifiedDate, CreatedBy, ModifiedBy) for all product-related entities.

## Changes Made

### 1. Created BaseEntity Class
- **File**: `BulkyBook.Models/BaseEntity.cs`
- **Properties**:
  - `IsDeleted` (bool, default: false)
  - `CreatedDate` (DateTime, default: DateTime.Now)
  - `ModifiedDate` (DateTime?, nullable)
  - `CreatedBy` (string?, nullable, max 450 chars for User ID)
  - `ModifiedBy` (string?, nullable, max 450 chars for User ID)

### 2. Updated Models to Inherit from BaseEntity
All following models now inherit from `BaseEntity`:
- ✅ `Product`
- ✅ `ProductOption`
- ✅ `ProductOptionValue`
- ✅ `ProductVariant` (removed duplicate `IsDeleted` property)
- ✅ `Categry`
- ✅ `FlashSale` (removed duplicate `CreatedDate` property)
- ✅ `FlashSaleItem`

### 3. Created AuditHelper Utility
- **File**: `BulkyBook.Utility/AuditHelper.cs`
- **Methods**:
  - `SetCreatedAudit(BaseEntity entity, ClaimsPrincipal? user)` - Sets CreatedDate, CreatedBy, IsDeleted=false
  - `SetModifiedAudit(BaseEntity entity, ClaimsPrincipal? user)` - Sets ModifiedDate, ModifiedBy
  - `SetDeletedAudit(BaseEntity entity, ClaimsPrincipal? user)` - Sets IsDeleted=true, ModifiedDate, ModifiedBy

### 4. Updated Repository Base Class
- **File**: `BulkyBook.DataAccess/Repository/Repository.cs`
- **Changes**:
  - `add()`: Automatically sets `CreatedDate` and `IsDeleted=false` for BaseEntity types
  - `Get()`: Automatically filters out deleted items (`!IsDeleted`) for BaseEntity types
  - `GetAll()`: Automatically filters out deleted items for BaseEntity types
  - `remove()`: Performs soft delete (sets `IsDeleted=true`, `ModifiedDate`) for BaseEntity types, hard delete for others
  - `removeRage()`: Performs soft delete for BaseEntity types, hard delete for others

### 5. Updated Repository Update Methods
All repository `Update()` methods now set `ModifiedDate`:
- ✅ `ProductReprository.update()`
- ✅ `ProductOptionRepository.Update()`
- ✅ `ProductOptionValueRepository.Update()`
- ✅ `ProductVariantRepository.Update()`
- ✅ `CategryReprository.update()`
- ✅ `FlashSaleRepository.Update()`
- ✅ `FlashSaleItemRepository.Update()`

### 6. Updated FlashSaleRepository Special Methods
- `GetActiveFlashSales()`: Now filters out deleted FlashSales and FlashSaleItems
- `GetFlashSaleWithItems()`: Now filters out deleted FlashSales

### 7. Updated Controllers to Set Audit Fields

#### ProductController
- ✅ `UpSert()`: Sets audit fields on create/update
- ✅ `Delete()`: Sets deleted audit fields
- ✅ `SaveProductStep()`: Sets modified audit fields
- ✅ `AddOption()`: Sets created audit fields
- ✅ `AddOptionValue()`: Sets created audit fields
- ✅ `DeleteOption()`: Sets deleted audit fields
- ✅ `DeleteOptionValue()`: Sets deleted audit fields
- ✅ `GenerateVariants()`: Sets created/deleted audit fields for variants
- ✅ `UpdateVariant()`: Sets modified audit fields

#### CategriesController
- ✅ `Create()`: Sets created audit fields
- ✅ `Edit()`: Sets modified audit fields
- ✅ `DeleteConfirmed()`: Sets deleted audit fields

#### FlashSaleController
- ✅ `Create()`: Sets created audit fields
- ✅ `Edit()`: Sets modified audit fields
- ✅ `Delete()`: Sets deleted audit fields for FlashSale and FlashSaleItems
- ✅ `AddProductToSale()`: Sets created audit fields for FlashSaleItem
- ✅ `RemoveProduct()`: Sets deleted audit fields for FlashSaleItem

## Database Migration Required

You need to create a migration to add the audit columns to all tables:

```bash
dotnet ef migrations add AddSoftDeleteAndAuditFields --project BulkyBook.DataAccess --startup-project WebApplication2
dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
```

### SQL Script (if manual migration needed):

```sql
-- Add columns to Products table
ALTER TABLE Products ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE Products ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE Products ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE Products ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE Products ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to ProductOptions table
ALTER TABLE ProductOptions ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE ProductOptions ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE ProductOptions ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE ProductOptions ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE ProductOptions ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to ProductOptionValues table
ALTER TABLE ProductOptionValues ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE ProductOptionValues ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE ProductOptionValues ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE ProductOptionValues ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE ProductOptionValues ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to ProductVariants table (IsDeleted already exists, just add audit fields)
ALTER TABLE ProductVariants ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE ProductVariants ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE ProductVariants ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE ProductVariants ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to Categries table
ALTER TABLE Categries ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE Categries ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE Categries ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE Categries ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE Categries ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to FlashSales table (CreatedDate already exists, just add other fields)
ALTER TABLE FlashSales ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE FlashSales ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE FlashSales ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE FlashSales ADD ModifiedBy NVARCHAR(450) NULL;

-- Add columns to FlashSaleItems table
ALTER TABLE FlashSaleItems ADD IsDeleted BIT NOT NULL DEFAULT 0;
ALTER TABLE FlashSaleItems ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
ALTER TABLE FlashSaleItems ADD ModifiedDate DATETIME2 NULL;
ALTER TABLE FlashSaleItems ADD CreatedBy NVARCHAR(450) NULL;
ALTER TABLE FlashSaleItems ADD ModifiedBy NVARCHAR(450) NULL;
```

## Important Notes

1. **Automatic Filtering**: All `Get()` and `GetAll()` queries automatically filter out deleted items for BaseEntity types
2. **Soft Delete**: All `remove()` operations perform soft delete (set `IsDeleted=true`) instead of hard delete
3. **Audit Trail**: All create/update/delete operations now track who and when
4. **Backward Compatibility**: Existing data will have `IsDeleted=false` and `CreatedDate=GETDATE()` by default
5. **User Tracking**: `CreatedBy` and `ModifiedBy` are set from `User.FindFirst(ClaimTypes.NameIdentifier)?.Value` (User ID)

## Testing Checklist

- [ ] Create a product - verify CreatedDate and CreatedBy are set
- [ ] Update a product - verify ModifiedDate and ModifiedBy are set
- [ ] Delete a product - verify IsDeleted=true, ModifiedDate and ModifiedBy are set
- [ ] Query products - verify deleted products don't appear
- [ ] Test same for ProductOptions, ProductOptionValues, ProductVariants, Categries, FlashSales, FlashSaleItems

