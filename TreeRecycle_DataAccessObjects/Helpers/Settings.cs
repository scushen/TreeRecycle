using System;
using System.Configuration;
using System.Globalization;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Typed access to web.config appSettings. Every key is listed in README.md.
    /// </summary>
    public static class Settings
    {
        public static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key] ?? string.Empty;
        }

        public static string SiteUrl { get { return Get("SiteUrl").TrimEnd('/'); } }
        public static string SiteName { get { return string.IsNullOrEmpty(Get("SiteName")) ? "TreeRecycle" : Get("SiteName"); } }

        public static string StripeSecretKey { get { return Get("StripeSecretKey"); } }
        public static string StripeConnectWebhookSecret { get { return Get("StripeConnectWebhookSecret"); } }

        // Booking fees are per currency: see CountryInfo.BookingFee (PlatformFeePerTree_GBP etc.).

        public static string GoogleGeocodingApiKey { get { return Get("GoogleGeocodingApiKey"); } }

        public static string PostmarkServerToken { get { return Get("PostmarkServerToken"); } }
        public static string EmailFromAddress { get { return Get("EmailFromAddress"); } }
        public static string AdminNotifyEmail { get { return Get("AdminNotifyEmail"); } }

        /// <summary>The platform admin works in Irish time; club pages use the club's own time zone.</summary>
        public const string AdminTimeZone = "GMT Standard Time";

        /// <summary>Stored dates are UTC; convert to a Windows time zone id for display. Unknown ids fall back to Irish time.</summary>
        public static DateTime ToLocal(DateTime utc, string timeZoneId)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone(timeZoneId));
        }

        public static DateTime TodayIn(string timeZoneId)
        {
            return ToLocal(DateTime.UtcNow, timeZoneId).Date;
        }

        private static TimeZoneInfo Zone(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrEmpty(id) ? AdminTimeZone : id);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(AdminTimeZone);
            }
        }
    }
}
