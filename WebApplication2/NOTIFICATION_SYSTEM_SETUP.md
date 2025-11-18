# 🔔 Order Notification System - Setup Guide

## Overview
Comprehensive notification system that sends real-time push notifications, database logs, and emails to admins and customers after order confirmation.

## ✅ What's Implemented

### 1. **Database Notification Logging**
- ✅ New `Notification` model created
- ✅ Notifications stored in database
- ✅ Track read/unread status
- ✅ Link to related orders

### 2. **Real-Time Push Notifications (SignalR)**
- ✅ SignalR Hub configured
- ✅ Admin group for broadcast notifications
- ✅ Real-time order alerts
- ✅ Browser notifications support

### 3. **Email Notifications**
- ✅ Beautiful HTML email templates
- ✅ Admin notification emails
- ✅ Customer order confirmation emails
- ✅ Order details included

### 4. **UI Components**
- ✅ Notification bell in navbar (for admins)
- ✅ Badge counter for unread notifications
- ✅ Dropdown notification list
- ✅ Modern styling with animations

## 🚀 Setup Steps

### Step 1: Install Required NuGet Packages

Run these commands in Package Manager Console:

```powershell
# For SignalR (real-time notifications)
Install-Package Microsoft.AspNetCore.SignalR
Install-Package Microsoft.AspNetCore.SignalR.Client
```

Or using .NET CLI:

