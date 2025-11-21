# 🏗️ Flash Sale System Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    FLASH SALE SYSTEM                            │
│                     (Phase 1: Admin)                            │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│   Admin User     │─────▶│  Flash Sale      │─────▶│   Database       │
│   Interface      │      │  Controller      │      │   (SQL Server)   │
└──────────────────┘      └──────────────────┘      └──────────────────┘
        │                          │                          │
        │                          │                          │
        ▼                          ▼                          ▼
┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│   Razor Views    │      │   Repositories   │      │   EF Core        │
│   (CRUD Pages)   │      │   (Unit of Work) │      │   DbContext      │
└──────────────────┘      └──────────────────┘      └──────────────────┘
```

---

## Data Flow

### Creating a Flash Sale

```
User Action               Controller                Repository              Database
    │                         │                         │                      │
    │ 1. Click "Create"       │                         │                      │
    ├────────────────────────▶│                         │                      │
    │                         │                         │                      │
    │ 2. Fill Form & Submit   │                         │                      │
    ├────────────────────────▶│                         │                      │
    │                         │ 3. Validate Data        │                      │
    │                         ├────────────────────────▶│                      │
    │                         │                         │ 4. Insert Record     │
    │                         │                         ├─────────────────────▶│
    │                         │                         │                      │
    │                         │                         │ 5. Confirm Insert    │
    │                         │                         ◀──────────────────────┤
    │                         │ 6. Save Changes         │                      │
    │                         ◀─────────────────────────┤                      │
    │                         │                         │                      │
    │ 7. Redirect to Add Prods│                         │                      │
    ◀─────────────────────────┤                         │                      │
    │                         │                         │                      │
```

### Adding Products to Flash Sale

```
User Action               Controller                Repository              Database
    │                         │                         │                      │
    │ 1. Select Product       │                         │                      │
    ├────────────────────────▶│                         │                      │
    │                         │ 2. Get Product Info     │                      │
    │                         │ (AJAX)                  │                      │
    │                         ├────────────────────────▶│                      │
    │                         │                         │ 3. Query Product     │
    │                         │                         ├─────────────────────▶│
    │                         │                         │                      │
    │                         │                         │ 4. Return Data       │
    │                         │                         ◀──────────────────────┤
    │                         ◀─────────────────────────┤                      │
    │ 5. Display Stock/Price  │                         │                      │
    ◀─────────────────────────┤                         │                      │
    │                         │                         │                      │
    │ 6. Enter Qty & Price    │                         │                      │
    │ 7. Click Add            │                         │                      │
    ├────────────────────────▶│                         │                      │
    │                         │ 8. Validate             │                      │
    │                         │ - Qty ≤ Stock           │                      │
    │                         │ - Price > 0             │                      │
    │                         │ - Not duplicate         │                      │
    │                         │                         │                      │
    │                         │ 9. Add FlashSaleItem    │                      │
    │                         ├────────────────────────▶│                      │
    │                         │                         │ 10. Insert Item      │
    │                         │                         ├─────────────────────▶│
    │                         │                         │                      │
    │                         │                         │ 11. Confirm          │
    │                         │                         ◀──────────────────────┤
    │                         ◀─────────────────────────┤                      │
    │ 12. Success Message     │                         │                      │
    ◀─────────────────────────┤                         │                      │
```

---

## Database Relationships

```sql
┌─────────────────────────┐
│    FlashSales           │
├─────────────────────────┤
│ Id (PK)                 │◀───┐
│ Name                    │    │
│ Description             │    │
│ StartDate               │    │
│ EndDate                 │    │ 1:N Relationship
│ IsActive                │    │
│ CreatedDate             │    │
└─────────────────────────┘    │
                               │
                               │
┌─────────────────────────┐    │
│   FlashSaleItems        │    │
├─────────────────────────┤    │
│ Id (PK)                 │    │
│ FlashSaleId (FK)        │────┘
│ ProductId (FK)          │────┐
│ FlashSaleQuantity       │    │
│ FlashSalePrice          │    │
│ AddedDate               │    │
└─────────────────────────┘    │
                               │
                               │ N:1 Relationship
                               │
