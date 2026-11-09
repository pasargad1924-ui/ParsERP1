using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsERP.Core.Services;

/// <summary>
/// تولید و اعتبارسنجی شماره سریال ۱۰ رقمی مطابق الگوریتم مشابه کد ملی ایران
/// ساختار: TT YY NNNNN C
/// TT   = کد نوع محصول (۲ رقم)
/// YY   = دو رقم آخر سال شمسی تولید
/// NNNNN = شماره سری اختصاصی (۵ رقم)
/// C    = رقم کنترل (Check Digit)
/// </summary>
public static class SerialService
{
    /// <summary>
    /// محاسبه رقم کنترل (الگوریتم مشابه کد ملی ایران)
    /// </summary>
    public static int CalculateCheckDigit(string nineDigits)
    {
        if (nineDigits == null || nineDigits.Length != 9 || !nineDigits.All(char.IsDigit))
            throw new ArgumentException("باید دقیقاً ۹ رقم باشد.");

        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            int digit = nineDigits[i] - '0';
            int weight = 10 - i; // 10,9,8,...,2
            sum += digit * weight;
        }

        int remainder = sum % 11;
        return remainder < 2 ? remainder : 11 - remainder;
    }

    /// <summary>
    /// تولید یک شماره سریال کامل ۱۰ رقمی
    /// </summary>
    public static string GenerateSerial(int productTypeCode, int solarYearTwoDigits, int serialNumber)
    {
        if (productTypeCode < 0 || productTypeCode > 99)
            throw new ArgumentOutOfRangeException(nameof(productTypeCode), "کد نوع محصول باید بین ۰۰ تا ۹۹ باشد.");
        if (solarYearTwoDigits < 0 || solarYearTwoDigits > 99)
            throw new ArgumentOutOfRangeException(nameof(solarYearTwoDigits), "سال باید دو رقم باشد.");
        if (serialNumber < 0 || serialNumber > 99999)
            throw new ArgumentOutOfRangeException(nameof(serialNumber), "شماره سری باید بین ۰ تا ۹۹۹۹۹ باشد.");

        string nine = $"{productTypeCode:D2}{solarYearTwoDigits:D2}{serialNumber:D5}";
        int check = CalculateCheckDigit(nine);
        return nine + check.ToString();
    }

    /// <summary>
    /// تولید دسته‌ای سریال با شروع از یک شماره مشخص
    /// </summary>
    public static List<string> GenerateBatch(int productTypeCode, int solarYearTwoDigits, int startSerial, int count)
    {
        if (count <= 0) throw new ArgumentException("تعداد باید بزرگ‌تر از صفر باشد.");
        if (startSerial + count - 1 > 99999)
            throw new ArgumentException("محدوده سریال از ۹۹۹۹۹ تجاوز می‌کند.");

        var list = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(GenerateSerial(productTypeCode, solarYearTwoDigits, startSerial + i));
        }
        return list;
    }

    /// <summary>
    /// اعتبارسنجی کامل یک سریال ۱۰ رقمی
    /// </summary>
    public static bool Validate(string serial)
    {
        if (string.IsNullOrWhiteSpace(serial) || serial.Length != 10 || !serial.All(char.IsDigit))
            return false;

        string nine = serial.Substring(0, 9);
        int expectedCheck = CalculateCheckDigit(nine);
        int actualCheck = serial[9] - '0';
        return expectedCheck == actualCheck;
    }

    /// <summary>
    /// تجزیه سریال به اجزای آن
    /// </summary>
    public static (int ProductType, int Year, int Number, int CheckDigit)? Parse(string serial)
    {
        if (!Validate(serial)) return null;

        return (
            int.Parse(serial.Substring(0, 2)),
            int.Parse(serial.Substring(2, 2)),
            int.Parse(serial.Substring(4, 5)),
            int.Parse(serial.Substring(9, 1))
        );
    }
}
