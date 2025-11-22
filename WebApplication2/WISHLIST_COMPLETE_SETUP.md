# Complete Wishlist Implementation - Setup Guide

## ✅ Files Created in Current Workspace

The following files have been created and are ready to use:

### 1. **Models/Wishlist.cs** ✅
- Location: `Models/Wishlist.cs` (Move to BulkyBook.Models project)
- Complete Wishlist model class

### 2. **DataAccess/Repository/IRepository/IWishlistRepository.cs** ✅
- Location: `DataAccess/Repository/IRepository/IWishlistRepository.cs` (Move to BulkyBook.DataAccess project)
- Wishlist repository interface

### 3. **DataAccess/Repository/WishlistRepository.cs** ✅
- Location: `DataAccess/Repository/WishlistRepository.cs` (Move to BulkyBook.DataAccess project)
- Wishlist repository implementation

### 4. **wwwroot/css/wishlist.css** ✅
- Complete CSS styles for wishlist feature
- Already linked in _Layout.cshtml

### 5. **Controller Actions** ✅
- Added to `Areas/Customer/Controllers/HomeController.cs`
- All actions are ready

### 6. **JavaScript Functionality** ✅
- Updated `wwwroot/js/ecommerce-pro-features.js`
- All functions are ready

### 7. **View Updates** ✅
- Updated `Areas/Customer/Views/Home/Index.cshtml`
- Floating button and sidebar are ready

---

## 📝 Files to Update in Separate Projects

### Step 1: Copy Files to Separate Projects

**Copy these files from current workspace to:**
- `Models/Wishlist.cs` → Copy to `BulkyBook.Models` project
- `DataAccess/Repository/IRepository/IWishlistRepository.cs` → Copy to `BulkyBook.DataAccess/IRepository/`
- `DataAccess/Repository/WishlistRepository.cs` → Copy to `BulkyBook.DataAccess/Repository/`

---

### Step 2: Update IUnitOfWork Interface

**File:** `BulkyBook.DataAccess/IRepository/IUnitOfWork.cs`

**ADD this property:**
```csharp
IWishlistRepository wishlist { get; }
```

**Full example:**
```csharp
public interface IUnitOfWork
{
    ICategoryRepository categry { get; }
    IProductRepository product { get; }
    IShoppingCartRepository shoppingCart { get; }
    // ... other repositories ...
    
    // ADD THIS LINE:
    IWishlistRepository wishlist { get; }
    
    void save();
}
```

---

### Step 3: Update UnitOfWork Class

**File:** `BulkyBook.DataAccess/Repository/UnitOfWork.cs`

**A) ADD this property:**
```csharp
public IWishlistRepository wishlist { get; private set; }
```

**B) In constructor, ADD this line:**
```csharp
wishlist = new WishlistRepository(_db);
```

**Full example:**
```csharp
public class UnitOfWork : IUnitOfWork
{
    private ApplicationDBContext _db;
    
    // Existing properties
    public ICategoryRepository categry { get; private set; }
    public IProductRepository product { get; private set; }
    public IShoppingCartRepository shoppingCart { get; private set; }
    // ... other repositories ...
    
    // ADD THIS:
    public IWishlistRepository wishlist { get; private set; }
    
    public UnitOfWork(ApplicationDBContext db)
    {
        _db = db;
        
        // Existing initializations
        categry = new CategoryRepository(_db);
        product = new ProductRepository(_db);
        shoppingCart = new ShoppingCartRepository(_db);
        // ... other initializations ...
        
        // ADD THIS LINE:
        wishlist = new WishlistRepository(_db);
    }
    
    public void save()
    {
        _db.SaveChanges();
    }
}
```

---

### Step 4: Update ApplicationDBContext

**File:** `BulkyBook.DataAccess/Data/ApplicationDBContext.cs`

**ADD this DbSet:**
```csharp
public DbSet<Wishlist> Wishlists { get; set; }
```

**OPTIONAL: In OnModelCreating method, ADD this configuration:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // ... existing configurations ...
    
    // ADD THIS:
    modelBuilder.Entity<Wishlist>()
        .HasOne(w => w.product)
        .WithMany()
        .HasForeignKey(w => w.ProductId)
        .OnDelete(DeleteBehavior.Cascade);
        
    modelBuilder.Entity<Wishlist>()
        .HasOne(w => w.applicationUser)
        .WithMany()
        .HasForeignKey(w => w.ApplicationUserId)
        .OnDelete(DeleteBehavior.Cascade);
        
    // Prevent duplicate wishlist entries
    modelBuilder.Entity<Wishlist>()
        .HasIndex(w => new { w.ApplicationUserId, w.ProductId })
        .IsUnique();
}
```

---

### Step 5: Create Database Migration

**Option A: Using Entity Framework Migration (Recommended)**

In Package Manager Console:
```
Add-Migration AddWishlistToDb
Update-Database
```

**Option B: Run SQL Script Directly**

Run the SQL script in `WISHLIST_MIGRATION.sql` file directly in your SQL Server database.

---

## ✅ Already Completed in Current Project

- ✅ Controller actions (ToggleWishlist, GetWishlistItems, RemoveFromWishlist)
- ✅ JavaScript functionality
- ✅ View updates (heart buttons, floating button, sidebar)
- ✅ CSS styles
- ✅ All frontend code

---

## 🚀 After Setup

Once you've completed the above steps:

1. **Build the solution** - Make sure it compiles without errors
2. **Run the migration** - Create the Wishlist table in database
3. **Test the feature**:
   - Login as a user
   - Click heart icon on product cards to add to wishlist
   - Click floating wishlist button to view all items
   - Add items to cart from wishlist
   - Remove items from wishlist

---

## 📋 Quick Checklist

- [ ] Copy `Models/Wishlist.cs` to BulkyBook.Models project
- [ ] Copy `DataAccess/Repository/IRepository/IWishlistRepository.cs` to BulkyBook.DataAccess project
- [ ] Copy `DataAccess/Repository/WishlistRepository.cs` to BulkyBook.DataAccess project
- [ ] Update `IUnitOfWork.cs` - Add `wishlist` property
- [ ] Update `UnitOfWork.cs` - Add `wishlist` property and initialization
- [ ] Update `ApplicationDBContext.cs` - Add `Wishlists` DbSet
- [ ] Run migration or SQL script
- [ ] Build solution
- [ ] Test wishlist feature

---

## 📄 Reference Files

- **WISHLIST_MIGRATION.sql** - SQL script to create database table
- **UPDATE_IUnitOfWork.txt** - Instructions for updating IUnitOfWork
- **UPDATE_UnitOfWork.txt** - Instructions for updating UnitOfWork
- **UPDATE_ApplicationDBContext.txt** - Instructions for updating ApplicationDBContext
- **WISHLIST_IMPLEMENTATION_GUIDE.md** - Detailed implementation guide

All code is ready and waiting for you to connect the repository files!

