# Guest Checkout Feature - Complete Implementation Guide

## 📋 Overview

The Guest Checkout feature allows users to browse products, add items to cart, and complete purchases **without creating an account**. This significantly reduces friction in the buying process and can improve conversion rates.

---

## ✨ Features Implemented

### 1. **Session-Based Cart for Guests**
- Cart items are stored in session for unauthenticated users
- Persistent across page navigation until session expires
- Automatic cleanup after 100 minutes (configurable)

### 2. **Guest Browsing**
- All products visible without login
- Add to cart functionality available for guests
- Cart sidebar and floating cart button for all users

### 3. **Guest Checkout Process**
- Email field required for order tracking
- Full shipping details collection
- Stripe payment integration
- Order confirmation page

### 4. **Order Tracking for Guests**
- Track orders using Order ID + Email
- View complete order details
- Check order and payment status
- Accessible via footer link

---

## 🏗️ Technical Implementation

### Database Changes

#### **OrderHeader Model Updates**
```csharp
public class OrderHeader
{
    public string? ApplicationUserId { get; set; }    // Now nullable
    public string? Email { get; set; }                 // New field for guest email
    public bool IsGuestOrder { get; set; } = false;   // New field to identify guest orders
    // ... other existing fields
}
```

### New Files Created

#### **1. GuestCartItem.cs** (`BulkyBook.Models`)
```csharp
// Model for storing cart items in session
public class GuestCartItem
{
    public int ProductId { get; set; }
    public int Count { get; set; }
    public DateTime AddedAt { get; set; }
}
```

#### **2. GuestCartHelper.cs** (`BulkyBook.Utility`)
```csharp
// Helper class for managing session-based cart
public static class GuestCartHelper
{
    - GetGuestCart(ISession session)
    - SaveGuestCart(ISession session, List<GuestCartItem> cart)
    - AddToCart(ISession session, int productId, int count)
    - RemoveFromCart(ISession session, int productId)
    - UpdateQuantity(ISession session, int productId, int count)
    - ClearCart(ISession session)
    - GetCartCount(ISession session)
}
```

#### **3. TrackOrder.cshtml** (`Areas/Customer/Views/Home`)
- Order tracking form
- Input fields: Order ID and Email
- Validation and error handling

#### **4. OrderTracking.cshtml** (`Areas/Customer/Views/Home`)
- Complete order details display
- Customer information
- Shipping information
- Order items list
- Order status and tracking

---

## 🔄 Modified Files

### **Program.cs**
```csharp
// Added session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Added middleware
app.UseSession();
```

### **HomeController.cs**
**Changes:**
- Removed `[Authorize]` from `Details` action
- Updated `ToggleCart` to support both authenticated and guest users
- Updated `GetCartProductIds` to check session for guests
- Added `TrackOrder` GET and POST actions

