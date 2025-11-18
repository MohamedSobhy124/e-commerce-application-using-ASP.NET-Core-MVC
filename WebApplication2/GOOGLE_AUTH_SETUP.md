# 🔐 Google Authentication Setup Guide

## Overview
This guide will help you set up Google authentication for your BulkyBook e-commerce application, allowing users to register and login using their Google accounts.

## ✅ What's Already Done

1. ✅ **Backend Configuration** - `Program.cs` updated with Google authentication
2. ✅ **Configuration File** - `appsettings.json` prepared with placeholders
3. ✅ **Modern Login Page** - Updated with Google login button
4. ✅ **Modern Register Page** - Updated with Google signup button
5. ✅ **CSS Styling** - `external-login.css` created for Google button styling

## 🚀 Setup Steps

### Step 1: Install NuGet Package

Open your Package Manager Console and run:

```powershell
Install-Package Microsoft.AspNetCore.Authentication.Google
```

Or using .NET CLI:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google
```

### Step 2: Create Google OAuth 2.0 Credentials

1. **Go to Google Cloud Console**
   - Visit: https://console.cloud.google.com/

2. **Create a New Project** (or select existing)
   - Click "Select a project" → "New Project"
   - Name it: "BulkyBook" or your preferred name
   - Click "Create"

3. **Enable Google+ API**
   - Go to "APIs & Services" → "Library"
   - Search for "Google+ API"
   - Click on it and press "Enable"

4. **Configure OAuth Consent Screen**
   - Go to "APIs & Services" → "OAuth consent screen"
   - Choose "External" user type
   - Click "Create"
   - Fill in required fields:
     - **App name**: BulkyBook
     - **User support email**: Your email
     - **Developer contact**: Your email
   - Click "Save and Continue"
   - Skip "Scopes" (click "Save and Continue")
   - Add test users if needed
   - Click "Save and Continue"

5. **Create OAuth 2.0 Credentials**
   - Go to "APIs & Services" → "Credentials"
   - Click "+ CREATE CREDENTIALS" → "OAuth client ID"
   - Choose "Web application"
   - Name it: "BulkyBook Web Client"
   - **Authorized JavaScript origins**:
     ```
     https://localhost:7109
     http://localhost:5272
     ```
   - **Authorized redirect URIs**:
     ```
     https://localhost:7109/signin-google
     http://localhost:5272/signin-google
     ```
   - Click "Create"
   - **SAVE** your Client ID and Client Secret!

### Step 3: Update appsettings.json

Replace the placeholders in your `appsettings.json`:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_ACTUAL_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_ACTUAL_CLIENT_SECRET_HERE"
  }
}
```

**Example:**
```json
"Authentication": {
  "Google": {
    "ClientId": "123456789-abcdefghijklmnop.apps.googleusercontent.com",
    "ClientSecret": "GOCSPX-abc123def456ghi789"
  }
}
```

⚠️ **IMPORTANT**: Never commit your actual credentials to source control!

### Step 4: Configure for Production

When deploying to production:

1. **Add Production URLs** to Google Console:
   - Authorized JavaScript origins: `https://yourdomain.com`
   - Authorized redirect URIs: `https://yourdomain.com/signin-google`

2. **Use Environment Variables** (Recommended):

In production, use environment variables or Azure Key Vault:

```bash
# On Azure App Service
az webapp config appsettings set --name YourAppName --resource-group YourResourceGroup --settings Authentication__Google__ClientId="YOUR_CLIENT_ID" Authentication__Google__ClientSecret="YOUR_CLIENT_SECRET"
```

