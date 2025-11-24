# Service Subscription System - Complete Setup Guide

## Overview
This guide will help you set up the complete Service Subscription feature with support for:
- Online services (full payment)
- Offline services (partial payment with configurable percentage)
- Admin notifications and email alerts
- Offer/promo code support
- Guest and registered user support

## Step 1: Database Migration

Run the SQL script `SERVICE_SUBSCRIPTION_MIGRATION.sql` on your database to create the required tables:
- ServiceSubscriptions
- ServiceOffers
- ServicePurchases

## Step 2: Add Models to BulkyBook.Models Project

Add the model classes from `SERVICE_SUBSCRIPTION_MODELS.txt` to your BulkyBook.Models project:
1. ServiceSubscription.cs
2. ServiceOffer.cs
3. ServicePurchase.cs

## Step 3: Add Repositories to UnitOfWork

### Update IUnitOfWork Interface
Add these properties to `BulkyBook.DataAccess/IRepository/IUnitOfWork.cs`:

```csharp
IServiceSubscriptionRepository ServiceSubscription { get; }
IServiceOfferRepository ServiceOffer { get; }
IServicePurchaseRepository ServicePurchase { get; }
```

### Create Repository Interfaces
Create these files in `BulkyBook.DataAccess/Repository/IRepository/`:

**IServiceSubscriptionRepository.cs:**
```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceSubscriptionRepository : IRepository<ServiceSubscription>
    {
        void Update(ServiceSubscription obj);
    }
}
```

**IServiceOfferRepository.cs:**
```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceOfferRepository : IRepository<ServiceOffer>
    {
        void Update(ServiceOffer obj);
    }
}
```

**IServicePurchaseRepository.cs:**
```csharp
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServicePurchaseRepository : IRepository<ServicePurchase>
    {
        void Update(ServicePurchase obj);
    }
}
```

### Create Repository Implementations
Create these files in `BulkyBook.DataAccess/Repository/`:

**ServiceSubscriptionRepository.cs:**
```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServiceSubscriptionRepository : Repository<ServiceSubscription>, IServiceSubscriptionRepository
    {
        private ApplicationDBContext _db;
        public ServiceSubscriptionRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceSubscription obj)
        {
            _db.ServiceSubscriptions.Update(obj);
        }
    }
}
```

**ServiceOfferRepository.cs:**
```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServiceOfferRepository : Repository<ServiceOffer>, IServiceOfferRepository
    {
        private ApplicationDBContext _db;
        public ServiceOfferRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceOffer obj)
        {
            _db.ServiceOffers.Update(obj);
        }
    }
}
```

**ServicePurchaseRepository.cs:**
```csharp
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServicePurchaseRepository : Repository<ServicePurchase>, IServicePurchaseRepository
    {
        private ApplicationDBContext _db;
        public ServicePurchaseRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServicePurchase obj)
        {
            _db.ServicePurchases.Update(obj);
        }
    }
}
```

### Update ApplicationDBContext
Add these DbSets to `BulkyBook.DataAccess/Data/ApplicationDBContext.cs`:

```csharp
public DbSet<ServiceSubscription> ServiceSubscriptions { get; set; }
public DbSet<ServiceOffer> ServiceOffers { get; set; }
public DbSet<ServicePurchase> ServicePurchases { get; set; }
```

### Update UnitOfWork Implementation
In `BulkyBook.DataAccess/Repository/UnitOfWork.cs`, add:

```csharp
private IServiceSubscriptionRepository _serviceSubscription;
private IServiceOfferRepository _serviceOffer;
private IServicePurchaseRepository _servicePurchase;

public IServiceSubscriptionRepository ServiceSubscription
{
    get
    {
        if (_serviceSubscription == null)
            _serviceSubscription = new ServiceSubscriptionRepository(_db);
        return _serviceSubscription;
    }
}

public IServiceOfferRepository ServiceOffer
{
    get
    {
        if (_serviceOffer == null)
            _serviceOffer = new ServiceOfferRepository(_db);
        return _serviceOffer;
    }
}

public IServicePurchaseRepository ServicePurchase
{
    get
    {
        if (_servicePurchase == null)
            _servicePurchase = new ServicePurchaseRepository(_db);
        return _servicePurchase;
    }
}
```

## Step 4: Update Notification Service

Add this method to `Services/INotificationService.cs`:

```csharp
Task SendServicePurchaseNotificationToAdmins(ServicePurchase purchase);
```

And implement it in `Services/NotificationService.cs`:

```csharp
public async Task SendServicePurchaseNotificationToAdmins(ServicePurchase purchase)
{
    // Get admin email from configuration
    var adminEmail = _configuration["StockAlerts:AdminEmail"];
    
    // Send email notification
    if (!string.IsNullOrEmpty(adminEmail))
    {
        var emailBody = GenerateServicePurchaseEmailTemplate(purchase);
        await _emailSender.SendEmailAsync(
            adminEmail,
            $"New Service Subscription Purchase #{purchase.Id} - Ideal Weight",
            emailBody
        );
    }

    // Send push notifications to all admins
    var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
    foreach (var admin in adminUsers)
    {
        await LogNotification(
            admin.Id,
            "New Service Subscription",
            $"New service subscription #{purchase.Id} - {purchase.ServiceSubscription?.Title}. Amount: {purchase.AmountPaid:C}",
            "ServiceSubscription",
            purchase.Id
        );
    }

    // Send real-time notification via SignalR
    await _hubContext.Clients.Group("Admins").SendAsync(
        "ReceiveServiceSubscriptionNotification",
        new
        {
            title = "New Service Subscription",
            message = $"Service subscription #{purchase.Id} - {purchase.ServiceSubscription?.Title}",
            purchaseId = purchase.Id,
            amount = purchase.AmountPaid,
            timestamp = DateTime.Now
        }
    );
}
```

## Step 5: Create Images Directory

Create the directory `wwwroot/images/services/` for storing service images.

## Step 6: Add Navigation Links

### Admin Navigation
Add to admin navigation menu:

```html
<li>
    <a class="dropdown-item" asp-area="Admin" asp-controller="ServiceSubscription" asp-action="Index">
        <i class="bi bi-briefcase me-2"></i>Service Subscriptions
    </a>
</li>
<li>
    <a class="dropdown-item" asp-area="Admin" asp-controller="ServiceOffer" asp-action="Index">
        <i class="bi bi-tag me-2"></i>Service Offers
    </a>
</li>
```

### Customer Navigation
Add to customer navigation:

```html
<li>
    <a class="nav-link" asp-area="Customer" asp-controller="ServiceSubscription" asp-action="Index">
        <i class="bi bi-briefcase me-2"></i>Services
    </a>
</li>
```

## Step 7: Test the Feature

1. **Create a Service:**
   - Go to Admin > Service Subscriptions > Create
   - Fill in service details
   - Choose Online or Offline type
   - For offline, set payment percentage
   - Upload an image
   - Save

2. **Create an Offer (Optional):**
   - Go to Admin > Service Offers > Create
   - Select a service
   - Set discount type and value
   - Set start and end dates
   - Save

3. **Test Customer Subscription:**
   - Go to Customer > Services
   - View service details
   - Subscribe as guest or registered user
   - Complete payment via Stripe
   - Verify admin notifications

## Features Included

✅ **Service Management:**
- Create, edit, delete services
- Online (full payment) and Offline (partial payment) types
- Image upload support
- Active/inactive status toggle

✅ **Offer Management:**
- Apply offers to services
- Link to existing promo codes
- Percentage or fixed amount discounts
- Date range validation

✅ **Payment Processing:**
- Stripe integration
- Full payment for online services
- Partial payment for offline services
- Guest and registered user support

✅ **Notifications:**
- Email to configured admin email
- Push notifications to all admins
- Real-time SignalR notifications
- Database notification logging

✅ **User Experience:**
- Modern, responsive design
- Beautiful UI with animations
- Mobile-friendly
- Arabic and English support ready

## Configuration

The admin email for notifications is configured in `appsettings.json`:
```json
"StockAlerts": {
    "AdminEmail": "smosobhy@gmail.com"
}
```

## Troubleshooting

1. **Images not displaying:** Check that `wwwroot/images/services/` directory exists and has proper permissions.

2. **Payment not processing:** Verify Stripe keys are correctly configured in `appsettings.json`.

3. **Notifications not sending:** Check email configuration in `appsettings.json` under `Smtp` section.

4. **Repository errors:** Ensure all repositories are properly registered in UnitOfWork and ApplicationDBContext.

## Support

For issues or questions, refer to the code comments or check the implementation files.

