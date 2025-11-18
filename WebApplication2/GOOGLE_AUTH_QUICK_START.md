# ⚡ Google Authentication - Quick Start

## What's Been Added

✅ **Backend**: Google authentication configured in `Program.cs`  
✅ **Config**: `appsettings.json` ready for your Google credentials  
✅ **Login Page**: Modern design with Google button  
✅ **Register Page**: Google signup option at the top  
✅ **Styling**: Professional Google button with logo  

## 🚀 Quick Setup (5 Minutes)

### 1. Install NuGet Package
```powershell
Install-Package Microsoft.AspNetCore.Authentication.Google
```

### 2. Get Google Credentials

1. Go to https://console.cloud.google.com/
2. Create new project: "BulkyBook"
3. Enable "Google+ API"
4. Configure OAuth consent screen
5. Create OAuth credentials (Web application)
6. Add redirect URI: `https://localhost:7109/signin-google`
7. Copy Client ID and Client Secret

### 3. Update appsettings.json

Replace placeholders in `appsettings.json`:

```json
"Authentication": {
  "Google": {
    "ClientId": "PASTE_YOUR_CLIENT_ID_HERE",
    "ClientSecret": "PASTE_YOUR_CLIENT_SECRET_HERE"
  }
}
```

### 4. Run and Test

```bash
dotnet run
```

Go to Login page and click "Continue with Google" button!

## 🎨 What You'll See

### Login Page
- Beautiful gradient header
- Professional Google button with logo
- Security notice
- Modern responsive design

### Register Page
- "Quick sign up with Google" at top
- Traditional form below
- Clean, modern layout
- All fields properly styled

## 📝 Features

Users can now:
- ✅ Register in 1 click with Google
- ✅ Login instantly with Google
- ✅ Auto-verified email
- ✅ No password to remember

## ⚠️ Important Notes

1. **Never commit secrets** - Don't commit real credentials to GitHub
2. **Use User Secrets** for development
3. **Production URLs** - Add your domain to Google Console when deploying
4. **HTTPS required** - Google OAuth requires secure connections

## 📖 Full Documentation

See `GOOGLE_AUTH_SETUP.md` for:
- Detailed setup instructions
- Troubleshooting guide
- Security best practices
- Production deployment

## 🎉 That's It!

Your app now has Google authentication! Users will love the convenience of one-click login.

---

**Need detailed setup help?** Read `GOOGLE_AUTH_SETUP.md`  
**Having issues?** Check the troubleshooting section in the full guide