┌─────────────────────────┐    │
│      Products           │    │
├─────────────────────────┤    │
│ Id (PK)                 │◀───┘
│ Title                   │
│ Price                   │
│ StockQuantity           │
│ MinimumStockAlert       │
│ ... (other fields)      │
└─────────────────────────┘
```

---

## Component Structure

### Backend (C# / ASP.NET Core MVC)

```
┌─────────────────────────────────────────┐
│           Models Layer                  │
├─────────────────────────────────────────┤
│ • FlashSale.cs                          │
│   - Properties                          │
│   - Calculated Properties               │
│   - Navigation Properties               │
│                                         │
│ • FlashSaleItem.cs                      │
│   - Properties                          │
│   - Calculated Properties               │
│   - Navigation Properties               │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│      Data Access Layer                  │
├─────────────────────────────────────────┤
│ • ApplicationDBContext                  │
│   - DbSet<FlashSale>                    │
│   - DbSet<FlashSaleItem>                │
│                                         │
│ • Repository Pattern                    │
│   - IFlashSaleRepository                │
│   - FlashSaleRepository                 │
│   - IFlashSaleItemRepository            │
│   - FlashSaleItemRepository             │
│                                         │
│ • Unit of Work Pattern                  │
│   - IUnitOfWork                         │
│   - UnitOfWork                          │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│        Controller Layer                 │
├─────────────────────────────────────────┤
│ • FlashSaleController.cs                │
│   - Index() - List all                  │
│   - Create() - Create form & POST       │
│   - Edit() - Edit form & POST           │
│   - Details() - View details            │
│   - AddProducts() - Manage products     │
│   - AddProductToSale() - AJAX POST      │
│   - RemoveProduct() - AJAX POST         │
│   - Delete() - AJAX POST                │
│   - ToggleActive() - AJAX POST          │
│   - GetProductInfo() - AJAX GET         │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│          View Layer                     │
├─────────────────────────────────────────┤
│ • Index.cshtml - List view              │
│ • Create.cshtml - Create form           │
│ • Edit.cshtml - Edit form               │
│ • Details.cshtml - Details view         │
│ • AddProducts.cshtml - Product mgmt     │
└─────────────────────────────────────────┘
```

### Frontend (HTML/CSS/JavaScript)

```
┌─────────────────────────────────────────┐
│          UI Components                  │
├─────────────────────────────────────────┤
│                                         │
│ Flash Sale Cards                        │
│ ├─ Status Badge                         │
│ ├─ Date Display                         │
│ ├─ Statistics (Products, Qty, Value)    │
│ └─ Action Buttons                       │
│                                         │
│ Forms                                   │
│ ├─ Input Fields (validated)             │
│ ├─ Date Pickers                         │
│ ├─ Dropdowns                            │
│ └─ Checkboxes/Toggles                   │
│                                         │
│ Product Management                      │
│ ├─ Product Selection Dropdown           │
│ ├─ Quantity Input (validated)           │
│ ├─ Price Input (validated)              │
│ ├─ Product Info Box (AJAX)              │
│ └─ Product Cards (added products)       │
│                                         │
│ Tables                                  │
│ ├─ Product List                         │
│ ├─ Pricing Comparison                   │
│ └─ Statistics Totals                    │
│                                         │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│       JavaScript Interactions           │
├─────────────────────────────────────────┤
│                                         │
│ AJAX Functions:                         │
│ • getProductInfo()                      │
│ • addProduct()                          │
│ • removeProduct()                       │
│ • deleteFlashSale()                     │
│ • toggleActive()                        │
│                                         │
│ Utilities:                              │
│ • updateDuration()                      │
│ • calculateDiscount()                   │
│ • validateQuantity()                    │
│ • validatePrice()                       │
│                                         │
│ Notifications:                          │
│ • SweetAlert2 (confirmations)           │
│ • Toastr (success/error messages)       │
│                                         │
└─────────────────────────────────────────┘
```

---

## Status Calculation Logic

```
┌─────────────────────────────────────────┐
│      Is Flash Sale ACTIVE?              │
└─────────────────────────────────────────┘
                  │
                  ▼
         ┌─────────────────┐
         │ IsActive = TRUE?│
         └────────┬─────────┘
                  │
          ┌───────┴────────┐
          │                │
         YES               NO ─────▶ INACTIVE
          │
          ▼
