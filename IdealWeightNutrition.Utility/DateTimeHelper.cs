using System;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper class to get current time in UAE timezone (Asia/Dubai, UTC+4)
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo _uaeTimeZone;
        private static readonly TimeSpan _uaeOffset = TimeSpan.FromHours(4); // UAE is UTC+4
        
        static DateTimeHelper()
        {
            // Get UAE timezone (Asia/Dubai, UTC+4)
            try
            {
                // Try Windows timezone ID first
                _uaeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");
            }
            catch
            {
                try
                {
                    // Try Linux/Mac timezone ID
                    _uaeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai");
                }
                catch
                {
                    // Final fallback: Create custom timezone for UTC+4 (Gulf Standard Time)
                    _uaeTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "UAE Standard Time",
                        _uaeOffset,
                        "UAE Standard Time",
                        "UAE Standard Time");
                }
            }
        }

        /// <summary>
        /// Gets the current date and time in UAE timezone (UTC+4)
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _uaeTimeZone);

        /// <summary>
        /// Gets the current date in UAE timezone (UTC+4)
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Gets the current UTC time
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Converts a UTC DateTime to UAE timezone
        /// </summary>
        public static DateTime ToUaeTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UTC if unspecified
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), _uaeTimeZone);
            }
            return TimeZoneInfo.ConvertTime(utcDateTime, _uaeTimeZone);
        }

        /// <summary>
        /// Converts a UAE DateTime to UTC
        /// </summary>
        public static DateTime ToUtc(DateTime uaeDateTime)
        {
            if (uaeDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UAE time if unspecified
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(uaeDateTime, DateTimeKind.Unspecified), _uaeTimeZone);
            }
            return TimeZoneInfo.ConvertTime(uaeDateTime, _uaeTimeZone).ToUniversalTime();
        }
    }
}

