using System;
using System.Globalization;

namespace IdealWeightNutrition.Utility
{
    public static class CurrencyHelper
    {
        /// <summary>
        /// Gets the currency symbol based on culture
        /// </summary>
        /// <param name="culture">Culture name (e.g., "en", "ar")</param>
        /// <returns>Currency symbol (AED for en, د.إ for ar)</returns>
        public static string GetCurrencySymbol(string culture = null)
        {
            if (string.IsNullOrEmpty(culture))
            {
                culture = CultureInfo.CurrentCulture.Name;
            }

            return culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "د.إ" : "AED";
        }

        /// <summary>
        /// Formats a decimal value with currency symbol
        /// </summary>
        /// <param name="value">The decimal value to format</param>
        /// <param name="culture">Culture name (e.g., "en", "ar")</param>
        /// <returns>Formatted string with currency symbol</returns>
        public static string FormatCurrency(decimal value, string culture = null)
        {
            var symbol = GetCurrencySymbol(culture);
            return $"{symbol} {value:N2}";
        }

        /// <summary>
        /// Formats a double value with currency symbol
        /// </summary>
        /// <param name="value">The double value to format</param>
        /// <param name="culture">Culture name (e.g., "en", "ar")</param>
        /// <returns>Formatted string with currency symbol</returns>
        public static string FormatCurrency(double value, string culture = null)
        {
            var symbol = GetCurrencySymbol(culture);
            return $"{symbol} {value:N2}";
        }
    }
}

