# Regenerate Product Slugs

## Issue
Existing products may have slugs with spaces or special characters. To fix this, you need to regenerate slugs for all products.

## Solution

### Option 1: Bulk Regenerate All Slugs (Recommended - Fastest)
1. Open your browser's developer console (F12)
2. Navigate to the Admin Products page
3. Run this JavaScript in the console:

```javascript
fetch('/Admin/Product/RegenerateAllSlugs', {
    method: 'POST',
    headers: {
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
    }
})
.then(response => response.json())
.then(data => {
    console.log(data.message);
    alert(data.message);
    if (data.success) {
        location.reload();
    }
});
```

Or use a tool like Postman:
- **URL:** `POST /Admin/Product/RegenerateAllSlugs`
- **Headers:** Include your authentication cookie
- This will regenerate slugs for ALL products at once

### Option 2: Edit Products Individually
1. Go to **Admin Panel** → **Products**
2. Click **Edit** on each product
3. Click **Update Product** (slugs will be auto-regenerated)
4. Repeat for all products

## What Changed
- ✅ Slugs now use **hyphens** instead of spaces
- ✅ All special characters (commas, periods, etc.) are **removed**
- ✅ URLs are now **path-based**: `/Customer/Home/Details/product-name` instead of query parameters
- ✅ Maximum slug length is **100 characters** for better SEO
- ✅ Improved slug generation handles Arabic and English text properly

## Example
- **Before:** `Body Builder Whey Protein, Chocolate, 5 LB` (with spaces and commas)
- **After:** `body-builder-whey-protein-chocolate-5-lb` (clean, hyphenated)

## URL Format
- **Before:** `/Customer/Home/Details?slug=Body%20Builder%20Whey%20Protein...` (query parameter with spaces)
- **After:** `/Customer/Home/Details/body-builder-whey-protein-chocolate-5-lb` (clean path-based URL)