┌──────────────────────────┐
│ CurrentTime >= StartDate?│
└──────────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   YES            NO ─────▶ SCHEDULED
    │
    ▼
┌──────────────────────────┐
│ CurrentTime <= EndDate?  │
└──────────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   YES            NO ─────▶ ENDED
    │
    ▼
┌──────────────────────────┐
│ Has Products with Qty>0? │
└──────────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   YES            NO ─────▶ SOLD OUT
    │
    ▼
  ACTIVE
```

---

## Security Architecture

```
┌─────────────────────────────────────────┐
│        Authentication Layer             │
├─────────────────────────────────────────┤
│ • ASP.NET Core Identity                 │
│ • Role-based Authorization              │
│ • [Authorize(Roles = SD.Role_Admin)]    │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│         Validation Layer                │
├─────────────────────────────────────────┤
│ Server-Side:                            │
│ • Model validation attributes           │
│ • Business logic validation             │
│ • Database constraints                  │
│                                         │
│ Client-Side:                            │
│ • JavaScript validation                 │
│ • HTML5 input validation                │
│ • Real-time feedback                    │
└─────────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│        Protection Layer                 │
├─────────────────────────────────────────┤
│ • Anti-Forgery Tokens (CSRF)            │
│ • SQL Injection Prevention (EF Core)    │
│ • XSS Prevention (Razor encoding)       │
│ • Input sanitization                    │
└─────────────────────────────────────────┘
```

---

## Future Extensions (Phase 2 & 3)

```
┌─────────────────────────────────────────────────────────┐
│                  Customer Interface                     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Homepage Flash Sale Widget                             │
│  ├─ Active Flash Sales Carousel                         │
│  ├─ Countdown Timer                                     │
│  ├─ Featured Products                                   │
│  └─ "View All" Button                                   │
│                                                         │
│  Flash Sales Page                                       │
│  ├─ Filter by Category                                  │
│  ├─ Sort by Discount/Time                               │
│  ├─ Product Grid with Flash Sale Badges                 │
│  └─ Real-time Stock Updates                             │
│                                                         │
│  Cart Integration                                       │
│  ├─ Track Flash Sale Items                              │
│  ├─ Deduct Flash Sale Quantity                          │
│  ├─ Deduct Product Stock                                │
│  ├─ Handle Expiration During Checkout                   │
│  └─ Apply Flash Sale Price                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Performance Considerations

### Database Optimization
- **Indexes**: 
  - FlashSales.IsActive, StartDate, EndDate
  - FlashSaleItems.FlashSaleId, ProductId
  - Products.StockQuantity

### Caching Strategy (Future)
- Cache active flash sales for 1 minute
- Invalidate cache on product add/remove
- Use distributed cache for multi-server

### Query Optimization
- Eager loading with `.Include()` for related data
- Pagination for large lists
- Select only needed columns

---

## API Response Formats

### Success Response
```json
{
  "success": true,
  "message": "Product added successfully",
  "data": {
    "productId": 123,
    "flashSaleId": 45
  }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Quantity cannot exceed stock quantity (50)",
  "errors": [
    {
      "field": "quantity",
      "error": "Invalid quantity"
    }
  ]
}
```

---

## File Structure

```
WebApplication2/
│
├── Areas/
│   └── Admin/
│       ├── Controllers/
│       │   └── FlashSaleController.cs
│       └── Views/
│           └── FlashSale/
│               ├── Index.cshtml
│               ├── Create.cshtml
│               ├── Edit.cshtml
│               ├── Details.cshtml
│               └── AddProducts.cshtml
│
├── BulkyBook.Models/
│   ├── FlashSale.cs
│   └── FlashSaleItem.cs
│
├── BulkyBook.DataAccess/
│   ├── Data/
│   │   └── ApplicationDBContext.cs
│   └── Repository/
│       ├── IRepository/
│       │   ├── IFlashSaleRepository.cs
│       │   ├── IFlashSaleItemRepository.cs
│       │   └── IUnitOfWork.cs
│       ├── FlashSaleRepository.cs
│       ├── FlashSaleItemRepository.cs
│       └── UnitOfWork.cs
│
└── Views/
    └── Shared/
        └── _Layout.cshtml (updated with nav link)
```

---

**Architecture Version:** 1.0  
**Last Updated:** November 21, 2024  
**Status:** Phase 1 Complete



