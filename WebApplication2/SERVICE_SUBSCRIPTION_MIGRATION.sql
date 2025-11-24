-- ============================================
-- Service Subscription System Migration
-- ============================================
-- This script creates tables for service subscriptions
-- with support for online (full payment) and offline (partial payment) services
-- ============================================

-- ServiceSubscription Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceSubscriptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ServiceSubscriptions] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(500) NOT NULL,
        [TitleAr] NVARCHAR(500) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [DescriptionAr] NVARCHAR(MAX) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [ServiceType] INT NOT NULL, -- 1 = Online, 2 = Offline
        [OfflinePaymentPercent] DECIMAL(5,2) NULL, -- For offline services, percentage to pay online
        [ImageUrl] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [DisplayOrder] INT NOT NULL DEFAULT 0
    );
    
    CREATE INDEX IX_ServiceSubscriptions_IsActive ON [dbo].[ServiceSubscriptions]([IsActive]);
    CREATE INDEX IX_ServiceSubscriptions_ServiceType ON [dbo].[ServiceSubscriptions]([ServiceType]);
END
GO

-- ServiceOffer Table (for applying offers/promo codes to services)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceOffers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ServiceOffers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ServiceSubscriptionId] INT NOT NULL,
        [PromoCodeId] INT NULL, -- Link to existing PromoCode if applicable
        [DiscountType] INT NOT NULL, -- 1 = Percentage, 2 = Fixed Amount
        [DiscountValue] DECIMAL(18,2) NOT NULL,
        [StartDate] DATETIME2 NOT NULL,
        [EndDate] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [CreatedBy] NVARCHAR(450) NULL,
        FOREIGN KEY ([ServiceSubscriptionId]) REFERENCES [dbo].[ServiceSubscriptions]([Id]) ON DELETE CASCADE,
        FOREIGN KEY ([PromoCodeId]) REFERENCES [dbo].[PromoCodes]([Id]) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_ServiceOffers_ServiceSubscriptionId ON [dbo].[ServiceOffers]([ServiceSubscriptionId]);
    CREATE INDEX IX_ServiceOffers_IsActive ON [dbo].[ServiceOffers]([IsActive]);
    CREATE INDEX IX_ServiceOffers_Dates ON [dbo].[ServiceOffers]([StartDate], [EndDate]);
END
GO

-- ServicePurchase Table (tracks customer subscriptions)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServicePurchases]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ServicePurchases] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ServiceSubscriptionId] INT NOT NULL,
        [ApplicationUserId] NVARCHAR(450) NULL, -- NULL for guest users
        [GuestEmail] NVARCHAR(256) NULL, -- For guest users
        [GuestName] NVARCHAR(256) NULL, -- For guest users
        [GuestPhone] NVARCHAR(50) NULL, -- For guest users
        [AmountPaid] DECIMAL(18,2) NOT NULL,
        [TotalAmount] DECIMAL(18,2) NOT NULL,
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected, Refunded
        [PaymentIntentId] NVARCHAR(500) NULL, -- Stripe payment intent ID
        [SessionId] NVARCHAR(500) NULL, -- Stripe session ID
        [ServiceOfferId] INT NULL, -- Applied offer if any
        [DiscountAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [PurchaseDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active', -- Active, Completed, Cancelled
        FOREIGN KEY ([ServiceSubscriptionId]) REFERENCES [dbo].[ServiceSubscriptions]([Id]),
        FOREIGN KEY ([ApplicationUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE SET NULL,
        FOREIGN KEY ([ServiceOfferId]) REFERENCES [dbo].[ServiceOffers]([Id]) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_ServicePurchases_ApplicationUserId ON [dbo].[ServicePurchases]([ApplicationUserId]);
    CREATE INDEX IX_ServicePurchases_ServiceSubscriptionId ON [dbo].[ServicePurchases]([ServiceSubscriptionId]);
    CREATE INDEX IX_ServicePurchases_PaymentStatus ON [dbo].[ServicePurchases]([PaymentStatus]);
    CREATE INDEX IX_ServicePurchases_PurchaseDate ON [dbo].[ServicePurchases]([PurchaseDate]);
END
GO

PRINT 'Service Subscription tables created successfully!';
GO

