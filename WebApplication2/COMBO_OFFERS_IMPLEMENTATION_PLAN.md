# 🎁 COMBO OFFERS IMPLEMENTATION PLAN

## 📋 Overview

Combo Offers allow customers to purchase multiple products together at a discounted price. Similar to Flash Sales, but focused on bundled products with special pricing.

---

## 🏗️ Architecture Overview

### Similar to Flash Sale System:
- **ComboOffer** (Main combo entity) - Similar to `FlashSale`
- **ComboOfferItem** (Products in combo) - Similar to `FlashSaleItem`
- Repository Pattern with UnitOfWork
- Admin management interface
- Customer-facing display
- Cart integration
- Order processing with stock deduction

---

## 📊 Database Structure

### 1. ComboOffer Table
```sql
ComboOffer
├── Id (int, PK)
├── Name (nvarchar(100)) - "Protein Combo Pack"
├── NameAr (nvarchar(100)) - "عرض البروتين"
├── Description (nvarchar(500))
├── DescriptionAr (nvarchar(500))
├── ImageUrl (nvarchar(500))
├── ComboPrice (decimal(18,2)) - Total price for the combo
├── OriginalTotalPrice (decimal(18,2)) - Sum of individual product prices
├── DiscountPercentage (decimal(5,2)) - Calculated discount %
├── StartDate (datetime)
├── EndDate (datetime)
├── IsActive (bit)
├── MinimumQuantity (int) - Min items required (default: all items)
├── MaximumQuantity (int) - Max combos per customer (optional)
├── CreatedDate (datetime)
├── ModifiedDate (datetime)
├── IsDeleted (bit)
└── ComboOfferItems (Collection) ──┐
                                  │
                                  ▼
```

### 2. ComboOfferItem Table
```sql
ComboOfferItem
├── Id (int, PK)
├── ComboOfferId (int, FK) ──────► Links to ComboOffer
├── ProductId (int, FK) ─────────► Links to Product
├── ProductVariantId (int, FK, nullable) ──► Links to ProductVariant (optional)
├── Quantity (int) - How many of this product in combo (default: 1)
├── DisplayOrder (int) - Order in which products appear
├── IsRequired (bit) - Must be included (default: true)
├── CreatedDate (datetime)
├── ModifiedDate (datetime)
├── IsDeleted (bit)
└── Navigation Properties
```

---

## 📁 Files to Create/Modify

### **MODELS** (2 new files)

#### 1. `BulkyBook.Models/ComboOffer.cs`
```csharp
- Inherits from BaseEntity
- Properties: Name, NameAr, Description, DescriptionAr, ImageUrl
- ComboPrice, OriginalTotalPrice, DiscountPercentage
- StartDate, EndDate, IsActive
- MinimumQuantity, MaximumQuantity
- Navigation: ComboOfferItems
- Calculated: IsCurrentlyActive, Savings, TimeRemaining
```

#### 2. `BulkyBook.Models/ComboOfferItem.cs`
```csharp
- Inherits from BaseEntity
- Properties: ComboOfferId, ProductId, ProductVariantId (nullable)
- Quantity, DisplayOrder, IsRequired
- Navigation: ComboOffer, Product, ProductVariant
- Calculated: Savings (if individual price vs combo price)
```

---

### **REPOSITORY INTERFACES** (2 new files)

#### 3. `BulkyBook.DataAccess/Repository/IRepository/IComboOfferRepository.cs`
```csharp
- Interface extending IRepository<ComboOffer>
- Methods:
  - GetActiveComboOffers()
  - GetComboOfferWithItems(int id)
  - Update(ComboOffer comboOffer)
```

#### 4. `BulkyBook.DataAccess/Repository/IRepository/IComboOfferItemRepository.cs`
```csharp
- Interface extending IRepository<ComboOfferItem>
- Methods:
  - GetItemsByComboOfferId(int comboOfferId)
  - GetItemsWithProducts(int comboOfferId)
```

---

### **REPOSITORY IMPLEMENTATIONS** (2 new files)

#### 5. `BulkyBook.DataAccess/Repository/ComboOfferRepository.cs`
```csharp
- Implements IComboOfferRepository
- GetActiveComboOffers() - Returns active combos with items
- GetComboOfferWithItems() - Includes all related data
- Update() - Handles audit fields
```

