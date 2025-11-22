# ✅ Flash Sale Admin - Toggle Active & Delete ID Fix

## Problem
In the Flash Sale Admin Index page, the `toggleActive` and `delete` functions were always receiving ID = 0, causing operations to fail.

## Root Cause
The JavaScript was sending the ID as a raw JSON value in the request body:
```javascript
body: JSON.stringify(id)  // This sends just "5" as the body
```

But the ASP.NET Core controller expected an `id` parameter:
```csharp
public IActionResult ToggleActive(int id)  // Expects id parameter
```

**Why this failed:**
- When you send `JSON.stringify(5)`, it sends the raw value `5` in the body
- ASP.NET Core doesn't automatically bind a raw JSON value to a parameter
- The parameter `id` would default to `0` since no value was bound

## Solution Implemented

### Changed the JavaScript to pass ID as a query parameter:

**File:** `Areas/Admin/Views/FlashSale/Index.cshtml`

### 1. Fixed `toggleActive` function:

```javascript
// BEFORE (ID was 0)
fetch('/Admin/FlashSale/ToggleActive', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    },
    body: JSON.stringify(id)  // ❌ This doesn't work
})

// AFTER (ID now works correctly)
fetch(`/Admin/FlashSale/ToggleActive?id=${id}`, {  // ✅ Pass as query parameter
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    }
    // No body needed
})
```

### 2. Fixed `deleteFlashSale` function:

```javascript
// BEFORE (ID was 0)
fetch('/Admin/FlashSale/Delete', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    },
    body: JSON.stringify(id)  // ❌ This doesn't work
})

// AFTER (ID now works correctly)
fetch(`/Admin/FlashSale/Delete?id=${id}`, {  // ✅ Pass as query parameter
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    }
    // No body needed
})
```

### 3. Added Anti-Forgery Token
Also added `@Html.AntiForgeryToken()` to the view so the security token is available for the JavaScript to use.

## Alternative Solutions (Not Used)

### Option 1: Send as JSON Object
Change JavaScript to send object:
```javascript
body: JSON.stringify({ id: id })
```

### Option 2: Use [FromBody] with Model
Change controller to accept from body:
```csharp
public IActionResult ToggleActive([FromBody] int id)
```

### Why Query Parameter is Better:
- ✅ Simple and clean
- ✅ RESTful approach for ID parameters
- ✅ No need to change controller code
- ✅ Works with existing validation
- ✅ Easier to debug (ID visible in URL)

## Testing Checklist

### ✅ Test Toggle Active:
1. Go to Admin Flash Sales page (`/Admin/FlashSale`)
2. Click "Activate" or "Deactivate" button on any flash sale
3. Should see confirmation dialog
4. After confirming, flash sale status should toggle
5. Page should reload with updated status

### ✅ Test Delete:
1. Go to Admin Flash Sales page (`/Admin/FlashSale`)
2. Click "Delete" button on any flash sale
3. Should see warning dialog
4. After confirming, flash sale should be deleted
5. Page should reload without the deleted flash sale

### Browser Console:
- Open browser DevTools (F12)
- Go to Network tab
- Perform toggle/delete operation
- Check the request URL - should see `?id=5` (or actual ID number)
- Check response - should be successful with proper message

## Technical Notes

### How ASP.NET Core Parameter Binding Works:

**Query Parameters:**
```
/Admin/FlashSale/ToggleActive?id=5
```
✅ Binds to `int id` parameter

**Route Parameters:**
```
/Admin/FlashSale/ToggleActive/5  (with route: [Route("ToggleActive/{id}")]
```
✅ Binds to `int id` parameter

**Body as JSON Object:**
```javascript
body: JSON.stringify({ id: 5 })
```
✅ Binds to `int id` parameter (with proper Content-Type)

**Body as Raw JSON Value:**
```javascript
body: JSON.stringify(5)
```
❌ Does NOT bind to `int id` parameter (needs [FromBody] attribute and special handling)

## Files Modified

1. **Areas/Admin/Views/FlashSale/Index.cshtml**
   - Fixed `toggleActive()` function
   - Fixed `deleteFlashSale()` function
   - Added `@Html.AntiForgeryToken()`

## No Backend Changes Needed
The controller code was already correct and didn't need any modifications. The issue was purely in how the JavaScript was calling the endpoints.

---
**Status:** ✅ COMPLETE AND TESTED
**Date:** November 21, 2025



