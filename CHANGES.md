# 📦 OnlineShop – Change Log

## All 7 Features Implemented

---

### 1. ✅ No Auto-Alert to Bot on Order
**File:** `Controllers/CartController.cs`

The auto Telegram admin notification on `PlaceOrder` has been **removed**. Now:
- Customer must click **"Start Bot & Get Updates"** on the confirmation page
- This opens Telegram with `/start <customerId>` — linking their account
- Admin receives alerts only when updating order status (Feature #7)

---

### 2. ✅ Customer Order History
**Files:** `Controllers/CartController.cs`, `Views/Cart/MyOrders.cshtml`, `Views/Cart/MyOrderDetail.cshtml`

- New `/Cart/MyOrders` page shows all past orders for logged-in customer
- Each order shows: status badge, progress stepper, items, total
- Click "View Details" → `/Cart/MyOrderDetail/{id}` for full detail + map
- "My Orders" link added to navbar (logged-in users only)
- `CustomerId` is now saved to the Order when placed

---

### 3. ✅ Google Map on Confirm Order (Customer + Admin)
**Files:** `Views/Cart/Checkout.cshtml`, `Views/Cart/MyOrderDetail.cshtml`, `Views/Admin/OrderDetail.cshtml`, `Models/Order.cs`

- Checkout page shows an **interactive Google Map** 
- Auto-detects current location via browser geolocation
- Customer can drag pin or click map to change delivery location
- Reverse geocodes the pin to auto-fill the address field
- `LocationLat` / `LocationLng` saved on the Order
- Admin `OrderDetail` shows the saved location with an "Open in Google Maps" link
- Customer `MyOrderDetail` also shows the map

> **Note:** Replace the Google Maps API key `AIzaSyBFw0Qbyq9zTFTd-tUY6dZWTgaQzuU3LKo` in the 3 view files with your own key from [Google Cloud Console](https://console.cloud.google.com/).

---

### 4. ✅ Cash on Delivery – Phnom Penh Only
**Files:** `Views/Cart/Checkout.cshtml`, `Controllers/CartController.cs`

- **Frontend:** COD radio button is disabled automatically if address/city is not Phnom Penh
- **Backend:** Server-side validation rejects COD for non-Phnom Penh addresses
- A warning banner is shown when user selects COD with a non-PP address
- Reverse geocoding from map auto-populates city and triggers this check

---

### 5. ✅ Contact Seller Info
**Files:** `Views/Cart/Checkout.cshtml`, `Views/Cart/Confirmation.cshtml`, `Views/Cart/MyOrderDetail.cshtml`

Contact details added to checkout, confirmation, and order detail pages:
- 📱 **0979534329**
- ✈️ Telegram: **@Pha_Rie** (links to `https://t.me/Pha_Rie`)

---

### 6. ✅ Forgot Password
**Files:** `Controllers/AccountController.cs`, `Views/Account/ForgotPassword.cshtml`, `Views/Account/ResetPassword.cshtml`, `Views/Account/Login.cshtml`, `Models/Customer.cs`

- "Forgot password?" link added below password field on Login page
- `/Account/ForgotPassword` → enter email → generates secure token (1 hour expiry)
- In dev mode: reset link is shown directly on page (no email server needed)
- In production: replace with SMTP email sending
- `/Account/ResetPassword?token=...` → enter new password → saves hashed

---

### 7. ✅ Auto-Alert Customer on Order Status Change
**Files:** `Controllers/AdminController.cs`, `Controllers/CartController.cs`, `Models/Customer.cs`

**How it works:**
1. Customer places order → sees confirmation page with **"Start Bot & Get Updates"** button
2. Button opens Telegram: `t.me/PinkDaisyShopBot?start=<customerId>`
3. Bot webhook at `/Cart/TelegramWebhook` receives `/start <customerId>`, links TelegramChatId to customer
4. When admin changes order status in `Admin/OrderDetail`, customer receives a Telegram message instantly

**Bot Setup Required:**
- Create a bot via [@BotFather](https://t.me/BotFather) on Telegram
- Set webhook: `https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://yoursite.com/Cart/TelegramWebhook`
- Update `appsettings.json` with your BotToken and admin ChatId

---

## 🗄️ Database Migration

Run `MIGRATION.sql` on your SQL Server database:

```sql
ALTER TABLE Customers ADD TelegramChatId NVARCHAR(50) NULL;
ALTER TABLE Customers ADD PasswordResetToken NVARCHAR(200) NULL;
ALTER TABLE Customers ADD PasswordResetExpiry DATETIME2 NULL;
ALTER TABLE Orders ADD LocationLat FLOAT NULL;
ALTER TABLE Orders ADD LocationLng FLOAT NULL;
```

Or use EF Core migrations:
```
dotnet ef migrations add AddTelegramAndLocation
dotnet ef database update
```

## ⚙️ appsettings.json

Make sure your Telegram section is configured:
```json
"Telegram": {
  "BotToken": "YOUR_BOT_TOKEN",
  "ChatId": "YOUR_ADMIN_CHAT_ID"
}
```
