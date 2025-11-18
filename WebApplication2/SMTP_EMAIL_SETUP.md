# 📧 SMTP Email Configuration Guide

## Overview
Your application now uses standard SMTP for sending emails. This works with any email provider (Gmail, Outlook, Yahoo, etc.).

## ✅ What's Implemented

- ✅ **EmailSender.cs** updated to use SMTP
- ✅ **Configuration** in appsettings.json
- ✅ **Error handling** with detailed logging
- ✅ **Async email sending**
- ✅ **HTML email support**

## 🚀 Setup Options

### Option 1: Gmail (Recommended for Testing)

#### Step 1: Enable 2-Factor Authentication
1. Go to your Google Account: https://myaccount.google.com/
2. Security → 2-Step Verification
3. Turn it ON

#### Step 2: Create App Password
1. Go to: https://myaccount.google.com/apppasswords
2. Select app: "Mail"
3. Select device: "Other (Custom name)" → Type "BulkyBook"
4. Click "Generate"
5. **Copy the 16-character password** (e.g., "abcd efgh ijkl mnop")

#### Step 3: Update appsettings.json
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "your-email@gmail.com",
  "Password": "your-app-password-here",
  "FromEmail": "your-email@gmail.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

**Example:**
```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "msobhy@gmail.com",
  "Password": "abcd efgh ijkl mnop",
  "FromEmail": "msobhy@gmail.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

### Option 2: Microsoft Outlook/Office 365

```json
"Smtp": {
  "Host": "smtp.office365.com",
  "Port": "587",
  "Username": "your-email@outlook.com",
  "Password": "your-password",
  "FromEmail": "your-email@outlook.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

### Option 3: Yahoo Mail

```json
"Smtp": {
  "Host": "smtp.mail.yahoo.com",
  "Port": "587",
  "Username": "your-email@yahoo.com",
  "Password": "your-app-password",
  "FromEmail": "your-email@yahoo.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

### Option 4: Custom SMTP Server

```json
"Smtp": {
  "Host": "mail.yourdomain.com",
  "Port": "587",
  "Username": "noreply@yourdomain.com",
  "Password": "your-password",
  "FromEmail": "noreply@yourdomain.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

### Option 5: Mailtrap (For Testing)

**Perfect for development/testing** - catches all emails without sending them:

1. Sign up at: https://mailtrap.io/
2. Get your credentials from inbox settings
3. Update config:

```json
"Smtp": {
  "Host": "smtp.mailtrap.io",
  "Port": "2525",
  "Username": "your-mailtrap-username",
  "Password": "your-mailtrap-password",
  "FromEmail": "test@bulkybook.com",
  "FromName": "BulkyBook Store",
  "EnableSsl": "true"
}
```

## 🔧 Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| **Host** | SMTP server address | smtp.gmail.com |
| **Port** | SMTP port (usually 587 or 465) | 587 |
| **Username** | Your email login | you@gmail.com |
| **Password** | Your email password or app password | abcd1234 |
| **FromEmail** | Sender email address | noreply@bulkybook.com |
| **FromName** | Sender display name | BulkyBook Store |
| **EnableSsl** | Use SSL/TLS encryption | true |

## 🔐 Security Best Practices

### 1. Use App Passwords (Gmail)
❌ **DON'T** use your regular Gmail password  
✅ **DO** create an App Password (see setup above)

### 2. Use Environment Variables (Production)
❌ **DON'T** commit passwords to Git  
✅ **DO** use environment variables or Azure Key Vault

**For Production:**
```bash
# Set environment variables
$env:Smtp__Username = "your-email@gmail.com"
$env:Smtp__Password = "your-app-password"
```

Or in Azure App Service:
- Go to Configuration
- Add Application Settings:
  - `Smtp__Username` = your email
  - `Smtp__Password` = your app password

### 3. Use User Secrets (Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "Smtp:Username" "your-email@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-app-password"
```

## 🧪 Testing

### Test Email Sending:

1. **Update appsettings.json** with your SMTP credentials
2. **Restart your application**
3. **Place a test order**
4. **Check email inboxes** (admin and customer)

### Quick Test Code:
You can test email sending directly in your controller:

```csharp
await _emailSender.SendEmailAsync(
    "test@example.com",
    "Test Email",
    "<h1>Hello</h1><p>This is a test email</p>"
);
```

## 📧 Email Templates

Your notification system sends 2 types of emails:

### 1. Admin Order Notification
- **Subject**: "New Order #{OrderId} - BulkyBook"
- **Content**:
  - Order number and date
  - Customer information
  - Order items with prices
  - Total amount
  - Payment status
  - View Order Details button

### 2. Customer Order Confirmation
- **Subject**: "Order Confirmation #{OrderId} - BulkyBook"
- **Content**:
  - Order confirmation message
  - Order details
  - Shipping address
  - Estimated delivery
  - Track Order button
  - Continue Shopping button

## ⚠️ Troubleshooting

### Error: "Authentication failed"
**Solution**: 
- For Gmail: Create an App Password
- Check username and password are correct
- Ensure 2FA is enabled (Gmail requirement)

### Error: "Mailbox unavailable"
**Solution**:
- Check FromEmail is valid
- Verify email format is correct
- Check SMTP username matches FromEmail

### Error: "Connection timeout"
**Solution**:
- Check Host and Port are correct
- Verify firewall isn't blocking port 587
- Try port 465 with SSL

### Error: "Could not connect to mail server"
**Solution**:
- Check internet connection
- Verify SMTP host is correct
- Try pinging the SMTP server

### Emails Go to Spam
**Solution**:
- Use a verified sender email
- Add SPF/DKIM records to your domain
- Use a professional FromName
- Don't use "noreply" if possible

## 📊 Common SMTP Providers

### Gmail
- **Host**: smtp.gmail.com
- **Port**: 587 (TLS) or 465 (SSL)
- **SSL**: true
- **Requires**: App Password (2FA enabled)

### Outlook/Office 365
- **Host**: smtp.office365.com
- **Port**: 587
- **SSL**: true
- **Requires**: Account password

### Yahoo
- **Host**: smtp.mail.yahoo.com
- **Port**: 587 or 465
- **SSL**: true
- **Requires**: App Password

### GoDaddy
- **Host**: smtpout.secureserver.net
- **Port**: 465 or 587
- **SSL**: true

### Hostinger
- **Host**: smtp.hostinger.com
- **Port**: 587
- **SSL**: true

## 🎯 What You Get

Your emails now include:
- ✅ **HTML content** with beautiful styling
- ✅ **Responsive design** for mobile/desktop
- ✅ **Inline CSS** for email client compatibility
- ✅ **Professional headers** with gradients
- ✅ **Clear call-to-action** buttons
- ✅ **Complete order information**
- ✅ **Company branding**

## 🔄 Email Flow

```
Order Placed
    ↓
NotificationService.SendOrderNotificationToAdmins()
    ↓
    ├─→ Get all admin users from database
    ├─→ For each admin:
    │   ├─→ Generate beautiful HTML email
    │   ├─→ Send via SMTP (EmailSender.SendEmailAsync)
    │   └─→ Log in database
    │
NotificationService.SendOrderConfirmationToCustomer()
    ↓
    ├─→ Get customer details
    ├─→ Get order items
    ├─→ Generate beautiful HTML email
    ├─→ Send via SMTP (EmailSender.SendEmailAsync)
    └─→ Log in database
```

## 💡 Quick Setup Example (Gmail)

**appsettings.json:**
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "msobhy123@gmail.com",
    "Password": "abcd efgh ijkl mnop",
    "FromEmail": "msobhy123@gmail.com",
    "FromName": "BulkyBook Store",
    "EnableSsl": "true"
  }
}
```

**Then:**
1. Restart app
2. Place order
3. Check emails! 📧

## 🎨 Email Preview

Your emails will look like this:

**Admin Email:**
```
┌───────────────────────────────────┐
│   🎉 NEW ORDER RECEIVED!          │ ← Purple gradient
│      Order #123                   │
├───────────────────────────────────┤
│ Customer: John Doe                │
│ Phone: +1-555-1234                │
│ Address: 123 Main St...           │
│                                   │
│ Items:                            │
│ • Product 1  x2  $40   $80       │
│ • Product 2  x1  $440  $440      │
│                                   │
│ Total: $520.00                    │
│                                   │
│    [View Order Details]           │
└───────────────────────────────────┘
```

**Customer Email:**
```
┌───────────────────────────────────┐
│   ✅ ORDER CONFIRMED!              │ ← Purple gradient  
│   Thank you, John!                │
├───────────────────────────────────┤
│ ✓ Order successfully placed       │ ← Green box
│                                   │
│ Order #123                        │
│ Date: Nov 16, 2024               │
│ Delivery: Nov 23-30, 2024        │
│                                   │
│ Your Items:                       │
│ • Product 1  x2  $120.00         │
│ • Product 2  x1  $400.00         │
│                                   │
│ Total: $520.00                    │
│                                   │
│ [Track Order] [Continue Shopping] │
└───────────────────────────────────┘
```

## 🎉 Benefits of SMTP

✅ **Works with any email provider** (Gmail, Outlook, Yahoo, custom)  
✅ **No third-party service** dependencies  
✅ **No API limits** (depends on your email provider)  
✅ **Standard protocol** - widely supported  
✅ **Easy to configure** - just add credentials  
✅ **Free with most email accounts**  

## 📝 Testing Checklist

- [ ] Update appsettings.json with SMTP credentials
- [ ] For Gmail: Create App Password with 2FA
- [ ] Restart application
- [ ] Place a test order
- [ ] Check admin email inbox
- [ ] Check customer email inbox
- [ ] Verify emails look professional
- [ ] Check spam folder if emails not received

## 🔒 Security Tips

1. **Never commit passwords** to source control
2. **Use environment variables** in production
3. **Use App Passwords** for Gmail (not your regular password)
4. **Enable SSL/TLS** for secure transmission
5. **Rotate passwords** regularly
6. **Monitor** for suspicious activity

## 💡 Provider Recommendations

### For Development/Testing:
- **Mailtrap** - Catches all emails, perfect for testing
- **Gmail** - Easy to setup, free

### For Production:
- **Office 365** - Professional, reliable
- **Gmail Workspace** - Professional, good deliverability
- **Custom Domain Email** - Most professional
- **AWS SES** - Scalable, pay-as-you-go

## 🎊 Complete!

Your email system is now configured to use **standard SMTP**!

Just update the credentials in `appsettings.json` and you're ready to send beautiful emails! 📧

---

**Need help with a specific provider?** See the examples above.  
**Having issues?** Check the troubleshooting section.  
**Ready to test?** Just place an order! 🚀

