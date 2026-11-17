using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsERP.Core.Services;

/// <summary>
/// سریال ۱۰ رقمی: TT YY NNNNN C  (رقم کنترل مشابه کد ملی)
/// </summary>
public static class SerialService
{
    public static int CalculateCheckDigit(string nineDigits)
    {
        if (nineDigits == null || nineDigits.Length != 9 || !nineDigits.All(char.IsDigit))
            throw new ArgumentException("باید دقیقاً ۹ رقم باشد.");

        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (nineDigits[i] - '0') * (10 - i);

        int remainder = sum % 11;
        return remainder < 2 ? remainder : 11 - remainder;
    }

    public static string GenerateSerial(int productTypeCode, int solarYearTwoDigits, int serialNumber)
    {
        if (productTypeCode < 0 || productTypeCode > 99)
            throw new ArgumentOutOfRangeException(nameof(productTypeCode));
        if (solarYearTwoDigits < 0 || solarYearTwoDigits > 99)
            throw new ArgumentOutOfRangeException(nameof(solarYearTwoDigits));
        if (serialNumber < 0 || serialNumber > 99999)
            throw new ArgumentOutOfRangeException(nameof(serialNumber));

        string nine = $"{productTypeCode:D2}{solarYearTwoDigits:D2}{serialNumber:D5}";
        return nine + CalculateCheckDigit(nine);
    }

    /// <summary>
    /// تولید تعداد مشخص سریال یکتا — شماره‌های ۵رقمی تصادفی که در usedNumbers نباشند
    /// </summary>
    public static List<string> GenerateUniqueRandom(
        int productTypeCode,
        int solarYearTwoDigits,
        int count,
        HashSet<int> usedNumbers,
        int maxAttemptsPerSerial = 5000)
    {
        if (count <= 0) throw new ArgumentException("تعداد باید بزرگ‌تر از صفر باشد.");
        var result = new List<string>(count);
        var rng = new Random();
        var newlyUsed = new HashSet<int>(usedNumbers);

        for (int i = 0; i < count; i++)
        {
            int attempts = 0;
            int num;
            do
            {
                num = rng.Next(0, 100000); // 00000..99999
                attempts++;
                if (attempts > maxAttemptsPerSerial)
                    throw new InvalidOperationException(
                        "فضای کافی برای تولید سریال یکتا در این نوع‌محصول/سال وجود ندارد.");
            } while (newlyUsed.Contains(num));

            newlyUsed.Add(num);
            result.Add(GenerateSerial(productTypeCode, solarYearTwoDigits, num));
        }
        return result;
    }

    public static bool Validate(string serial)
    {
        if (string.IsNullOrWhiteSpace(serial) || serial.Length != 10 || !serial.All(char.IsDigit))
            return false;
        string nine = serial.Substring(0, 9);
        return CalculateCheckDigit(nine) == (serial[9] - '0');
    }

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