#### 6. `BulkyBook.DataAccess/Repository/ComboOfferItemRepository.cs`
```csharp
- Implements IComboOfferItemRepository
- GetItemsByComboOfferId() - Gets all items for a combo
- GetItemsWithProducts() - Includes product details
```

---

### **UNIT OF WORK** (2 files to modify)

#### 7. `BulkyBook.DataAccess/Repository/IRepository/IUnitOfWork.cs`
```csharp
// ADD:
IComboOfferRepository ComboOffer { get; }
IComboOfferItemRepository ComboOfferItem { get; }
```

#### 8. `BulkyBook.DataAccess/Repository/UnitOfWork.cs`
```csharp
// ADD properties:
public IComboOfferRepository ComboOffer { get; private set; }
public IComboOfferItemRepository ComboOfferItem { get; private set; }

// ADD in constructor:
ComboOffer = new ComboOfferRepository(_db);
ComboOfferItem = new ComboOfferItemRepository(_db);
```

---

### **DATABASE CONTEXT** (1 file to modify)

#### 9. `BulkyBook.DataAccess/Data/ApplicationDBContext.cs`
```csharp
// ADD DbSets:
public DbSet<ComboOffer> ComboOffers { get; set; }
public DbSet<ComboOfferItem> ComboOfferItems { get; set; }

// ADD indexes in OnModelCreating:
modelBuilder.Entity<ComboOffer>().HasIndex(co => new { co.IsDeleted, co.IsActive, co.StartDate, co.EndDate });
modelBuilder.Entity<ComboOfferItem>().HasIndex(ci => new { ci.IsDeleted, ci.ComboOfferId, ci.ProductId });
```

---

### **MIGRATION** (1 new file - auto-generated)

#### 10. `BulkyBook.DataAccess/Migrations/YYYYMMDDHHMMSS_AddComboOffers.cs`
```bash
# Run command:
dotnet ef migrations add AddComboOffers --project ../BulkyBook.DataAccess
dotnet ef database update --project ../BulkyBook.DataAccess
```

---

### **ADMIN CONTROLLER** (1 new file)

#### 11. `WebApplication2/Areas/Admin/Controllers/ComboOfferController.cs`
```csharp
Actions:
- Index() - List all combo offers
- Create() - GET/POST - Create new combo
- Edit(int id) - GET/POST - Edit combo
- Details(int id) - View combo details
- Delete(int id) - GET/POST - Delete combo
- AddProducts(int id) - GET/POST - Add products to combo
- RemoveProduct(int comboId, int itemId) - Remove product from combo
```

---

### **ADMIN VIEWS** (6 new files)

#### 12. `WebApplication2/Areas/Admin/Views/ComboOffer/Index.cshtml`
- List all combo offers with status, dates, discount
- Filter by active/inactive
- Search functionality

#### 13. `WebApplication2/Areas/Admin/Views/ComboOffer/Create.cshtml`
- Form to create new combo offer
- Basic info: Name, Description, Image, Dates
- Price calculation section

#### 14. `WebApplication2/Areas/Admin/Views/ComboOffer/Edit.cshtml`
- Edit existing combo offer
- Similar to Create but with existing data

#### 15. `WebApplication2/Areas/Admin/Views/ComboOffer/Details.cshtml`
- View combo details
- List all products in combo
- Show pricing breakdown
- Stock status

#### 16. `WebApplication2/Areas/Admin/Views/ComboOffer/AddProducts.cshtml`
- Add/remove products from combo
- Product search and selection
- Quantity per product
- Display order
- Required/Optional toggle

#### 17. `WebApplication2/Areas/Admin/Views/ComboOffer/Delete.cshtml`
- Confirmation page for deletion

---

### **CUSTOMER CONTROLLER** (1 new file)

#### 18. `WebApplication2/Areas/Customer/Controllers/ComboOfferController.cs`
```csharp
Actions:
- Index() - List active combo offers
- Details(int id) - View combo details with products
- AddToCart(int id) - Add entire combo to cart
```

---

### **CUSTOMER VIEWS** (3 new files)

#### 19. `WebApplication2/Areas/Customer/Views/ComboOffer/Index.cshtml`
- Display active combo offers
- Grid/list view
- Show discount percentage
- Time remaining badges

#### 20. `WebApplication2/Areas/Customer/Views/ComboOffer/Details.cshtml`
- Combo details page
- Product list with images
- Price breakdown (original vs combo)
- Savings calculation
- "Add to Cart" button
- Stock availability

