using System;

namespace RENP;

public static class Extensions
{
    public static string FormatNumber(this double number, int significantDigits, double maxInvertValue = 0, bool forceDecimals = false)
    {
        if (double.IsNaN(number))
        {
            return "NaN";
        }

        if (number == 0)
        {
            return "0";
        }

        if (Math.Abs(number) <= 1e-10)
        {
            return "~0";
        }

        if (Math.Abs(number) < maxInvertValue)
        {
            return $"1/{Math.Round((decimal)(1 / number), 1):#.#}";
        }

        if (double.IsInfinity(number) || number > (double)decimal.MaxValue || number < (double)decimal.MinValue)
        {
            return number > 0 ? "Inf" : "-Inf";
        }

        try
        {
            return Math.Round((decimal)number, significantDigits).ToString($"#,##0.{new string(forceDecimals ? '0' : '#', significantDigits)}");
        }
        catch (OverflowException)
        {
            return number > 0 ? "Inf" : "-Inf";
        }
    }

    public static bool IsChanceable(this object item)
    {
        return true;
    }
}