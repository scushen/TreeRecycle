using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace TreeRecycle.DataAccessObjects
{
    public class GeoPoint
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string FormattedAddress { get; set; }
        /// <summary>Google's location_type: ROOFTOP, RANGE_INTERPOLATED, GEOMETRIC_CENTER or APPROXIMATE.</summary>
        public string Precision { get; set; }
        /// <summary>County / state / region, matched to the country's list where it has one.</summary>
        public string Region { get; set; }
        public string Town { get; set; }
    }

    /// <summary>
    /// Google couldn't answer (bad or restricted key, quota, network). Not the same as "no such address": the
    /// address may be fine, so callers say "try again" instead of "check your postcode".
    /// </summary>
    public class GeoLookupException : Exception
    {
        public GeoLookupException(string message) : base(message) { }
        public GeoLookupException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Distances and Google geocoding for every country in <see cref="Countries"/>.</summary>
    public static class Geo
    {
        /// <summary>Great-circle distance in km. Road distance is typically 1.2-1.4x this.</summary>
        public static double DistanceKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            const double R = 6371.0;
            double dLat = ToRad((double)(lat2 - lat1));
            double dLng = ToRad((double)(lng2 - lng1));
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRad((double)lat1)) * Math.Cos(ToRad((double)lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) { return deg * Math.PI / 180.0; }

        // Google lookups are billed per call, and one booking typically looks the same postcode up twice
        // (home-page search, then order submit). Found points are kept 7 days (Google's terms allow up to 30);
        // "no such address" for an hour so typos aren't re-billed; quota/key/network errors are never cached.
        private static readonly MemoryCache GeoCache = new MemoryCache("TreeRecycleGeocode");
        private static readonly TimeSpan FoundLifetime = TimeSpan.FromDays(7);
        private static readonly TimeSpan NotFoundLifetime = TimeSpan.FromHours(1);
        private static readonly object NotFound = new object();
        private static long _cacheHits;
        private static long _googleCalls;
        private static long _googleFailures;
        private static string _lastProblem;
        private static DateTime? _lastProblemUtc;

        /// <summary>Lookups answered from the cache since the app started (shown on health.ashx).</summary>
        public static long GeocodeCacheHits { get { return Interlocked.Read(ref _cacheHits); } }

        /// <summary>Billable Google Geocoding calls since the app started (shown on health.ashx).</summary>
        public static long GeocodeGoogleCalls { get { return Interlocked.Read(ref _googleCalls); } }

        /// <summary>Lookups Google refused or couldn't answer since the app started (shown on health.ashx).</summary>
        public static long GeocodeFailures { get { return Interlocked.Read(ref _googleFailures); } }

        /// <summary>The most recent refusal, with Google's own explanation, or null if there hasn't been one.</summary>
        public static string LastProblem { get { return _lastProblem; } }

        public static DateTime? LastProblemUtc { get { return _lastProblemUtc; } }

        /// <summary>
        /// Geocodes a postcode or a full address inside one country, from the cache when it can, otherwise with the
        /// Google Geocoding API. Returns null when Google says there's no such place. Throws <see cref="GeoLookupException"/> when Google can't
        /// answer (key, quota or network problem).
        /// </summary>
        public static GeoPoint Geocode(string postcodeOrAddress, string countryCode)
        {
            if (string.IsNullOrWhiteSpace(postcodeOrAddress))
                return null;
            CountryInfo country = Countries.Get(countryCode);

            // "a65f4e2", "A65 F4E2" and " a65  f4e2 " share one cache entry; the country is part of the key.
            string postcode = country.NormalisePostcode(postcodeOrAddress);
            string query = postcode != string.Empty ? postcode : Regex.Replace(postcodeOrAddress.Trim(), @"\s+", " ");
            string cacheKey = country.Code + "|" + query.ToUpperInvariant();

            object cached = GeoCache.Get(cacheKey);
            if (cached != null)
            {
                Interlocked.Increment(ref _cacheHits);
                return cached == NotFound ? null : Copy((GeoPoint)cached);
            }

            // Refusals throw before reaching here, so only real answers are cached.
            GeoPoint point = GeocodeFromGoogle(query, country);
            if (point != null)
                GeoCache.Set(cacheKey, Copy(point), DateTimeOffset.UtcNow.Add(FoundLifetime));
            else
                GeoCache.Set(cacheKey, NotFound, DateTimeOffset.UtcNow.Add(NotFoundLifetime));
            return point;
        }

        /// <summary>
        /// Where an order should be plotted. An Eircode pinpoints the house, so it's used alone (and is usually
        /// already cached from the search). Other countries' postcodes cover a whole street or district, so the full
        /// address is geocoded, falling back to the postcode if Google can't place the address.
        /// </summary>
        public static GeoPoint GeocodeAddress(string address, string town, string postcode, string countryCode)
        {
            CountryInfo country = Countries.Get(countryCode);
            if (country.PostcodeIsExact)
                return Geocode(postcode, country.Code);
            string full = string.Join(", ", new[] { address, town, postcode }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return Geocode(full, country.Code) ?? Geocode(postcode, country.Code);
        }

        private static GeoPoint Copy(GeoPoint p)
        {
            return new GeoPoint
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                FormattedAddress = p.FormattedAddress,
                Precision = p.Precision,
                Region = p.Region,
                Town = p.Town,
            };
        }

        /// <summary>
        /// One billable Google call. Null for ZERO_RESULTS; throws for OVER_QUERY_LIMIT, REQUEST_DENIED, INVALID_REQUEST,
        /// UNKNOWN_ERROR and network failures.
        /// </summary>
        private static GeoPoint GeocodeFromGoogle(string query, CountryInfo country)
        {
            string key = Settings.GoogleGeocodingApiKey;
            if (string.IsNullOrEmpty(key) || key.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                throw Problem("GoogleGeocodingApiKey is not set.", null);

            string cc = country.Code.ToLowerInvariant();
            string url = "https://maps.googleapis.com/maps/api/geocode/json?region=" + (cc == "gb" ? "uk" : cc)
                         + "&components=country:" + country.Code
                         + "&address=" + Uri.EscapeDataString(query) + "&key=" + Uri.EscapeDataString(key);
            string json;
            Interlocked.Increment(ref _googleCalls);
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    json = wc.DownloadString(url);
                }
            }
            catch (WebException ex)
            {
                throw Problem("Couldn't reach Google: " + ex.Message, ex);
            }

            JObject root = JObject.Parse(json);
            string status = (string)root["status"];
            if (status == "ZERO_RESULTS")
                return null;
            if (status != "OK")
            {
                string detail = (string)root["error_message"];
                throw Problem("Google said " + status + (string.IsNullOrEmpty(detail) ? "" : ": " + detail), null);
            }

            JToken first = root["results"][0];
            var point = new GeoPoint
            {
                Latitude = Math.Round(first["geometry"]["location"]["lat"].Value<decimal>(), 6),
                Longitude = Math.Round(first["geometry"]["location"]["lng"].Value<decimal>(), 6),
                Precision = (string)first["geometry"]["location_type"] ?? string.Empty,
                FormattedAddress = (string)first["formatted_address"] ?? string.Empty,
                Region = string.Empty,
                Town = string.Empty,
            };

            // Ireland and the UK name counties at level 2 (level 1 is "England" / "Leinster"-style); the others at level 1.
            string preferred = country.Code == "IE" || country.Code == "GB" ? "administrative_area_level_2" : "administrative_area_level_1";
            string fallback = preferred == "administrative_area_level_2" ? "administrative_area_level_1" : "administrative_area_level_2";
            foreach (string level in new[] { preferred, fallback })
            {
                foreach (JToken comp in first["address_components"])
                {
                    if (point.Region != string.Empty || !comp["types"].Values<string>().Contains(level))
                        continue;
                    point.Region = country.MatchRegion((string)comp["long_name"]);
                    if (point.Region == string.Empty)
                        point.Region = country.MatchRegion((string)comp["short_name"]);
                }
            }
            foreach (JToken comp in first["address_components"])
            {
                var types = comp["types"].Values<string>().ToList();
                if ((types.Contains("locality") || types.Contains("postal_town")) && point.Town == string.Empty)
                    point.Town = (string)comp["long_name"] ?? string.Empty;
            }
            return point;
        }

        private static GeoLookupException Problem(string message, Exception inner)
        {
            Interlocked.Increment(ref _googleFailures);
            _lastProblem = message;
            _lastProblemUtc = DateTime.UtcNow;
            System.Diagnostics.Trace.TraceWarning("Geocode problem: " + message);
            return new GeoLookupException(message, inner);
        }

        public static string GoogleMapsLink(decimal lat, decimal lng)
        {
            return "https://www.google.com/maps/search/?api=1&query=" +
                   lat.ToString(CultureInfo.InvariantCulture) + "," + lng.ToString(CultureInfo.InvariantCulture);
        }
    }
}
