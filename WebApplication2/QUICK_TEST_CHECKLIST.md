# ✅ Quick Test Checklist - Order Details Fix

## 🚀 What Changed

1. ✅ Controller parameter: `orderId` → **`id`**
2. ✅ JavaScript: Updated to handle **both PascalCase and camelCase**
3. ✅ Added debug logging for troubleshooting

---

## 🧪 Test NOW (5 Minutes)

### Step 1: Clear Cache & Restart
```bash
# 1. Clear browser cache (Ctrl+Shift+Delete)
#    OR open Incognito window

# 2. Stop app (Ctrl+C)

# 3. Restart
dotnet run --project WebApplication2
```

### Step 2: Test Order List
1. Login as Admin
2. Go to `/Admin/Order/Index`
3. ✅ Table should display all orders
4. ✅ No red errors in browser console (F12)

### Step 3: Test Details Link
1. **Hover** over any "Details" button
2. ✅ Tooltip should show: **"Order ID: 123"**
3. **Click** Details button
4. ✅ URL should be: **/Admin/Order/Details/123** (actual number)
5. ✅ Order details page loads correctly

---

## ✅ Success = You See:

- Order list displays ✅
- Details button hover shows Order ID ✅
- Clicking Details goes to correct URL ✅
- Details page loads with order info ✅
- No console errors ✅

---

## ❌ If Still Broken:

### Check Browser Console (F12)
Look for:
```
Invalid Order ID: {Id: 0, ...}
```

This means JSON property names don't match.

### Quick Fix:
**Add to Program.cs** (after `builder.Services.AddControllers()`):
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = 
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

Then restart app.

---

## 📞 What to Share If Still Broken:

1. Screenshot of browser console (F12 → Console tab)
2. Network tab response (F12 → Network → GetAll → Preview)
3. Hover tooltip text from Details button

---

**Quick test should take less than 5 minutes! 🚀**