#### 21. `WebApplication2/Areas/Customer/Views/ComboOffer/_ComboOfferCard.cshtml`
- Reusable card component for combo display
- Used in Index and home page sections

---

### **HOME PAGE INTEGRATION** (2 files to modify)

#### 22. `WebApplication2/Areas/Customer/Controllers/HomeController.cs`
```csharp
// ADD method:
public async Task<IActionResult> LoadComboOffersSection()
{
    // Get active combo offers
    // Return partial view
}
```

#### 23. `WebApplication2/Areas/Customer/Views/Home/_ComboOffersSection.cshtml`
- Display combo offers on home page
- Similar to Flash Sales section
- Carousel or grid layout

---

### **CART INTEGRATION** (2 files to modify)

#### 24. `BulkyBook.Models/ShoppingCart.cs`
```csharp
// ADD properties:
public int? ComboOfferId { get; set; }
[ForeignKey(nameof(ComboOfferId))]
[ValidateNever]
public ComboOffer? ComboOffer { get; set; }

[NotMapped]
public bool IsComboOffer => ComboOfferId.HasValue;
```

#### 25. `WebApplication2/Areas/Customer/Controllers/CartController.cs`
```csharp
// MODIFY Summary POST:
- Handle combo offer items
- Calculate combo price
- Add all combo products to cart
- Set ComboOfferId on cart items
```

---

### **ORDER PROCESSING** (2 files to modify)

#### 26. `BulkyBook.Models/OrderDetail.cs`
```csharp
// ADD property:
public int? ComboOfferId { get; set; }
[ForeignKey(nameof(ComboOfferId))]
[ValidateNever]
public ComboOffer? ComboOffer { get; set; }

[NotMapped]
public bool IsFromComboOffer => ComboOfferId.HasValue;
```

#### 27. `BulkyBook.Services/StockService.cs` (or CartController)
```csharp
// MODIFY ProcessOrderStockDeduction():
- Handle combo offer items
- Deduct stock for all products in combo
- Track combo offer usage
```

---

### **CSS STYLING** (1 new file)

#### 28. `WebApplication2/wwwroot/css/combo-offers.css`
- Styles for combo offer cards
- Price display
- Discount badges
- Product grid in combo details

---

### **LOCALIZATION** (3 files to modify)

#### 29. `WebApplication2/SharedResources.en.resx`
```xml
<!-- ADD keys: -->
<data name="ComboOffers" xml:space="preserve">
  <value>Combo Offers</value>
</data>
<data name="ComboOffer" xml:space="preserve">
  <value>Combo Offer</value>
</data>
<data name="AddComboToCart" xml:space="preserve">
  <value>Add Combo to Cart</value>
</data>
<data name="YouSave" xml:space="preserve">
  <value>You Save</value>
</data>
<data name="OriginalPrice" xml:space="preserve">
  <value>Original Price</value>
</data>
<data name="ComboPrice" xml:space="preserve">
  <value>Combo Price</value>
</data>
<!-- ... more keys ... -->
```

#### 30. `WebApplication2/SharedResources.ar.resx`
- Arabic translations for all combo offer keys

#### 31. `WebApplication2/SharedResources.resx`
- Default/fallback translations

---

## 🔄 Implementation Steps

### **Phase 1: Database & Models** (Steps 1-10)
1. Create `ComboOffer.cs` model
2. Create `ComboOfferItem.cs` model
3. Create repository interfaces
4. Create repository implementations
5. Update UnitOfWork interface
6. Update UnitOfWork implementation
7. Update ApplicationDBContext
8. Create and run migration
9. Test database structure

### **Phase 2: Admin Interface** (Steps 11-17)
10. Create ComboOfferController
11. Create admin views (Index, Create, Edit, Details, AddProducts, Delete)
12. Test CRUD operations
13. Test product management

### **Phase 3: Customer Interface** (Steps 18-23)
14. Create Customer ComboOfferController
15. Create customer views (Index, Details, _ComboOfferCard)
16. Create home page section
17. Test customer-facing features

### **Phase 4: Cart & Order Integration** (Steps 24-27)
18. Update ShoppingCart model
19. Update CartController
20. Update OrderDetail model
21. Update stock deduction logic
22. Test cart and checkout flow

### **Phase 5: Styling & Localization** (Steps 28-31)
23. Create CSS file
24. Add localization keys
25. Test bilingual support
26. Final testing and polish

---

## 🎯 Key Features