```bash
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

### Step 2: Create Database Migration

Run these commands:

```bash
cd BulkyBook.DataAccess
dotnet ef migrations add AddNotifications --startup-project ../WebApplication2
dotnet ef database update --startup-project ../WebApplication2
```

Or using Package Manager Console:

```powershell
Add-Migration AddNotifications -Project BulkyBook.DataAccess -StartupProject BulkyBook
Update-Database -Project BulkyBook.DataAccess -StartupProject BulkyBook
```

This will create the `Notifications` table in your database with these columns:
- Id (Primary Key)
- Title
- Message
- Type
- UserId (Foreign Key)
- OrderId (Foreign Key, nullable)
- IsRead
- CreatedAt
- Icon
- Link

### Step 3: Update Configuration (Optional)

Add base URL to `appsettings.json`:

```json
{
  "AppSettings": {
    "BaseUrl": "https://msobhyapp.runasp.net"
  }
}
```

Then update `NotificationService.cs` to use it:

```csharp
private string GetBaseUrl()
{
    return _configuration["AppSettings:BaseUrl"] ?? "https://msobhyapp.runasp.net";
}
```

## 📋 How It Works

### When a Customer Places an Order:

1. **Order Confirmation Page Triggered**
   - `CartController.OrderConfirmation()` is called

2. **Notifications Sent to All Admins**
   - Database: Notification logged for each admin
   - Email: Beautiful HTML email with order details sent to all admins
   - Push: Real-time notification sent via SignalR to all connected admins
   - Browser: Desktop notification shown (if permitted)

3. **Confirmation Sent to Customer**
   - Database: Notification logged for customer
   - Email: Order confirmation with full details sent to customer
   - Push: Real-time notification sent to customer

4. **Admin UI Updated**
   - Notification bell shows badge with count
   - Bell rings with animation
   - Red dot appears on bell
   - Toastr popup shows new order
   - Dropdown shows notification list

## 🎨 UI Components

### Notification Bell (Navbar)
- **Location**: Navigation bar (visible to Admins only)
- **Features**:
  - Bell icon with hover animation
  - Red badge with unread count
  - Pulsing dot indicator
  - Ring animation on new notification

### Notification Dropdown
- **Trigger**: Click on notification bell
- **Content**:
  - Header with "Mark all read" button
  - List of notifications with icons
  - Time ago for each notification
  - Click to view order details
  - "View All" link at bottom

### Toastr Popups
- **Success notifications** (Green)
  - "Order Confirmed" for customers
  - "New Order Received" for admins
- **Clickable** - Click to go to order details
- **Auto-dismiss** after 5-10 seconds
- **Sound effect** plays

## 📧 Email Templates

### Admin Email Template
Includes:
- ✅ Order number and date
- ✅ Customer information (name, phone, address)
- ✅ Order items with quantities and prices
- ✅ Order total
- ✅ Payment status
- ✅ "View Order Details" button
- ✅ Professional gradient header
- ✅ Responsive design

### Customer Email Template
Includes:
- ✅ Order confirmation message
- ✅ Order number and date
- ✅ Order items with product details
- ✅ Order total
- ✅ Shipping address
- ✅ Estimated delivery date
- ✅ "Track Order" and "Continue Shopping" buttons
- ✅ Support information
- ✅ Professional design

## 🔔 Notification Types

### Database Notifications
```csharp
{
    Title: "New Order Received",
    Message: "Order #123 by John Doe. Total: $520.00",
    Type: "Order",
    UserId: "admin-user-id",
    OrderId: 123,
    IsRead: false,
    CreatedAt: DateTime.Now,
    Icon: "bi-cart-check",
    Link: "/Admin/Order/Details/123"
}
```

### Push Notifications (SignalR)
```javascript
{
    title: "New Order Received",
    message: "Order #123 - John Doe",
    orderId: 123,
    total: 520.00,
    timestamp: "2024-11-16T..."
}
```

## 🛠️ Files Created/Modified

### Created Files:
1. `../BulkyBook.Models/Notification.cs` - Notification entity
2. `../BulkyBook.DataAccess/Repository/NotificationRepository.cs` - Repository
3. `../BulkyBook.DataAccess/Repository/IRepository/INotificationRepository.cs` - Interface
4. `Hubs/NotificationHub.cs` - SignalR Hub
5. `Services/NotificationService.cs` - Notification service
6. `Services/INotificationService.cs` - Service interface
7. `Areas/Admin/Controllers/NotificationApiController.cs` - API endpoints
8. `wwwroot/js/notifications.js` - SignalR client
9. `wwwroot/js/notification-handler.js` - UI handler
10. `wwwroot/css/notification-bell.css` - Bell styling

### Modified Files:
1. ✅ `Program.cs` - Added SignalR and NotificationService
2. ✅ `Areas/Customer/Controllers/CartController.cs` - Added notification calls
3. ✅ `../BulkyBook.DataAccess/Data/ApplicationDBContext.cs` - Added Notifications DbSet
4. ✅ `../BulkyBook.DataAccess/Repository/UnitOfWork.cs` - Added notification repository
5. ✅ `../BulkyBook.DataAccess/Repository/IRepository/IUnitOfWork.cs` - Added interface
6. ✅ `Views/Shared/_Layout.cshtml` - Added notification bell and scripts

## 🎯 Features

### For Admins:
- ✅ **Real-time push notifications** when orders placed
- ✅ **Email alerts** with full order details
- ✅ **Notification bell** in navbar
- ✅ **Badge counter** for unread notifications
- ✅ **Dropdown list** of recent notifications
- ✅ **Desktop notifications** (browser)
- ✅ **Sound alerts** on new orders
- ✅ **Database log** of all notifications

### For Customers:
- ✅ **Order confirmation email** with full details
- ✅ **Database notification** of order status
- ✅ **Estimated delivery date**
- ✅ **Track order link**
- ✅ **Professional email template**

## 🔧 Testing

### Test Notification System:

1. **Place a Test Order**
   - Login as a customer
   - Add items to cart
   - Complete checkout
   - Confirm payment

2. **Check Admin Notifications**
   - Login as admin in another browser/tab
   - Watch for bell icon to show badge
   - See toastr popup appear
   - Click bell to view dropdown
   - Check email inbox

3. **Check Customer Email**
   - Check customer's email inbox
   - Should receive order confirmation
   - Verify all order details are correct

4. **Check Database**
   - Query `Notifications` table
   - Should see entries for admin and customer

### Debug Checklist:
- [ ] SignalR connected (check browser console)
- [ ] Notification bell visible for admins
- [ ] Toastr library loaded
- [ ] Email service configured (SendGrid)
- [ ] Database migration applied
- [ ] User roles configured correctly

## 🎨 Customization

### Change Notification Position
In `toastr-config.js`:
```javascript
toastr.options.positionClass = "toast-bottom-right"; // or other position
```

### Change Notification Duration
```javascript
toastr.options.timeOut = "8000"; // 8 seconds
```

### Customize Email Template
Edit `NotificationService.cs`:
- `GenerateAdminEmailTemplate()` - Admin email
- `GenerateCustomerEmailTemplate()` - Customer email

### Change Notification Sound
Replace the audio data in `notification-handler.js` with your own sound file:
```javascript
const audio = new Audio('/sounds/notification.mp3');
```

## 🔒 Security

- ✅ Only admins see notification bell
- ✅ SignalR connections are authenticated
- ✅ API endpoints require authorization
- ✅ User can only see their own notifications
- ✅ Admin group is protected

## 📊 Notification Flow

```
Order Placed
    ↓