Or use User Secrets in development:

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"
```

## 🎨 UI Features

### Login Page
- **Modern design** with gradient header
- **Google button** with official Google colors and logo
- **Security notice** about data protection
- **Responsive layout** for all devices

### Register Page
- **Quick signup** with Google at the top
- **Traditional form** below for email registration
- **Modern styling** matching the app design
- **All fields organized** in a clean layout

### Google Button Styling
- Official Google logo (SVG)
- Proper colors and hover effects
- Smooth animations
- Accessible and responsive

## 🔧 How It Works

1. **User clicks "Continue with Google"**
2. Redirected to Google's login page
3. User authorizes the app
4. Google redirects back to your app with authentication token
5. App creates/updates user account
6. User is logged in automatically

## 📝 Testing

1. **Run your application**
   ```bash
   dotnet run
   ```

2. **Navigate to Login page**
   - Go to: `https://localhost:7109/Identity/Account/Login`

3. **Click "Continue with Google"**
   - Should redirect to Google login
   - After login, should return to your app
   - User account created automatically

4. **Check user in database**
   - New user should appear in AspNetUsers table
   - External login data in AspNetUserLogins table

## ⚠️ Troubleshooting

### Issue: "Redirect URI mismatch"
**Solution**: Make sure the redirect URI in Google Console matches exactly:
- Include `https://` or `http://`
- Include correct port number
- Path must be `/signin-google`

### Issue: "Invalid client ID"
**Solution**: 
- Double-check Client ID in appsettings.json
- No extra spaces or quotes
- Restart your application after changing appsettings.json

### Issue: "External authentication error"
**Solution**:
- Check that Google+ API is enabled
- Verify OAuth consent screen is configured
- Check browser console for errors

### Issue: Button not showing
**Solution**:
- Clear browser cache
- Make sure `external-login.css` is loaded
- Check browser DevTools for CSS errors

## 🔒 Security Best Practices

1. **Never commit secrets** to source control
2. **Use User Secrets** in development
3. **Use Azure Key Vault** or environment variables in production
4. **Regularly rotate** Client Secrets
5. **Monitor** OAuth consent screen for suspicious activity
6. **Use HTTPS** in production
7. **Validate redirect URIs** carefully

## 📊 What Gets Stored

When a user logs in with Google:

1. **AspNetUsers** table:
   - Email (from Google)
   - EmailConfirmed = true (Google verifies email)
   - UserName (from Google email)

2. **AspNetUserLogins** table:
   - LoginProvider: "Google"
   - ProviderKey: Google user ID
   - ProviderDisplayName: "Google"

## 🎯 Additional Features (Optional)

### Add More Providers

You can add other providers like Facebook, Microsoft:

```csharp
// In Program.cs
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions => { ... })
    .AddFacebook(facebookOptions => { ... })
    .AddMicrosoftAccount(microsoftOptions => { ... });
```

### Retrieve Google User Info

Access Google user information in your code:

```csharp
var email = User.FindFirst(ClaimTypes.Email)?.Value;
var name = User.FindFirst(ClaimTypes.Name)?.Value;
var googleId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
```

## 📱 Mobile/Desktop Apps

For mobile or desktop apps:
1. Create separate OAuth credentials for each platform
2. Use appropriate redirect URIs for each platform
3. Follow platform-specific OAuth flows

## 🔗 Useful Links

- [Google Cloud Console](https://console.cloud.google.com/)
- [Google OAuth Documentation](https://developers.google.com/identity/protocols/oauth2)
- [ASP.NET Core External Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/)

## ✅ Checklist

Before going live:

- [ ] Google OAuth credentials created
- [ ] Production URLs added to Google Console
- [ ] Secrets stored securely (not in appsettings.json)
- [ ] HTTPS enabled in production
- [ ] OAuth consent screen published
- [ ] Tested login/registration flow
- [ ] Error handling implemented
- [ ] Privacy policy URL added to OAuth consent screen

## 🎉 Done!

Your application now supports Google authentication! Users can:
- ✅ Register with Google (one click)
- ✅ Login with Google (one click)
- ✅ Link existing account to Google
- ✅ Seamless authentication experience

---

**Need Help?**
- Check the troubleshooting section above
- Review ASP.NET Core documentation
- Check Google Cloud Console for errors
- Verify all redirect URIs match exactly

**Version**: 1.0  
**Last Updated**: November 2024  
**Status**: ✅ Ready to Use

