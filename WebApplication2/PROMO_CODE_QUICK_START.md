# 🚀 Promo Code System - Quick Start Guide

## ⚡ Get Started in 5 Minutes

### Step 1: Apply Database Migration (1 minute)

Open Package Manager Console and run:
```powershell
Update-Database
```

Or using terminal in the DataAccess project folder:
```bash
dotnet ef database update --startup-project ../WebApplication2/BulkyBook.csproj
```

### Step 2: Run the Application (30 seconds)

Press F5 or run:
```bash
dotnet run
```

### Step 3: Access Admin Panel (30 seconds)

1. Log in with your admin account
2. Look for "Promo Codes" in the admin menu
3. Click to open promo code management

### Step 4: Create Your First Promo Code (2 minutes)

Click "Create New Promo Code" and try this example:

**Example 1: Simple 20% Off Code**
```
Code: WELCOME20
Description: Welcome discount - 20% off
Type: Percentage
Value: 20
Start Date: Today
End Date: 30 days from now
Status: ✓ Active
```

Click "Create" - Done! ✅

### Step 5: Test It (1 minute)

1. Open your site in a new incognito window
2. Add items to cart
3. Go to checkout
4. Enter code: `WELCOME20`
5. Click "Apply"
6. See your discount! 🎉

---

## 📋 More Quick Examples

### Flash Sale Code
```
Code: FLASH50
Description: Flash Sale - 50% off
Type: Percentage  
Value: 50
Maximum Discount: $200
Usage Limit: 100
Valid: Today only
```

### Fixed Discount Code
```
Code: SAVE25
Description: $25 off your order
Type: Fixed Amount
Value: 25
Minimum Order: $100
Per User Limit: 1
Valid: 30 days
```

### VIP Code
```
Code: VIP100
Description: VIP Members - $100 off
Type: Fixed Amount
Value: 100
Minimum Order: $500
Usage Limit: 50
Valid: This month
```

---

## 🎯 Admin Features at a Glance

| Feature | Location | Action |
|---------|----------|--------|
| View all codes | Admin → Promo Codes | See list |
| Create code | Click "Create New" | Fill form |
| Edit code | Click pencil icon | Modify |
| View stats | Click eye icon | See details |
| Enable/Disable | Click toggle icon | Quick switch |
| Delete | Click trash icon | Remove (if unused) |

---

## 🛍️ Customer Usage

Your customers will see a "Promo Code" section during checkout where they can:
- Enter their code
- See instant validation
- View their discount
- Remove code if needed

---

## 💡 Pro Tips

1. **Test First**: Create test codes with short expiry dates to test functionality
2. **Clear Codes**: Use clear, memorable codes like SAVE20, WELCOME10
3. **Set Limits**: Always set usage limits to prevent abuse
4. **Track Results**: Check the details page to see which codes work best
5. **Marketing**: Share codes on social media, email, etc.

---

## 📱 Mobile Friendly

The entire system works perfectly on mobile devices - both admin and customer interfaces!

---

## 🔗 Quick Links

- **Full Documentation**: See `PROMO_CODE_SYSTEM_COMPLETE.md`
- **Admin Panel**: `/Admin/PromoCode`
- **Checkout Page**: `/Customer/Cart/Summary`

---

## ❓ Need Help?

**Common Issues:**

**Q: Code not working?**
A: Check it's Active, within date range, and has usage left

**Q: Can't delete code?**
A: Codes that have been used can't be deleted, only deactivated

**Q: Discount not showing?**
A: Check minimum order amount requirement

---

## 🎉 You're All Set!

Start creating promo codes and boost your sales! 🚀

**Next Steps:**
1. Create 2-3 different promo codes
2. Test them thoroughly
3. Start promoting them to customers
4. Monitor usage in admin panel

**Happy Selling! 💰**

