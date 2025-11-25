# Product Variant System - Complete Guide

## Overview
This document describes the complete Product Variant System implementation for the e-commerce application. The system allows products to have multiple options (Size, Color, Flavor, etc.) with different prices and stock levels for each combination.

## System Architecture

### Database Models

1. **ProductOption** - Represents an option type (e.g., "Size", "Color", "Flavor")
   - `Id` - Primary key
   - `ProductId` - Foreign key to Product
   - `Name` - Option name (e.g., "Size")
   - `DisplayOrder` - Order for display

2. **ProductOptionValue** - Represents a value for an option (e.g., "S", "M", "L" for Size)
   - `Id` - Primary key
   - `ProductOptionId` - Foreign key to ProductOption
   - `Value` - The actual value (e.g., "S")
   - `DisplayOrder` - Order for display

3. **ProductVariant** - Represents a unique combination of option values
   - `Id` - Primary key
   - `ProductId` - Foreign key to Product
   - `Price` - Variant-specific price
   - `ListPrice`, `Price50`, `Price100` - Bulk pricing
   - `StockQuantity` - Stock for this variant
   - `SKU` - Variant-specific SKU
   - `ImageUrl` - Variant-specific image (optional)

4. **ProductVariantOptionValue** - Junction table linking variants to option values
   - `Id` - Primary key
   - `ProductVariantId` - Foreign key to ProductVariant
   - `ProductOptionValueId` - Foreign key to ProductOptionValue

### Product Model Updates

- Added `ProductType` enum: `Simple` (0) or `Variable` (1)
- Added navigation properties: `ProductOptions`, `ProductVariants`
- For Simple products: Use existing `Price` and `StockQuantity` fields
- For Variable products: Use variant prices and stock

### ShoppingCart Model Updates

- Added `ProductVariantId` (nullable) - Links cart item to specific variant
- For Simple products: `ProductVariantId` is null
- For Variable products: `ProductVariantId` must be set

## Admin Workflow

### Step 1: Create Product
1. Navigate to Admin → Products → Create
2. Fill in basic product information
3. **Important**: Select "Variable Product" from "Product Type" dropdown if product has options

### Step 2: Add Options
1. After saving the product, the "Product Options & Variants" section appears
2. Click "Add Option" button
3. Enter option name (e.g., "Size", "Color", "Flavor")
4. Click "Add" to save

### Step 3: Add Option Values
1. For each option, enter values in the input field
2. Click "Add" next to each value
3. Example for "Size": Add "S", "M", "L"
4. Example for "Color": Add "Red", "Black", "Blue"

### Step 4: Generate Variants
1. Once all options and values are added, click "Generate Variants"
2. System automatically creates all possible combinations
3. Example: Size (S, M, L) + Color (Red, Black) = 6 variants

### Step 5: Configure Variants
1. A table appears with all generated variants
2. For each variant, set:
   - **Price** - Variant-specific price
   - **List Price** - Original price (for showing discounts)
   - **Price 50+** - Bulk pricing for 50+ units
   - **Price 100+** - Bulk pricing for 100+ units
   - **Stock** - Available quantity
   - **SKU** - Unique identifier (optional)
   - **Image** - Variant-specific image (optional)

## Customer Experience

### Product Page Behavior

#### Simple Products
- No options displayed
- Shows product price and stock
- Add to cart works normally

#### Variable Products
1. **Option Selection**
   - Each option appears as a group of buttons
   - Customer must select one value for each option
   - Selected values are highlighted

2. **Dynamic Price Update**
   - As customer selects options, price updates automatically
   - Shows selected variant's price
   - Shows stock availability

3. **Add to Cart**
   - Button is disabled until all options are selected
   - Cart stores the specific variant ID
   - Stock validation prevents adding out-of-stock variants

## API Endpoints

### Admin Endpoints

- `GET /Admin/Product/GetProductOptions?productId={id}` - Get all options for a product
- `POST /Admin/Product/AddOption` - Add a new option
- `DELETE /Admin/Product/DeleteOption?optionId={id}` - Delete an option
- `POST /Admin/Product/AddOptionValue` - Add a value to an option
- `DELETE /Admin/Product/DeleteOptionValue?valueId={id}` - Delete an option value
- `POST /Admin/Product/GenerateVariants?productId={id}` - Generate all variant combinations
- `POST /Admin/Product/UpdateVariant` - Update variant details (price, stock, SKU)
- `POST /Admin/Product/UploadVariantImage?variantId={id}` - Upload variant image

## Database Migration

To apply the variant system to your database, run:

```bash
# In Package Manager Console or terminal
dotnet ef migrations add AddProductVariantSystem --project BulkyBook.DataAccess --startup-project WebApplication2
dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
```

## Example Use Cases

### Example 1: T-Shirt with Size and Color
- **Product**: Nike T-Shirt
- **Options**: 
  - Size: S, M, L, XL
  - Color: Red, Black, Blue
- **Variants Generated**: 12 combinations (4 sizes × 3 colors)
- Each variant can have different price and stock

### Example 2: Protein Powder with Flavor and Size
- **Product**: Gold Whey Protein
- **Options**:
  - Flavor: Vanilla, Chocolate
  - Size: 1KG, 2KG
- **Variants Generated**: 4 combinations
- Different prices: 1KG = 125 AED, 2KG = 210 AED

### Example 3: Simple Product (No Options)
- **Product**: Water Bottle
- **Product Type**: Simple
- Uses product's base price and stock
- No variants needed

## Important Notes

1. **Backward Compatibility**: Existing Simple products continue to work without changes
2. **Stock Management**: Each variant has its own stock level
3. **Price Flexibility**: Each variant can have completely different pricing
4. **Image Support**: Variants can have their own images (e.g., different colors)
5. **Cart Logic**: Cart items are linked to specific variants, not just products

## Troubleshooting

### Variants not generating?
- Ensure all options have at least one value
- Check that product is saved before adding options
- Verify product type is set to "Variable"

### Price not updating on customer page?
- Check browser console for JavaScript errors
- Verify all options are selected
- Ensure variants were generated correctly

### Cart not working with variants?
- Verify `ProductVariantId` is being passed in form
- Check that variant exists and has stock
- Ensure cart repository handles variant ID

## Future Enhancements

Potential improvements:
- Bulk variant editing
- Variant import/export
- Variant-specific discounts
- Variant image gallery
- Variant comparison feature