**Key Methods:**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult ToggleCart(int productId)
{
    if (User.Identity.IsAuthenticated)
    {
        // Use database cart
    }
    else
    {
        // Use session cart
        GuestCartHelper.AddToCart(HttpContext.Session, productId, 1);
    }
}
```

### **CartController.cs**
**Changes:**
- Removed `[Authorize]` attribute from controller
- Updated `Index` action to load cart from session for guests
- Updated `Summary` action to handle guest checkout
- Updated `SummaryPOST` to process guest orders
- Updated `OrderConfirmation` to clear session cart for guests
- Updated `GetCartItems` to support guests
- Updated `UpdateQuantity`, `Pluse`, `Minus`, `Remove` actions

**Key Methods:**
```csharp
public IActionResult Summary()
{
    if (User.Identity.IsAuthenticated)
    {
        // Load from database
    }
    else
    {
        // Load from session
        var guestCart = GuestCartHelper.GetGuestCart(HttpContext.Session);
        ShoppingCartVM.OrderHeader.IsGuestOrder = true;
    }
}
```

### **Summary.cshtml**
**Changes:**
- Added email field for guest users
- Added guest checkout notification banner
- Conditional rendering based on authentication status

```html
@if (Model.OrderHeader.IsGuestOrder || !User.Identity.IsAuthenticated)
{
    <div class="alert alert-info">
        <strong>Guest Checkout</strong> - Your order details will be sent to your email.
    </div>
    <div class="form-row">
        <label>Email Address *</label>
        <input asp-for="OrderHeader.Email" type="email" class="form-control" required />
    </div>
}
```

### **Index.cshtml** (Cart)
**Changes:**
- Updated cart quantity controls to pass `ProductId` parameter
- Support for both database cart ID and session-based product ID

### **Index.cshtml** (Home)
**Changes:**
- Removed authentication check for "Add to Cart" buttons
- Show cart functionality for all users
- Updated JavaScript to handle guest cart operations
- Removed redirect to login for cart operations

### **_Layout.cshtml**
**Changes:**
- Updated "Track Order" footer link to point to actual page

---

## 🚀 How It Works

### For Guest Users:

1. **Browse Products**
   - No login required
   - View all products and categories

2. **Add to Cart**
   - Click "Add to Cart" button
   - Items stored in browser session
   - Cart badge updates automatically

3. **View Cart**
   - Click cart icon or floating cart button
   - See all cart items
   - Modify quantities or remove items

4. **Checkout**
   - Click "Proceed to Summary"
   - Enter email address (required)
   - Fill in shipping details
   - Complete payment via Stripe

5. **Order Confirmation**
   - View order confirmation
   - Receive order ID
   - Session cart cleared

6. **Track Order**
   - Visit "Track Order" page (footer link)
   - Enter Order ID and Email
   - View complete order details and status

### For Authenticated Users:

- Cart items stored in database
- No email required (linked to account)
- Order history available in account
- All guest features also available

---

## 🔒 Security Considerations

1. **Session Security**
   - HttpOnly cookies enabled
   - Essential cookies for GDPR compliance
   - 100-minute timeout

2. **Order Tracking**
   - Requires both Order ID AND Email
   - No order details exposed without matching credentials

3. **Payment Processing**
   - Stripe handles all payment data
   - PCI DSS compliant
   - No credit card data stored

---

## 📊 Database Migration Required

**IMPORTANT:** You must run a database migration to apply the changes to the `OrderHeader` table.

### Steps:

#### Option 1: Package Manager Console (Visual Studio)
```bash
Add-Migration GuestCheckoutSupport -Project BulkyBook.DataAccess
Update-Database -Project BulkyBook.DataAccess
```

#### Option 2: .NET CLI
```bash
dotnet ef migrations add GuestCheckoutSupport --project BulkyBook.DataAccess --startup-project WebApplication2
dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
```

---

## ✅ Testing Checklist

### Guest User Flow:
- [ ] Browse products without login
- [ ] Add products to cart as guest
- [ ] View cart items in sidebar
- [ ] Update quantities in cart
- [ ] Remove items from cart
- [ ] Proceed to checkout
- [ ] Fill email and shipping details
- [ ] Complete Stripe payment
- [ ] View order confirmation
- [ ] Track order using Order ID + Email
- [ ] Verify session cart clears after purchase

### Authenticated User Flow:
- [ ] Login and add products to cart
- [ ] Cart persists across sessions (database)
- [ ] Checkout without email field
- [ ] Order linked to user account
- [ ] All guest features work for authenticated users

### Edge Cases:
- [ ] Empty cart redirects properly
- [ ] Invalid Order ID/Email combination fails gracefully
- [ ] Session expiration handling
- [ ] Page refresh maintains cart items
- [ ] Multiple concurrent guest carts (different browsers)

---

## 🎨 UI/UX Improvements

1. **Clear Guest Indicators**
   - Info banner on checkout page
   - Email field prominently displayed
   - "Guest Checkout" badge

2. **Floating Cart Button**
   - Visible on all pages
   - Shows item count
   - Quick access to cart

3. **Order Tracking**
   - Easily accessible from footer
   - Simple two-field form
   - Comprehensive order details view

4. **Responsive Design**
   - Works on all devices
   - Mobile-friendly cart sidebar
   - Touch-optimized buttons

---

## 📈 Future Enhancements

1. **Email Notifications**
   - Send order confirmation email to guest
   - Include order tracking link
   - Order status update emails

2. **Guest to Account Conversion**
   - "Create account with this order" option
   - Pre-fill registration with guest data
   - Link past guest orders to new account

3. **Cart Persistence**
   - Store guest cart in cookies for longer retention
   - Option to send cart link via email
   - "Save cart for later" functionality

4. **Analytics**
   - Track guest vs. authenticated conversions
   - Cart abandonment rates
   - Guest order value metrics

5. **Enhanced Order Tracking**
   - Real-time shipping updates
   - SMS notifications
   - Map view of delivery

---

## 🛠️ Configuration

### Session Timeout (Program.cs)
```csharp
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100); // Adjust as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

### Stripe Configuration
Ensure your `appsettings.json` has valid Stripe keys:
```json
{
  "Stripe": {
    "SecretKey": "your_stripe_secret_key",
    "PublishableKey": "your_stripe_publishable_key"
  }
}
```

---

## 📞 Support & Troubleshooting

### Common Issues:

**1. Cart not persisting**
- Check if session middleware is configured
- Verify `app.UseSession()` is called before `app.UseAuthorization()`
- Clear browser cookies and test again

**2. Order tracking not working**
- Run database migration
- Check if `Email` and `IsGuestOrder` fields exist in OrderHeader table
- Verify email matching is case-insensitive

**3. Checkout errors**
- Ensure Stripe keys are configured
- Check if email validation is working
- Verify all required fields are present

---

## 📝 Notes

- Guest cart items expire with session (100 minutes by default)
- Guest orders are marked with `IsGuestOrder = true`
- ApplicationUserId is null for guest orders
- Email is required for all guest orders
- Order tracking requires exact email match

---

## 🎉 Benefits

1. **Increased Conversions** - Reduced friction in checkout process
2. **Better UX** - Users can browse and buy without commitment
3. **Lower Barrier** - No account creation required
4. **Flexibility** - Option to create account later
5. **Competitive Advantage** - Many sites still require accounts

---

**Implementation Complete! 🚀**

The guest checkout feature is now fully functional and ready for testing. Remember to run the database migration before deploying to production.

