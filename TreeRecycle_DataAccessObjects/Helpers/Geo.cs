using System;
using System.Collections.Generic;
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
        public string County { get; set; }
        public string Town { get; set; }
    }

    /// <summary>
    /// Counties, Eircodes, distances and geocoding for the Republic of Ireland.
    /// </summary>
    public static class Geo
    {
        public static readonly string[] Provinces = { "Connacht", "Leinster", "Munster", "Ulster" };

        /// <summary>The 26 counties of the Republic, keyed to their province (Ulster = Cavan, Donegal, Monaghan).</summary>
        public static readonly Dictionary<string, string> CountyProvince = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Galway", "Connacht" }, { "Leitrim", "Connacht" }, { "Mayo", "Connacht" }, { "Roscommon", "Connacht" }, { "Sligo", "Connacht" },
            { "Carlow", "Leinster" }, { "Dublin", "Leinster" }, { "Kildare", "Leinster" }, { "Kilkenny", "Leinster" }, { "Laois", "Leinster" },
            { "Longford", "Leinster" }, { "Louth", "Leinster" }, { "Meath", "Leinster" }, { "Offaly", "Leinster" }, { "Westmeath", "Leinster" },
            { "Wexford", "Leinster" }, { "Wicklow", "Leinster" },
            { "Clare", "Munster" }, { "Cork", "Munster" }, { "Kerry", "Munster" }, { "Limerick", "Munster" }, { "Tipperary", "Munster" }, { "Waterford", "Munster" },
            { "Cavan", "Ulster" }, { "Donegal", "Ulster" }, { "Monaghan", "Ulster" },
        };

        public static IList<string> CountiesIn(string province)
        {
            return CountyProvince.Where(kv => string.IsNullOrEmpty(province) || kv.Value.Equals(province, StringComparison.OrdinalIgnoreCase))
                                 .Select(kv => kv.Key).OrderBy(c => c).ToList();
        }

        public static IList<string> AllCounties { get { return CountiesIn(null); } }

        public static string ProvinceOf(string county)
        {
            string province;
            return county != null && CountyProvince.TryGetValue(county, out province) ? province : string.Empty;
        }

        // Routing key (A65, D6W, ...) + 4-character unique identifier. Letters exclude B, G, I, J, L, M, O, Q, S, U, Z.
        private static readonly Regex EircodeRegex = new Regex(@"^(?:[AC-FHKNPRTV-Y]\d{2}|D6W)[0-9AC-FHKNPRTV-Y]{4}$", RegexOptions.Compiled);

        /// <summary>Upper-cases, strips spaces and returns "A65 F4E2" form, or "" when it isn't a valid Eircode.</summary>
        public static string NormaliseEircode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            string compact = Regex.Replace(input.ToUpperInvariant(), @"\s+", "");
            if (!EircodeRegex.IsMatch(compact))
                return string.Empty;
            return compact.Substring(0, 3) + " " + compact.Substring(3);
        }

        /// <summary>Great-circle distance in km. Road distance is typically 1.2-1.4x this in rural Ireland.</summary>
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

        // Google lookups are billed per call, and one booking typically looks the same Eircode up twice
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
        /// Geocodes an Eircode (or a free-text Irish address), from the cache when it can, otherwise with the
        /// Google Geocoding API. Returns null when nothing usable comes back. Throws on configuration or network errors.
        /// </summary>
        public static GeoPoint Geocode(string eircodeOrAddress)
        {
            if (string.IsNullOrWhiteSpace(eircodeOrAddress))
                return null;

            // "a65f4e2", "A65 F4E2" and " a65  f4e2 " share one cache entry.
            string eircode = NormaliseEircode(eircodeOrAddress);
            string cacheKey = eircode != string.Empty ? eircode : Regex.Replace(eircodeOrAddress.Trim().ToUpperInvariant(), @"\s+", " ");

            object cached = GeoCache.Get(cacheKey);
            if (cached != null)
            {
                Interlocked.Increment(ref _cacheHits);
                return cached == NotFound ? null : Copy((GeoPoint)cached);
            }

            bool definitive;
            GeoPoint point = GeocodeFromGoogle(cacheKey, out definitive);
            if (point != null)
                GeoCache.Set(cacheKey, Copy(point), DateTimeOffset.UtcNow.Add(FoundLifetime));
            else if (definitive)
                GeoCache.Set(cacheKey, NotFound, DateTimeOffset.UtcNow.Add(NotFoundLifetime));
            return point;
        }

        private static GeoPoint Copy(GeoPoint p)
        {
            return new GeoPoint
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                FormattedAddress = p.FormattedAddress,
                Precision = p.Precision,
                County = p.County,
                Town = p.Town,
            };
        }

        /// <summary>
        /// One billable Google call. definitive = Google answered properly (found, or ZERO_RESULTS), so the answer can be
        /// cached; false for OVER_QUERY_LIMIT, REQUEST_DENIED, INVALID_REQUEST, UNKNOWN_ERROR.
        /// </summary>
        private static GeoPoint GeocodeFromGoogle(string eircodeOrAddress, out bool definitive)
        {
            definitive = false;
            string key = Settings.GoogleGeocodingApiKey;
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("GoogleGeocodingApiKey is not set in web.config.");

            string url = "https://maps.googleapis.com/maps/api/geocode/json?region=ie&components=country:IE&address="
                         + Uri.EscapeDataString(eircodeOrAddress.Trim()) + "&key=" + Uri.EscapeDataString(key);
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
                County = string.Empty,
                Town = string.Empty,
            };
            foreach (JToken comp in first["address_components"])
            {
                var types = comp["types"].Values<string>().ToList();
                string name = (string)comp["long_name"] ?? string.Empty;
                if (types.Contains("administrative_area_level_1") || types.Contains("administrative_area_level_2"))
                {
                    string county = name.Replace("County ", "").Replace("Co. ", "").Trim();
                    if (county.StartsWith("Dublin", StringComparison.OrdinalIgnoreCase) || county.Contains("Dún Laoghaire") || county == "Fingal")
                        county = "Dublin";
                    if (CountyProvince.ContainsKey(county) && point.County == string.Empty)
                        point.County = county;
                }
                if ((types.Contains("locality") || types.Contains("postal_town")) && point.Town == string.Empty)
                    point.Town = name;
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
