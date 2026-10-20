using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Geocodes an Eircode (or a free-text Irish address) with the Google Geocoding API.
        /// Returns null when nothing usable comes back. Throws only on configuration errors.
        /// </summary>
        public static GeoPoint Geocode(string eircodeOrAddress)
        {
            if (string.IsNullOrWhiteSpace(eircodeOrAddress))
                return null;
            string key = Settings.GoogleGeocodingApiKey;
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("GoogleGeocodingApiKey is not set in web.config.");

            string url = "https://maps.googleapis.com/maps/api/geocode/json?region=ie&components=country:IE&address="
                         + Uri.EscapeDataString(eircodeOrAddress.Trim()) + "&key=" + Uri.EscapeDataString(key);
            string json;
            using (var wc = new WebClient())
            {
                wc.Encoding = System.Text.Encoding.UTF8;
                json = wc.DownloadString(url);
            }

            JObject root = JObject.Parse(json);
            if ((string)root["status"] != "OK")
                return null;
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
