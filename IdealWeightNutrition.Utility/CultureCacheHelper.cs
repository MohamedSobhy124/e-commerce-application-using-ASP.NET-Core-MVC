using System.Globalization;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper utility for culture-aware caching.
    /// Use this when caching partial views, View Components, or any localized content
    /// to ensure cache keys include the current culture.
    /// </summary>
    public static class CultureCacheHelper
    {
        /// <summary>
        /// Gets a culture-aware cache key by appending the current UI culture to the base key.
        /// This ensures that cached content is separated by language (e.g., "Header_en", "Header_ar").
        /// </summary>
        /// <param name="baseKey">The base cache key (e.g., "Header", "Footer", "Menu")</param>
        /// <returns>A culture-aware cache key (e.g., "Header_en" or "Header_ar")</returns>
        /// <example>
        /// // ❌ Wrong - cache key without culture
        /// var html = await RenderPartialViewToString("_Header");
        /// _cache.Set("Header", html);
        /// 
        /// // ✅ Correct - cache key with culture
        /// var html = await RenderPartialViewToString("_Header");
        /// var cacheKey = CultureCacheHelper.GetCultureKey("Header");
        /// _cache.Set(cacheKey, html);
        /// </example>
        public static string GetCultureKey(string baseKey)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            return $"{baseKey}_{culture}";
        }

        /// <summary>
        /// Gets a culture-aware cache key with additional parameters.
        /// Useful when you need to cache by both culture and other parameters (e.g., user ID, category ID).
        /// </summary>
        /// <param name="baseKey">The base cache key</param>
        /// <param name="additionalParams">Additional parameters to include in the cache key</param>
        /// <returns>A culture-aware cache key with parameters</returns>
        /// <example>
        /// // Cache by culture and user ID
        /// var cacheKey = CultureCacheHelper.GetCultureKey("UserMenu", userId);
        /// // Result: "UserMenu_en_123" or "UserMenu_ar_123"
        /// </example>
        public static string GetCultureKey(string baseKey, params object[] additionalParams)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var paramString = additionalParams != null && additionalParams.Length > 0
                ? "_" + string.Join("_", additionalParams)
                : string.Empty;
            return $"{baseKey}_{culture}{paramString}";
        }
    }
}