OrderConfirmation Action
    ↓
    ├─→ NotificationService.SendOrderNotificationToAdmins()
    │       ├─→ Log to database (all admins)
    │       ├─→ Send emails (all admins)
    │       └─→ Push notification via SignalR (all admins)
    │
    └─→ NotificationService.SendOrderConfirmationToCustomer()
            ├─→ Log to database (customer)
            ├─→ Send email (customer)
            └─→ Push notification via SignalR (customer)
```

## 🐛 Troubleshooting

### Notifications Not Showing
1. Check browser console for SignalR connection errors
2. Verify SignalR scripts are loaded
3. Check user has Admin role
4. Clear browser cache

### Emails Not Sending
1. Verify SendGrid API key is configured
2. Check email addresses are valid
3. Check spam folder
4. Review email service logs

### Bell Icon Not Showing
1. Verify user has Admin role
2. Check notification-bell.css is loaded
3. Clear browser cache
4. Verify Bootstrap Icons loaded

### Database Errors
1. Ensure migration is applied
2. Check database connection
3. Verify Notifications table exists
4. Check foreign key constraints

## 🎉 Benefits

### Business Benefits:
- ✅ **Instant order awareness** - No delays
- ✅ **Better customer service** - Quick order processing
- ✅ **Professional communication** - Beautiful emails
- ✅ **Order tracking** - Full audit trail
- ✅ **Multiple channels** - Email + Push + Database

### Technical Benefits:
- ✅ **Real-time updates** - SignalR push notifications
- ✅ **Scalable** - Handles multiple admins
- ✅ **Reliable** - Multiple notification methods
- ✅ **Logged** - Database audit trail
- ✅ **Modern** - Latest technologies

## 📱 Browser Notifications

The system supports desktop browser notifications:
- **Chrome**: Shows OS-level notifications
- **Firefox**: Shows OS-level notifications
- **Edge**: Shows OS-level notifications
- **Safari**: Shows OS-level notifications

Users must grant permission when prompted.

## 🔗 API Endpoints

### GET `/api/notifications/unread`
Returns unread notifications for current user

### POST `/api/notifications/mark-read/{id}`
Marks specific notification as read

### POST `/api/notifications/mark-all-read`
Marks all user's notifications as read

### GET `/api/notifications/count`
Returns count of unread notifications

## 📚 Next Steps

After setup:
1. ✅ Run migration to create Notifications table
2. ✅ Test with a real order
3. ✅ Customize email templates
4. ✅ Add notification history page
5. ✅ Configure notification preferences

## 🎊 Complete Feature Set

Your app now has:
- ✅ **Real-time push notifications** via SignalR
- ✅ **Email notifications** with beautiful templates
- ✅ **Database logging** for audit trail
- ✅ **Notification bell** in navbar
- ✅ **Sound alerts** for new orders
- ✅ **Browser notifications** (desktop)
- ✅ **Mobile responsive** notification UI
- ✅ **Professional design** matching your app theme

---

**Version**: 1.0  
**Last Updated**: November 2024  
**Status**: ✅ Ready to Use (after migration)

**Need Help?**
- Check troubleshooting section
- Review browser console for errors
- Verify all NuGet packages installed
- Ensure migration is applied

