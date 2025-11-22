-- ===========================================
-- Wishlist Table Creation Script
-- ===========================================
-- Run this script in your SQL Server database
-- Or use Entity Framework Migration: Add-Migration AddWishlistToDb
-- ===========================================

-- Check if table exists and create it
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Wishlists]') AND type in (N'U'))
BEGIN
    PRINT 'Creating Wishlists table...';
    
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
    
    PRINT 'Wishlists table created successfully!';
    
    -- Create Indexes for better query performance
    PRINT 'Creating indexes...';
    
    CREATE NONCLUSTERED INDEX [IX_Wishlists_ProductId] 
        ON [dbo].[Wishlists] ([ProductId]);
    
    CREATE NONCLUSTERED INDEX [IX_Wishlists_ApplicationUserId] 
        ON [dbo].[Wishlists] ([ApplicationUserId]);
    
    -- Create Unique Constraint to prevent duplicate wishlist items (same user + same product)
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Wishlists_User_Product] 
        ON [dbo].[Wishlists] ([ApplicationUserId], [ProductId]);
    
    PRINT 'Indexes created successfully!';
    PRINT 'Wishlist table setup completed!';
END
ELSE
BEGIN
    PRINT 'Wishlists table already exists.';
    
    -- Check if unique constraint exists
    IF NOT EXISTS (
        SELECT * FROM sys.indexes 
        WHERE name = 'UQ_Wishlists_User_Product' 
        AND object_id = OBJECT_ID('dbo.Wishlists')
    )
    BEGIN
        PRINT 'Adding unique constraint...';
        CREATE UNIQUE NONCLUSTERED INDEX [UQ_Wishlists_User_Product] 
            ON [dbo].[Wishlists] ([ApplicationUserId], [ProductId]);
        PRINT 'Unique constraint added successfully!';
    END
    ELSE
    BEGIN
        PRINT 'Unique constraint already exists.';
    END
END
GO

-- Verify table creation
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Wishlists]') AND type in (N'U'))
BEGIN
    SELECT 
        'Wishlists table exists' AS Status,
        COUNT(*) AS RowCount
    FROM [dbo].[Wishlists];
END
ELSE
BEGIN
    PRINT 'ERROR: Wishlists table was not created!';
END
GO

