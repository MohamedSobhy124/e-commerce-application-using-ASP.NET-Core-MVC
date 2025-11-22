# Wishlist Implementation Guide

This guide explains the complete wishlist implementation that needs to be done.

## Files Created/Modified

### 1. Model File (Need to create in BulkyBook.Models project)
**File:** `BulkyBook.Models/Wishlist.cs`

```csharp
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product product { get; set; }
        
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser applicationUser { get; set; }
    }
}
```

### 2. Repository Interface (Need to create)
**File:** `BulkyBook.DataAccess.Repository.IRepository/IWishlistRepository.cs`

```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IWishlistRepository : IRepository<Wishlist>
    {
        void Update(Wishlist obj);
    }
}
```

### 3. Repository Implementation (Need to create)
**File:** `BulkyBook.DataAccess.Repository/WishlistRepository.cs`

```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
    {
        private ApplicationDBContext _db;
        public WishlistRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Wishlist obj)
        {
            _db.Wishlists.Update(obj);
        }
    }
}
```

### 4. Update IUnitOfWork (Need to add)
**File:** `BulkyBook.DataAccess.Repository.IRepository/IUnitOfWork.cs`

Add this property:
```csharp
IWishlistRepository wishlist { get; }
```

### 5. Update UnitOfWork (Need to add)
**File:** `BulkyBook.DataAccess.Repository/UnitOfWork.cs`

Add this property:
```csharp
public IWishlistRepository wishlist { get; private set; }
```

And in constructor:
```csharp
wishlist = new WishlistRepository(_db);
```

### 6. Database Migration (Need to create)

Run in Package Manager Console:
```
Add-Migration AddWishlistToDb
Update-Database
```

### 7. Update ApplicationDBContext (Need to add)
**File:** `BulkyBook.DataAccess.Data/ApplicationDBContext.cs`

Add this DbSet:
```csharp
public DbSet<Wishlist> Wishlists { get; set; }
```

### 8. CSS for Wishlist (Create new file)
**File:** `wwwroot/css/wishlist.css`

