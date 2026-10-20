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

        /// <summary>Our technology fee per tree, in euro. Goes to the platform Stripe account as an application fee.</summary>
        public static decimal PlatformFeePerTree
        {
            get
            {
                decimal fee;
                return decimal.TryParse(Get("PlatformFeePerTree"), NumberStyles.Number, CultureInfo.InvariantCulture, out fee) ? fee : 2.00m;
            }
        }

        public static string GoogleGeocodingApiKey { get { return Get("GoogleGeocodingApiKey"); } }

        public static string PostmarkServerToken { get { return Get("PostmarkServerToken"); } }
        public static string EmailFromAddress { get { return Get("EmailFromAddress"); } }
        public static string AdminNotifyEmail { get { return Get("AdminNotifyEmail"); } }

        /// <summary>Ireland. Stored dates are UTC; everything shown to people is Irish time.</summary>
        public static readonly TimeZoneInfo IrishTime = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        public static DateTime ToIrish(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IrishTime);
        }

        public static DateTime TodayIrish { get { return ToIrish(DateTime.UtcNow).Date; } }

        public static string Money(decimal amount)
        {
            return "€" + amount.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
