-- ===========================================
-- Add Wishlist to IUnitOfWork and UnitOfWork
-- ===========================================
-- 
-- STEP 1: Open IUnitOfWork.cs in BulkyBook.DataAccess project
-- Add this property:
--
-- IWishlistRepository wishlist { get; }
--
-- ===========================================
-- 
-- STEP 2: Open UnitOfWork.cs in BulkyBook.DataAccess project
-- 
-- a) Add this property:
--    public IWishlistRepository wishlist { get; private set; }
--
-- b) In constructor, add this line:
--    wishlist = new WishlistRepository(_db);
--
-- ===========================================
-- 
-- STEP 3: Open ApplicationDBContext.cs in BulkyBook.DataAccess/Data folder
-- Add this DbSet:
--
-- public DbSet<Wishlist> Wishlists { get; set; }
--
-- ===========================================
-- 
-- STEP 4: Run this SQL to create the table:
-- ===========================================

-- Create Wishlist Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Wishlists]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Wishlists] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ProductId] int NOT NULL,
        [ApplicationUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Wishlists_Products_ProductId] 
            FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Products] ([Id]) 
            ON DELETE CASCADE,
        CONSTRAINT [FK_Wishlists_AspNetUsers_ApplicationUserId] 
            FOREIGN KEY ([ApplicationUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) 
            ON DELETE CASCADE
    );
    
    -- Create Indexes for better performance
    CREATE NONCLUSTERED INDEX [IX_Wishlists_ProductId] 
        ON [dbo].[Wishlists] ([ProductId]);
    
    CREATE NONCLUSTERED INDEX [IX_Wishlists_ApplicationUserId] 
        ON [dbo].[Wishlists] ([ApplicationUserId]);
    
    -- Create Unique Constraint to prevent duplicate wishlist items
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Wishlists_User_Product] 
        ON [dbo].[Wishlists] ([ApplicationUserId], [ProductId]);
    
    PRINT 'Wishlists table created successfully!';
END
ELSE
BEGIN
    PRINT 'Wishlists table already exists.';
END
GO

