# Guest Checkout - Testing Instructions

## 🚦 Before Testing

### 1. Run Database Migration
**CRITICAL:** You must run this migration before testing!

```bash
# Option 1: Package Manager Console
Add-Migration GuestCheckoutSupport -Project BulkyBook.DataAccess
Update-Database -Project BulkyBook.DataAccess

# Option 2: .NET CLI
dotnet ef migrations add GuestCheckoutSupport --project BulkyBook.DataAccess --startup-project WebApplication2
dotnet ef database update --project BulkyBook.DataAccess --startup-project WebApplication2
```

### 2. Verify Stripe Configuration
Check `appsettings.json`:
```json
{
  "Stripe": {
    "SecretKey": "your_test_secret_key",
    "PublishableKey": "your_test_publishable_key"
  }
}
```

Use Stripe test keys for testing!

### 3. Build and Run
```bash
dotnet build
dotnet run --project WebApplication2
```

---

## 🧪 Test Scenarios

### **Scenario 1: Guest Checkout (Happy Path)**

1. **Open incognito/private browser window** (to ensure no existing session)

2. **Browse Products**
   - Navigate to home page: `https://localhost:XXXX`
   - Verify products are visible without login
   - ✅ Expected: All products displayed with "Add to Cart" buttons

3. **Add Items to Cart**
   - Click "Add to Cart" on 2-3 products
   - Check floating cart badge updates
   - ✅ Expected: Cart count increases, success notifications appear

4. **View Cart**
   - Click cart icon or floating cart button
   - Verify items are listed with correct quantities and prices
   - ✅ Expected: Cart sidebar shows all added items

5. **Modify Cart**
   - Increase quantity on one item
   - Decrease quantity on another
   - Remove one item
   - ✅ Expected: Cart updates correctly, totals recalculate

6. **Proceed to Checkout**
   - Click "Proceed to Summary"
   - ✅ Expected: Redirected to checkout page

7. **Fill Checkout Form**
   - Note: Email field should be visible with "Guest Checkout" banner
   - Fill in:
     - **Email:** `guest@test.com`
     - **Name:** `Test Guest`
     - **Phone:** `1234567890`
     - **Street Address:** `123 Test St`
     - **City:** `Test City`
     - **State:** `TS`
     - **Postal Code:** `12345`
   - ✅ Expected: Form accepts all inputs

8. **Complete Payment**
   - Click "Place Order"
   - Use Stripe test card: `4242 4242 4242 4242`
   - Any future expiry date (e.g., 12/25)
   - Any 3-digit CVC (e.g., 123)
   - ✅ Expected: Redirected to Stripe checkout

9. **Order Confirmation**
   - Complete Stripe payment
   - ✅ Expected: Redirected to order confirmation page
   - ✅ Note down the Order ID shown

10. **Track Order**
    - Navigate to footer → "Track Order" link
    - Enter Order ID and Email used (`guest@test.com`)
    - Click "Track Order"
    - ✅ Expected: See complete order details with status

---

### **Scenario 2: Cart Persistence**

1. **Add items as guest**
   - Add 2-3 products to cart

2. **Navigate around site**
   - Go to different product pages
   - View cart again
   - ✅ Expected: Cart items persist

3. **Close and reopen browser**
   - Close browser (not just tab)
   - Reopen and navigate to site
   - ⚠️ Expected: Cart is empty (session expired)

---

### **Scenario 3: Authenticated User Cart**

1. **Login as existing user**
   - Login with your account

2. **Add items to cart**
   - Add 2-3 products

3. **Proceed to checkout**
   - ✅ Expected: No email field (already authenticated)
   - ✅ Expected: Name/address pre-filled from profile

4. **Complete purchase**
   - Verify order completes successfully

5. **Logout and login again**
   - ✅ Expected: Cart persists (stored in database)

---

### **Scenario 4: Empty Cart Handling**

1. **As guest, navigate to cart**
   - Go to: `https://localhost:XXXX/Customer/Cart/Index`
   - ✅ Expected: Shows empty cart message

2. **Try to checkout with empty cart**
   - Navigate to: `https://localhost:XXXX/Customer/Cart/Summary`
   - ✅ Expected: Redirected to home with error message

---

### **Scenario 5: Order Tracking Validation**

1. **Try invalid Order ID**
   - Go to "Track Order"
   - Enter: Order ID = `99999`, Email = `test@test.com`
   - ✅ Expected: "Order not found" error message