### **1. Flexible Product Bundling**
- Add multiple products to one combo
- Set quantity per product
- Optional vs required products
- Support for product variants

### **2. Smart Pricing**
- Automatic calculation of original total
- Set combo price
- Calculate discount percentage
- Display savings to customers

### **3. Time-Based Offers**
- Start and end dates
- Active/inactive status
- Time remaining display
- Automatic activation/deactivation

### **4. Stock Management**
- Check stock for all products
- Prevent adding out-of-stock combos
- Deduct stock when combo purchased
- Track combo availability

### **5. Cart Integration**
- Add entire combo to cart
- All products added at once
- Combo price applied
- Individual products still visible

### **6. Order Processing**
- Track combo offers in orders
- Stock deduction for all products
- Order history shows combo info
- Analytics support

---

## 📝 Example Use Cases

### **Example 1: Protein Combo**
```
Combo: "Complete Protein Stack"
Products:
  - Whey Protein (2kg) - Qty: 1
  - Creatine (500g) - Qty: 1
  - Pre-Workout (300g) - Qty: 1

Original Total: AED 450.00
Combo Price: AED 380.00
Savings: AED 70.00 (15.5% off)
```

### **Example 2: Vitamin Bundle**
```
Combo: "Daily Wellness Pack"
Products:
  - Multivitamin (60 tabs) - Qty: 2
  - Vitamin D3 (60 caps) - Qty: 1
  - Omega-3 (60 caps) - Qty: 1

Original Total: AED 280.00
Combo Price: AED 220.00
Savings: AED 60.00 (21.4% off)
```

---

## ⚠️ Important Considerations

### **1. Stock Validation**
- All products must be in stock
- Check stock before allowing combo purchase
- Handle partial stock scenarios

### **2. Price Calculation**
- Calculate original total from current product prices
- Allow manual override of combo price
- Recalculate discount percentage automatically

### **3. Cart Logic**
- When combo added, add all products individually
- Mark cart items with ComboOfferId
- Apply combo price to total
- Prevent duplicate additions

### **4. Order Processing**
- Track which items came from combo
- Deduct stock for all products
- Maintain combo reference in order
- Support refunds/returns

### **5. Display Order**
- Allow admin to set product display order
- Show products in logical sequence
- Highlight required vs optional items

---

## 🚀 Quick Start Commands

```bash
# 1. Create models
# (Manual creation of ComboOffer.cs and ComboOfferItem.cs)

# 2. Create repositories
# (Manual creation of repository files)

# 3. Update UnitOfWork
# (Edit existing files)

# 4. Create migration
cd WebApplication2
dotnet ef migrations add AddComboOffers --project ../BulkyBook.DataAccess

# 5. Apply migration
dotnet ef database update --project ../BulkyBook.DataAccess

# 6. Build and test
dotnet build
dotnet run
```

---

## 📊 Database Schema Visualization

```
┌─────────────────┐
│  ComboOffer     │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ NameAr          │
│ Description     │
│ ComboPrice      │
│ OriginalPrice   │
│ Discount%       │
│ StartDate       │
│ EndDate         │
│ IsActive        │
└────────┬────────┘
         │ 1
         │
         │ *
         ▼
┌─────────────────┐
│ ComboOfferItem  │
├─────────────────┤
│ Id (PK)         │
│ ComboOfferId(FK)│──┐
│ ProductId (FK)  │──┼──► Product
│ VariantId (FK)  │──┘
│ Quantity        │
│ DisplayOrder    │
│ IsRequired      │
└─────────────────┘
```

---

## ✅ Testing Checklist

- [ ] Create combo offer in admin
- [ ] Add products to combo
- [ ] Edit combo offer
- [ ] Delete combo offer
- [ ] View combo on customer side
- [ ] Add combo to cart
- [ ] Checkout with combo
- [ ] Stock deduction works
- [ ] Order shows combo info
- [ ] Bilingual support works
- [ ] Time-based activation works
- [ ] Stock validation works
- [ ] Price calculation correct
- [ ] Cart totals correct

---

## 📚 Similar Implementations to Reference

1. **FlashSale System** - Similar structure and flow
2. **ServiceSubscription** - Time-based offers
3. **PromoCode** - Discount calculations
4. **ShoppingCart** - Cart integration pattern

---

This implementation plan provides a complete roadmap for adding Combo Offers to your e-commerce application. Follow the phases sequentially for best results!

