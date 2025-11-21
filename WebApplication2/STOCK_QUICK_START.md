# ⚡ STOCK MANAGEMENT - QUICK START

## 🎯 3 Steps to Get Started

---

## ✅ STEP 1: Run Database Migration

Open **Package Manager Console** in Visual Studio:

```powershell
cd ../BulkyBook.DataAccess
Add-Migration AddStockManagementToProduct
Update-Database
```

⏱️ **Time**: 30 seconds

---

## ✅ STEP 2: Add Stock to Products

1. Go to **Admin Panel** → **Products**
2. Click **Edit** on any product
3. Scroll to **"Stock Management"** section (NEW!)
4. Enter:
   - **Stock Quantity**: How many you have (e.g., `50`)
   - **Low Stock Alert**: Warning level (e.g., `10`)
5. Click **Update Product**

⏱️ **Time**: 10 seconds per product

---

## ✅ STEP 3: See It In Action!

Go to **Home Page** and look for:

- **Out of Stock** (StockQuantity = 0):
  ```
  ❌ OUT OF STOCK badge
  "Out of Stock - Currently Unavailable" message
  Disabled "Add to Cart" button
  ```

- **Low Stock** (StockQuantity ≤ Alert Level):
  ```
  🔥 LOW STOCK badge
  "Only 3 left in stock - Order soon!" message
  Working "Add to Cart" button
  ```

- **In Stock** (StockQuantity > Alert Level):
  ```
  🆕 NEW or 📈 TRENDING badges
  No stock warnings
  Working "Add to Cart" button
  ```

⏱️ **Time**: Instant!

---

## 🎯 QUICK TEST

### Test 1: Out of Stock
1. Edit product → Set Stock = `0`
2. Save → View on home page
3. ✅ Should show red "OUT OF STOCK" badge
4. ✅ "Add to Cart" should be disabled

### Test 2: Low Stock
1. Edit product → Set Stock = `3`, Alert = `5`
2. Save → View on home page
3. ✅ Should show orange "LOW STOCK" badge
4. ✅ Should say "Only 3 left!"

### Test 3: In Stock
1. Edit product → Set Stock = `50`, Alert = `10`
2. Save → View on home page
3. ✅ Should show regular badges
4. ✅ Normal "Add to Cart" button

---

## 💡 RECOMMENDED SETTINGS

| Product Type | Stock Quantity | Alert Level |
|--------------|----------------|-------------|
| Popular items | 100+ | 20 |
| Regular items | 30-50 | 10 |
| Limited items | 10-20 | 5 |
| Pre-orders | 0 | 5 |

---

## 🎉 YOU'RE DONE!

**Time to Complete**: 5 minutes total

Your stock management system is now:
- ✅ Storing real stock data
- ✅ Showing accurate availability
- ✅ Creating urgency for low stock
- ✅ Preventing out-of-stock orders

**ENJOY!** 📦✨