2. **Try wrong email**
   - Enter: Order ID = `[valid ID]`, Email = `wrong@test.com`
   - ✅ Expected: "Order not found" error message

3. **Try valid combination**
   - Enter correct Order ID + Email
   - ✅ Expected: Order details displayed

---

### **Scenario 6: Mixed Cart (Guest to Authenticated)**

1. **Add items as guest**
   - Add 2 products to cart

2. **Login**
   - Click login and authenticate
   - ⚠️ Expected: Guest cart in session, user cart in database
   - ⚠️ Note: This is a known limitation - carts don't merge automatically

3. **Optional Enhancement:** Implement cart merging logic

---

### **Scenario 7: Cart Sidebar Functionality**

1. **Add items to cart**
   - Add 3 products

2. **Click floating cart button**
   - ✅ Expected: Sidebar slides in from right

3. **Modify quantities in sidebar**
   - Use +/- buttons
   - ✅ Expected: Quantities update, subtotal recalculates

4. **Remove item from sidebar**
   - Click trash icon
   - ✅ Expected: Item removed, cart badge updates

5. **Click checkout from sidebar**
   - ✅ Expected: Redirected to checkout page

---

## 🔍 What to Look For

### ✅ Success Indicators:
- No authentication required for browsing/cart
- Cart badge shows correct count
- Email field appears for guests in checkout
- Stripe payment processes successfully
- Order confirmation displays Order ID
- Order tracking works with correct credentials
- Cart clears after successful purchase
- Session cart works across page navigation

### ❌ Potential Issues:
- Cart items disappear on page refresh (check session middleware)
- Email field missing on checkout (check IsGuestOrder logic)
- Order tracking fails (check database migration)
- Cart badge not updating (check JavaScript)
- Checkout redirects to login (check [Authorize] attributes removed)

---

## 🐛 Debugging Tips

### If cart doesn't persist:
```csharp
// Add logging to GuestCartHelper
var cart = GetGuestCart(session);
Console.WriteLine($"Cart items: {cart.Count}");
```

### If order tracking fails:
```sql
-- Check if guest orders are created
SELECT Id, Email, IsGuestOrder, ApplicationUserId 
FROM OrderHeaders 
WHERE IsGuestOrder = 1;
```

### If email field doesn't show:
```html
<!-- Check in Summary.cshtml -->
@if (!User.Identity.IsAuthenticated)
{
    <p>User is NOT authenticated</p>
}
```

### Check session configuration:
```csharp
// In Program.cs, verify order:
app.UseRouting();
app.UseSession();      // Must be BEFORE UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
```

---

## 📊 Test Data

### Stripe Test Cards:
- **Success:** 4242 4242 4242 4242
- **Declined:** 4000 0000 0000 0002
- **Requires Auth:** 4000 0025 0000 3155

### Sample Guest Data:
```
Email: guest1@test.com
Name: John Guest
Phone: 5551234567
Address: 123 Main St
City: Springfield
State: IL
Postal: 62701
```

---

## ✅ Checklist

Before marking as complete:

- [ ] Database migration applied successfully
- [ ] Guest can browse without login
- [ ] Guest can add items to cart
- [ ] Cart persists across pages (same session)
- [ ] Cart sidebar works for guests
- [ ] Floating cart badge updates correctly
- [ ] Guest checkout shows email field
- [ ] Stripe payment completes for guest
- [ ] Order confirmation displays
- [ ] Order tracking works with Order ID + Email
- [ ] Order tracking fails with wrong credentials
- [ ] Empty cart redirects properly
- [ ] Cart clears after successful purchase
- [ ] Authenticated users can still checkout normally
- [ ] No console errors in browser
- [ ] No errors in application logs

---

## 🎯 Performance Testing

1. **Add 50 items to cart**
   - Test session size limits
   - Verify performance doesn't degrade

2. **Multiple concurrent sessions**
   - Open 3-4 incognito windows
   - Add different items in each
   - Verify no cart mixing

3. **Session expiry**
   - Add items to cart
   - Wait for session timeout (100 minutes or reduce for testing)
   - Try to checkout
   - Should handle gracefully

---

## 📞 Need Help?

If tests fail, check:
1. Database migration ran successfully
2. Session middleware is configured
3. All [Authorize] attributes removed from necessary actions
4. GuestCartHelper is working correctly
5. Stripe keys are valid

Review `GUEST_CHECKOUT_GUIDE.md` for implementation details.

---

**Happy Testing! 🎉**

