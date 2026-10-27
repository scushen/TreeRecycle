using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Everything that differs between the countries we operate in: currency, postcode format and name,
    /// region list, distance unit, default time zone and booking fee. Add a country here and the whole site follows.
    /// </summary>
    public class CountryInfo
    {
        public string Code { get; set; }               // ISO 3166-1 alpha-2, as Stripe and Google use it
        public string Name { get; set; }
        public string Currency { get; set; }           // lower-case ISO 4217, as Stripe uses it
        public string CurrencySymbol { get; set; }
        public decimal DefaultBookingFee { get; set; }
        public string PostcodeName { get; set; }       // "Eircode", "postcode", "ZIP code"
        public string PostcodeExample { get; set; }
        /// <summary>True when a postcode pinpoints one address (Eircode). Otherwise we geocode the full address on orders.</summary>
        public bool PostcodeIsExact { get; set; }
        public string RegionName { get; set; }         // "County", "State", "Region"
        /// <summary>Fixed region list for dropdowns; null = free text (UK counties are too irregular to list).</summary>
        public string[] Regions { get; set; }
        public bool UsesMiles { get; set; }
        public string DefaultTimeZone { get; set; }    // Windows time zone id (Azure App Service runs Windows)
        /// <summary>Time zones a club in this country can pick from (id, label). One entry = no choice needed.</summary>
        public KeyValuePair<string, string>[] TimeZones { get; set; }
        public Func<string, string> NormalisePostcode { get; set; }

        public string DistanceUnit { get { return UsesMiles ? "miles" : "km"; } }
        public string DistanceUnitShort { get { return UsesMiles ? "mi" : "km"; } }

        /// <summary>Booking fee per tree in this country's currency: appSetting PlatformFeePerTree_GBP etc., else the default.</summary>
        public decimal BookingFee
        {
            get
            {
                string configured = Settings.Get("PlatformFeePerTree_" + Currency.ToUpperInvariant());
                if (configured == "" && Currency == "eur")
                    configured = Settings.Get("PlatformFeePerTree"); // original single-currency setting
                decimal fee;
                return decimal.TryParse(configured, NumberStyles.Number, CultureInfo.InvariantCulture, out fee) ? fee : DefaultBookingFee;
            }
        }

        public string Money(decimal amount)
        {
            return CurrencySymbol + amount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>Distance for display in this country's unit, e.g. "3.4 km" or "2.1 mi".</summary>
        public string Distance(double km)
        {
            double value = UsesMiles ? km / Countries.KmPerMile : km;
            return value.ToString("0.0", CultureInfo.InvariantCulture) + " " + DistanceUnitShort;
        }

        public int ToDisplayUnits(double km)
        {
            return (int)Math.Round(UsesMiles ? km / Countries.KmPerMile : km);
        }

        public double FromDisplayUnits(int value)
        {
            return UsesMiles ? value * Countries.KmPerMile : value;
        }

        public bool HasRegionList { get { return Regions != null && Regions.Length > 0; } }

        /// <summary>Matches a region name loosely ("NSW", "New South Wales", "County Cork", "Co. Cork") to the list.</summary>
        public string MatchRegion(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            string clean = name.Replace("County ", "").Replace("Co. ", "").Trim();
            if (!HasRegionList)
                return clean;
            string hit = Regions.FirstOrDefault(r => r.Equals(clean, StringComparison.OrdinalIgnoreCase));
            if (hit == null && Code == "AU")
            {
                string fromAbbrev;
                if (AustralianAbbreviations.TryGetValue(clean.ToUpperInvariant(), out fromAbbrev))
                    hit = fromAbbrev;
            }
            if (hit == null && Code == "US")
            {
                string fromAbbrev;
                if (UsAbbreviations.TryGetValue(clean.ToUpperInvariant(), out fromAbbrev))
                    hit = fromAbbrev;
            }
            if (hit == null && Code == "IE" && (clean.StartsWith("Dublin", StringComparison.OrdinalIgnoreCase)
                                                || clean.Contains("Dún Laoghaire") || clean == "Fingal"))
                hit = "Dublin";
            return hit ?? string.Empty;
        }

        internal static readonly Dictionary<string, string> AustralianAbbreviations = new Dictionary<string, string>
        {
            { "ACT", "Australian Capital Territory" }, { "NSW", "New South Wales" }, { "NT", "Northern Territory" }, { "QLD", "Queensland" },
            { "SA", "South Australia" }, { "TAS", "Tasmania" }, { "VIC", "Victoria" }, { "WA", "Western Australia" },
        };

        internal static readonly Dictionary<string, string> UsAbbreviations = new Dictionary<string, string>();
    }

    public static class Countries
    {
        public const double KmPerMile = 1.609344;
        public const string DefaultCode = "IE";

        private static readonly string[] UsStates =
        {
            "Alabama|AL", "Alaska|AK", "Arizona|AZ", "Arkansas|AR", "California|CA", "Colorado|CO", "Connecticut|CT", "Delaware|DE",
            "District of Columbia|DC", "Florida|FL", "Georgia|GA", "Hawaii|HI", "Idaho|ID", "Illinois|IL", "Indiana|IN", "Iowa|IA",
            "Kansas|KS", "Kentucky|KY", "Louisiana|LA", "Maine|ME", "Maryland|MD", "Massachusetts|MA", "Michigan|MI", "Minnesota|MN",
            "Mississippi|MS", "Missouri|MO", "Montana|MT", "Nebraska|NE", "Nevada|NV", "New Hampshire|NH", "New Jersey|NJ",
            "New Mexico|NM", "New York|NY", "North Carolina|NC", "North Dakota|ND", "Ohio|OH", "Oklahoma|OK", "Oregon|OR",
            "Pennsylvania|PA", "Rhode Island|RI", "South Carolina|SC", "South Dakota|SD", "Tennessee|TN", "Texas|TX", "Utah|UT",
            "Vermont|VT", "Virginia|VA", "Washington|WA", "West Virginia|WV", "Wisconsin|WI", "Wyoming|WY",
        };

        public static readonly IList<CountryInfo> All;
        private static readonly Dictionary<string, CountryInfo> ByCode;

        static Countries()
        {
            foreach (string s in UsStates)
            {
                string[] parts = s.Split('|');
                CountryInfo.UsAbbreviations[parts[1]] = parts[0];
            }

            All = new List<CountryInfo>
            {
                new CountryInfo
                {
                    Code = "IE", Name = "Ireland", Currency = "eur", CurrencySymbol = "€", DefaultBookingFee = 2.00m,
                    PostcodeName = "Eircode", PostcodeExample = "A65 F4E2", PostcodeIsExact = true,
                    RegionName = "County", UsesMiles = false, DefaultTimeZone = "GMT Standard Time",
                    TimeZones = new[] { Tz("GMT Standard Time", "Irish time") },
                    Regions = new[]
                    {
                        "Carlow", "Cavan", "Clare", "Cork", "Donegal", "Dublin", "Galway", "Kerry", "Kildare", "Kilkenny", "Laois",
                        "Leitrim", "Limerick", "Longford", "Louth", "Mayo", "Meath", "Monaghan", "Offaly", "Roscommon", "Sligo",
                        "Tipperary", "Waterford", "Westmeath", "Wexford", "Wicklow",
                    },
                    NormalisePostcode = NormaliseEircode,
                },
                new CountryInfo
                {
                    Code = "GB", Name = "United Kingdom", Currency = "gbp", CurrencySymbol = "£", DefaultBookingFee = 2.00m,
                    PostcodeName = "postcode", PostcodeExample = "SW1A 1AA", PostcodeIsExact = false,
                    RegionName = "County", Regions = null, UsesMiles = true, DefaultTimeZone = "GMT Standard Time",
                    TimeZones = new[] { Tz("GMT Standard Time", "UK time") },
                    NormalisePostcode = NormaliseUkPostcode,
                },
                new CountryInfo
                {
                    Code = "AU", Name = "Australia", Currency = "aud", CurrencySymbol = "A$", DefaultBookingFee = 3.00m,
                    PostcodeName = "postcode", PostcodeExample = "2000", PostcodeIsExact = false,
                    RegionName = "State", UsesMiles = false, DefaultTimeZone = "AUS Eastern Standard Time",
                    TimeZones = new[]
                    {
                        Tz("AUS Eastern Standard Time", "Sydney, Melbourne, Canberra"), Tz("E. Australia Standard Time", "Brisbane"),
                        Tz("Tasmania Standard Time", "Hobart"), Tz("Cen. Australia Standard Time", "Adelaide"),
                        Tz("AUS Central Standard Time", "Darwin"), Tz("W. Australia Standard Time", "Perth"),
                    },
                    Regions = CountryInfo.AustralianAbbreviations.Values.OrderBy(v => v).ToArray(),
                    NormalisePostcode = p => Digits(p, 4),
                },
                new CountryInfo
                {
                    Code = "NZ", Name = "New Zealand", Currency = "nzd", CurrencySymbol = "NZ$", DefaultBookingFee = 3.00m,
                    PostcodeName = "postcode", PostcodeExample = "6011", PostcodeIsExact = false,
                    RegionName = "Region", UsesMiles = false, DefaultTimeZone = "New Zealand Standard Time",
                    TimeZones = new[] { Tz("New Zealand Standard Time", "New Zealand time") },
                    Regions = new[]
                    {
                        "Auckland", "Bay of Plenty", "Canterbury", "Gisborne", "Hawke's Bay", "Manawatū-Whanganui", "Marlborough", "Nelson",
                        "Northland", "Otago", "Southland", "Taranaki", "Tasman", "Waikato", "Wellington", "West Coast",
                    },
                    NormalisePostcode = p => Digits(p, 4),
                },
                new CountryInfo
                {
                    Code = "US", Name = "United States", Currency = "usd", CurrencySymbol = "US$", DefaultBookingFee = 2.00m,
                    PostcodeName = "ZIP code", PostcodeExample = "10001", PostcodeIsExact = false,
                    RegionName = "State", UsesMiles = true, DefaultTimeZone = "Eastern Standard Time",
                    TimeZones = new[]
                    {
                        Tz("Eastern Standard Time", "Eastern"), Tz("Central Standard Time", "Central"), Tz("Mountain Standard Time", "Mountain"),
                        Tz("US Mountain Standard Time", "Arizona"), Tz("Pacific Standard Time", "Pacific"),
                        Tz("Alaskan Standard Time", "Alaska"), Tz("Hawaiian Standard Time", "Hawaii"),
                    },
                    Regions = UsStates.Select(s => s.Split('|')[0]).ToArray(),
                    NormalisePostcode = NormaliseZip,
                },
            };
            ByCode = All.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The country for a code; unknown or empty codes fall back to Ireland.</summary>
        public static CountryInfo Get(string code)
        {
            CountryInfo c;
            return code != null && ByCode.TryGetValue(code, out c) ? c : ByCode[DefaultCode];
        }

        /// <summary>The country that uses a currency ("eur" -> Ireland), for formatting stored order amounts.</summary>
        public static CountryInfo ForCurrency(string currency)
        {
            return All.FirstOrDefault(c => c.Currency.Equals(currency ?? "", StringComparison.OrdinalIgnoreCase)) ?? ByCode[DefaultCode];
        }

        /// <summary>Totals per currency, e.g. "€70.00 · £12.00"; amounts in different currencies are never added together.</summary>
        public static string MoneyByCurrency<T>(IEnumerable<T> items, Func<T, string> currency, Func<T, decimal> amount)
        {
            var parts = items.GroupBy(i => (currency(i) ?? "").ToLowerInvariant())
                             .Select(g => ForCurrency(g.Key).Money(g.Sum(amount)))
                             .ToList();
            return parts.Count == 0 ? ByCode[DefaultCode].Money(0) : string.Join(" · ", parts);
        }

        public static bool IsSupported(string code)
        {
            return code != null && ByCode.ContainsKey(code);
        }

        /// <summary>Best guess from the host name (.ie, .co.uk, .com.au, .co.nz); null when it says nothing.</summary>
        public static string FromHost(string host)
        {
            host = (host ?? "").ToLowerInvariant();
            if (host.EndsWith(".ie")) return "IE";
            if (host.EndsWith(".uk")) return "GB";
            if (host.EndsWith(".au")) return "AU";
            if (host.EndsWith(".nz")) return "NZ";
            if (host.EndsWith(".us")) return "US";
            return null;
        }

        private static KeyValuePair<string, string> Tz(string id, string label)
        {
            return new KeyValuePair<string, string>(id, label);
        }

        // Eircode: routing key (A65, D6W, ...) + 4-character identifier. Letters exclude B, G, I, J, L, M, O, Q, S, U, Z.
        private static readonly Regex EircodeRegex = new Regex(@"^(?:[AC-FHKNPRTV-Y]\d{2}|D6W)[0-9AC-FHKNPRTV-Y]{4}$", RegexOptions.Compiled);
        private static readonly Regex UkPostcodeRegex = new Regex(@"^[A-Z]{1,2}\d[A-Z\d]?\d[A-Z]{2}$", RegexOptions.Compiled);

        private static string Compact(string input)
        {
            return Regex.Replace((input ?? "").ToUpperInvariant(), @"[\s-]+", "");
        }

        /// <summary>"a65f4e2" -> "A65 F4E2", or "" when invalid.</summary>
        public static string NormaliseEircode(string input)
        {
            string c = Compact(input);
            return EircodeRegex.IsMatch(c) ? c.Substring(0, 3) + " " + c.Substring(3) : string.Empty;
        }

        /// <summary>"sw1a1aa" -> "SW1A 1AA", or "" when invalid.</summary>
        public static string NormaliseUkPostcode(string input)
        {
            string c = Compact(input);
            return UkPostcodeRegex.IsMatch(c) ? c.Substring(0, c.Length - 3) + " " + c.Substring(c.Length - 3) : string.Empty;
        }

        /// <summary>"10001" or "10001-1234", or "" when invalid.</summary>
        public static string NormaliseZip(string input)
        {
            string c = Compact(input);
            if (Regex.IsMatch(c, @"^\d{5}$")) return c;
            if (Regex.IsMatch(c, @"^\d{9}$")) return c.Substring(0, 5) + "-" + c.Substring(5);
            return string.Empty;
        }

        private static string Digits(string input, int length)
        {
            string c = Compact(input);
            return Regex.IsMatch(c, @"^\d{" + length + "}$") ? c : string.Empty;
        }
    }
}
