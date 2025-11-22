# Complete Wishlist Implementation

This document contains all the files and code needed to fully implement the wishlist feature.

## Files to Create/Update

### 1. ✅ Wishlist Model (Created in Models/Wishlist.cs)
**Location:** Move to `BulkyBook.Models` project or keep in `Models/Wishlist.cs`

### 2. ✅ IWishlistRepository Interface (Created in DataAccess/Repository/IRepository/IWishlistRepository.cs)
**Location:** Move to `BulkyBook.DataAccess` project or keep in current location

### 3. ✅ WishlistRepository Implementation (Created in DataAccess/Repository/WishlistRepository.cs)
**Location:** Move to `BulkyBook.DataAccess` project or keep in current location

### 4. IUnitOfWork Interface Update
**Location:** Find `BulkyBook.DataAccess/IRepository/IUnitOfWork.cs` and add:

```csharp
IWishlistRepository wishlist { get; }
```

### 5. UnitOfWork Class Update
**Location:** Find `BulkyBook.DataAccess/Repository/UnitOfWork.cs` and:

**a) Add property:**
```csharp
public IWishlistRepository wishlist { get; private set; }
```

**b) In constructor, add:**
```csharp
wishlist = new WishlistRepository(_db);
```

### 6. ApplicationDBContext Update
**Location:** Find `BulkyBook.DataAccess/Data/ApplicationDBContext.cs` and add:

```csharp
public DbSet<Wishlist> Wishlists { get; set; }
```

### 7. Database Migration SQL
Run this SQL script in your database:

```sql
-- Create Wishlist Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Wishlists]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Wishlists] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ApplicationUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Wishlists_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Wishlists_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_Wishlists_User_Product] UNIQUE ([ApplicationUserId], [ProductId])
    );
    
    CREATE INDEX [IX_Wishlists_ProductId] ON [dbo].[Wishlists] ([ProductId]);
    CREATE INDEX [IX_Wishlists_ApplicationUserId] ON [dbo].[Wishlists] ([ApplicationUserId]);
END
GO
```

## Alternative: Entity Framework Migration

If you prefer using EF migrations, run in Package Manager Console:

```
Add-Migration AddWishlistToDb
Update-Database
```

## Quick Setup Checklist

- [ ] Copy Wishlist.cs to BulkyBook.Models project
- [ ] Copy IWishlistRepository.cs to BulkyBook.DataAccess project
- [ ] Copy WishlistRepository.cs to BulkyBook.DataAccess project  
- [ ] Update IUnitOfWork interface
- [ ] Update UnitOfWork class
- [ ] Update ApplicationDBContext
- [ ] Run migration or SQL script
- [ ] Test wishlist functionality

All controller code and frontend code is already implemented and ready!

