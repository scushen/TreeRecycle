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

        /// <summary>Lookups answered from the cache since the app started (shown on health.ashx).</summary>
        public static long GeocodeCacheHits { get { return Interlocked.Read(ref _cacheHits); } }

        /// <summary>Billable Google Geocoding calls since the app started (shown on health.ashx).</summary>
        public static long GeocodeGoogleCalls { get { return Interlocked.Read(ref _googleCalls); } }

        /// <summary>
        /// Geocodes a postcode or a full address inside one country, from the cache when it can, otherwise with the
        /// Google Geocoding API. Returns null when nothing usable comes back. Throws on configuration or network errors.
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

            bool definitive;
            GeoPoint point = GeocodeFromGoogle(query, country, out definitive);
            if (point != null)
                GeoCache.Set(cacheKey, Copy(point), DateTimeOffset.UtcNow.Add(FoundLifetime));
            else if (definitive)
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
        /// One billable Google call. definitive = Google answered properly (found, or ZERO_RESULTS), so the answer can be
        /// cached; false for OVER_QUERY_LIMIT, REQUEST_DENIED, INVALID_REQUEST, UNKNOWN_ERROR.
        /// </summary>
        private static GeoPoint GeocodeFromGoogle(string query, CountryInfo country, out bool definitive)
        {
            definitive = false;
            string key = Settings.GoogleGeocodingApiKey;
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("GoogleGeocodingApiKey is not set.");

            string cc = country.Code.ToLowerInvariant();
            string url = "https://maps.googleapis.com/maps/api/geocode/json?region=" + (cc == "gb" ? "uk" : cc)
                         + "&components=country:" + country.Code
                         + "&address=" + Uri.EscapeDataString(query) + "&key=" + Uri.EscapeDataString(key);
            string json;
            Interlocked.Increment(ref _googleCalls);
            using (var wc = new WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                json = wc.DownloadString(url);
            }

            JObject root = JObject.Parse(json);
            string status = (string)root["status"];
            if (status == "ZERO_RESULTS")
            {
                definitive = true;
                return null;
            }
            if (status != "OK")
            {
                System.Diagnostics.Trace.TraceWarning("Google geocode status " + status + ": " + (string)root["error_message"]);
                return null;
            }
            definitive = true;

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

        public static string GoogleMapsLink(decimal lat, decimal lng)
        {
            return "https://www.google.com/maps/search/?api=1&query=" +
                   lat.ToString(CultureInfo.InvariantCulture) + "," + lng.ToString(CultureInfo.InvariantCulture);
        }
    }
}