```css
/* Wishlist Button Styles */
.wishlist-btn {
    position: absolute;
    top: 1rem;
    right: 1rem;
    z-index: 10;
    background: rgba(255, 255, 255, 0.9);
    border: none;
    border-radius: 50%;
    width: 40px;
    height: 40px;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: all 0.3s ease;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
}

.wishlist-btn:hover {
    background: white;
    transform: scale(1.1);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.wishlist-btn i {
    font-size: 1.2rem;
    color: #666;
    transition: all 0.3s ease;
}

.wishlist-btn.active i {
    color: #e91e63;
}

.wishlist-btn.active:hover i {
    color: #c2185b;
}

/* Floating Wishlist Button */
.floating-wishlist-btn {
    position: fixed;
    bottom: 120px;
    right: 20px;
    width: 60px;
    height: 60px;
    background: linear-gradient(135deg, #e91e63, #c2185b);
    border: none;
    border-radius: 50%;
    color: white;
    font-size: 1.5rem;
    cursor: pointer;
    box-shadow: 0 4px 16px rgba(233, 30, 99, 0.4);
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.3s ease;
}

.floating-wishlist-btn:hover {
    transform: translateY(-3px) scale(1.05);
    box-shadow: 0 6px 20px rgba(233, 30, 99, 0.6);
}

.floating-wishlist-btn.pulse {
    animation: pulse 0.6s ease;
}

.floating-wishlist-badge {
    position: absolute;
    top: -5px;
    right: -5px;
    background: #fff;
    color: #e91e63;
    border-radius: 50%;
    width: 24px;
    height: 24px;
    display: none;
    align-items: center;
    justify-content: center;
    font-size: 0.75rem;
    font-weight: 700;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}

/* Wishlist Sidebar */
.wishlist-sidebar {
    position: fixed;
    top: 0;
    right: -400px;
    width: 400px;
    height: 100vh;
    background: white;
    box-shadow: -4px 0 16px rgba(0, 0, 0, 0.1);
    z-index: 10000;
    transition: right 0.3s ease;
    display: flex;
    flex-direction: column;
}

.wishlist-sidebar.active {
    right: 0;
}

.wishlist-header {
    padding: 1.5rem;
    border-bottom: 2px solid #f0f0f0;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: linear-gradient(135deg, #e91e63, #c2185b);
    color: white;
}

.wishlist-title {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 700;
}

.wishlist-close-btn {
    background: transparent;
    border: none;
    color: white;
    font-size: 1.5rem;
    cursor: pointer;
    padding: 0.5rem;
    border-radius: 50%;
    transition: all 0.3s ease;
}

.wishlist-close-btn:hover {
    background: rgba(255, 255, 255, 0.2);
}

.wishlist-items-container {
    flex: 1;
    overflow-y: auto;
    padding: 1rem;
}

.wishlist-item {
    display: flex;
    gap: 1rem;
    padding: 1rem;
    border-bottom: 1px solid #f0f0f0;
    transition: background 0.3s ease;
}

.wishlist-item:hover {
    background: #f9f9f9;
}

.wishlist-item-image {
    width: 80px;
    height: 80px;
    object-fit: cover;
    border-radius: 8px;
}

.wishlist-item-details {
    flex: 1;
}

.wishlist-item-title {
    font-size: 0.95rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
    color: #333;
}

.wishlist-item-price {
    margin-bottom: 0.75rem;
}

.wishlist-item-price .current-price {
    font-weight: 700;
    color: #e91e63;
    font-size: 1.1rem;
}

.wishlist-item-price .list-price {
    text-decoration: line-through;
    color: #999;
    font-size: 0.9rem;
    margin-left: 0.5rem;
}

.wishlist-item-actions {
    display: flex;
    gap: 0.5rem;
}

.btn-add-cart-from-wishlist {
    flex: 1;
    background: linear-gradient(135deg, #3B9DD5, #7BC043);
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 6px;
    font-size: 0.85rem;
    cursor: pointer;
    transition: all 0.3s ease;
}

.btn-add-cart-from-wishlist:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(59, 157, 213, 0.4);
}

.btn-remove-wishlist {
    background: #f5f5f5;
    border: none;
    color: #e91e63;
    padding: 0.5rem;
    border-radius: 6px;
    cursor: pointer;
    transition: all 0.3s ease;
}

.btn-remove-wishlist:hover {
    background: #ffe0e6;
}

.wishlist-empty {
    text-align: center;
    padding: 3rem 1rem;
    color: #999;
}

.wishlist-empty i {
    font-size: 3rem;
    margin-bottom: 1rem;
    color: #ddd;
}

.wishlist-footer {
    padding: 1.5rem;
    border-top: 2px solid #f0f0f0;
}

.btn-view-all {
    background: linear-gradient(135deg, #3B9DD5, #7BC043);
    color: white;
    border: none;
    padding: 0.75rem 1.5rem;
    border-radius: 8px;
    text-decoration: none;
    display: block;
    text-align: center;
    font-weight: 600;
    transition: all 0.3s ease;
}

.btn-view-all:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(59, 157, 213, 0.4);
    color: white;
}

.wishlist-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
    z-index: 9999;
    display: none;
    transition: opacity 0.3s ease;
}

.wishlist-overlay.active {
    display: block;
}

/* Wishlist Particles Animation */
@keyframes wishlistParticle {
    0% {
        opacity: 1;
        transform: translate(0, 0) scale(1);
    }
    100% {
        opacity: 0;
        transform: translate(var(--tx), var(--ty)) scale(0);
    }
}

.wishlist-particle {
    position: absolute;
    width: 6px;
    height: 6px;
    background: #e91e63;
    border-radius: 50%;
    animation: wishlistParticle 1s ease-out forwards;
    pointer-events: none;
}

/* Mobile Responsive */
@media (max-width: 768px) {
    .wishlist-sidebar {
        width: 100%;
        right: -100%;
    }
    
    .floating-wishlist-btn {
        bottom: 180px;
        right: 15px;
        width: 50px;
        height: 50px;
        font-size: 1.25rem;
    }
    
    .wishlist-btn {
        width: 35px;
        height: 35px;
        top: 0.75rem;
        right: 0.75rem;
    }
    
    .wishlist-btn i {
        font-size: 1rem;
    }
}
```

### 9. Link CSS in _Layout.cshtml

Add this line in the `<head>` section:
```html
<link rel="stylesheet" href="~/css/wishlist.css" asp-append-version="true" />
```

### 10. Add to ViewBag in HomeController Index

Already added in the updated code.

### 11. Update Index.cshtml

Already added wishlist button rendering and floating button code.

### 12. Update JavaScript

Already updated in `ecommerce-pro-features.js`.

## Summary

The wishlist system is now functional with:
- ✅ Controller actions (ToggleWishlist, GetWishlistItems, RemoveFromWishlist)
- ✅ JavaScript functionality for logged-in users only
- ✅ Filled heart icon on home screen for wishlisted products
- ✅ Floating wishlist button (like cart/WhatsApp)
- ✅ Wishlist sidebar with product list
- ✅ Add to cart from wishlist
- ✅ Remove from wishlist

**Next Steps:**
1. Create Wishlist model in BulkyBook.Models project
2. Create IWishlistRepository interface
3. Create WishlistRepository implementation
4. Update IUnitOfWork and UnitOfWork
5. Add Wishlist DbSet to ApplicationDBContext
6. Create and run database migration
7. Create wishlist.css file
8. Link CSS in _Layout.cshtml

After completing these steps, the wishlist feature will be fully functional!

