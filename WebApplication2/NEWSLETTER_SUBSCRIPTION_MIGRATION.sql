-- Newsletter Subscription Table Migration
-- Run this SQL script to create the NewsletterSubscriptions table

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewsletterSubscriptions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[NewsletterSubscriptions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [SubscribedDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [UnsubscribedDate] datetime2 NULL,
        [Source] nvarchar(50) NULL,
        CONSTRAINT [PK_NewsletterSubscriptions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    -- Create unique index on Email to prevent duplicates
    CREATE UNIQUE NONCLUSTERED INDEX [IX_NewsletterSubscriptions_Email] 
    ON [dbo].[NewsletterSubscriptions] ([Email] ASC);
    
    -- Create index on IsActive for filtering active subscriptions
    CREATE NONCLUSTERED INDEX [IX_NewsletterSubscriptions_IsActive] 
    ON [dbo].[NewsletterSubscriptions] ([IsActive] ASC);
    
    PRINT 'NewsletterSubscriptions table created successfully!';
END
ELSE
BEGIN
    PRINT 'NewsletterSubscriptions table already exists.';
END

