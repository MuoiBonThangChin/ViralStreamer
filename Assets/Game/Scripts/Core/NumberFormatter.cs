using System;
using System.Globalization;

namespace StreamStar
{
    public static class NumberFormatter
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T" };
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>1234 -> 1.23K, 1.5e15 -> 1.50aa</summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "MAX";
            if (value < 0) return "-" + Format(-value);
            if (value < 1000) return value < 10 && value != Math.Floor(value)
                ? value.ToString("0.#", Inv)
                : Math.Floor(value).ToString("0", Inv);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double scaled = value / Math.Pow(1000, tier);
            if (scaled >= 999.995) { scaled /= 1000; tier++; }
            string suffix = tier < Suffixes.Length ? Suffixes[tier] : LetterSuffix(tier - Suffixes.Length);
            string format = scaled >= 100 ? "0" : scaled >= 10 ? "0.0" : "0.00";
            return scaled.ToString(format, Inv) + suffix;
        }

        static string LetterSuffix(int index)
        {
            char first = (char)('a' + (index / 26) % 26);
            char second = (char)('a' + index % 26);
            return new string(new[] { first, second });
        }

        public static string Duration(double seconds)
        {
            int s = (int)Math.Max(0, Math.Ceiling(seconds));
            if (s >= 3600) return (s / 3600) + "h " + ((s % 3600) / 60).ToString("00", Inv) + "m";
            if (s >= 60) return (s / 60) + ":" + (s % 60).ToString("00", Inv);
            return s + "s";
        }
    }
}
