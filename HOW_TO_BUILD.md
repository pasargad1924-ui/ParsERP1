# راهنمای ساخت و اجرای پارس‌ئی‌آرپی روی ویندوز

چون محیط توسعه فعلی لینوکس است، فایل EXE از اینجا تولید نمی‌شود.  
با دنبال کردن این مراحل روی یک سیستم ویندوز می‌توانید در کمتر از ۱۵ دقیقه برنامه را بسازید و اجرا کنید.

## پیش‌نیاز (همه رایگان)

1. **Visual Studio 2022 Community** (رایگان)  
   دانلود از: https://visualstudio.microsoft.com/downloads/  
   هنگام نصب فقط این مورد را تیک بزنید:  
   **.NET desktop development**

2. یا فقط **.NET 8 SDK** (اگر ویژوال استودیو نمی‌خواهید):  
   https://dotnet.microsoft.com/download/dotnet/8.0

## روش ۱ – با Visual Studio (پیشنهادی)

1. پوشه `ParsERP` را از حالت فشرده خارج کنید.
2. فایل `ParsERP/ParsERP.csproj` را با Visual Studio باز کنید.
3. اگر پیغام Restore یا SDK داد، OK کنید.
4. از منوی بالا **Build → Build Solution** را بزنید (یا Ctrl+Shift+B).
5. برای اجرا دکمه سبز **Start** را بزنید (یا F5).

### ورود آزمایشی
- نام کاربری: `admin`
- رمز عبور: `1234`

## روش ۲ – فقط با خط فرمان

```powershell
cd مسیر\ParsERP\ParsERP
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

فایل اجرایی در مسیر زیر ساخته می‌شود:
```
ParsERP\bin\Release\net8.0-windows\ParsERP.exe
```

## فونت فارسی پیشنهادی

بهترین فونت متن‌باز و خوانا برای فارسی در حال حاضر **Vazirmatn** است.  
دانلود: https://github.com/rastikerdar/vazirmatn  

بعد از دانلود، فایل‌های TTF را در ویندوز نصب کنید (راست‌کلیک → Install).  
در کد از قبل `Vazirmatn` به عنوان اولویت اول تنظیم شده است.

## ساخت نصب‌کننده (Installer) بعداً

پس از اینکه برنامه پایدار شد، می‌توانید با ابزارهای زیر نصب‌کننده بسازید:
- Inno Setup (رایگان و عالی)
- Advanced Installer
- یا MSIX

## نکته مهم امنیتی نسخه فعلی

ورود فعلی فقط آزمایشی است (`admin / 1234`).  
در نسخه‌های بعدی:
- رمز با الگوریتم Argon2 هش می‌شود
- دیتابیس رمزنگاری می‌شود
- سطح دسترسی نقش‌محور کامل فعال می‌گردد
