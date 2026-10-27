using System.Linq;
using System.Web;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>One club in the public search results.</summary>
    public class ClubResult
    {
        public string Name { get; set; }
        public string Place { get; set; }
        public string DistanceText { get; set; }
        /// <summary>"collects within 20 km" / "collects within 12 miles".</summary>
        public string RadiusText { get; set; }
        public string PriceFrom { get; set; }
        public string NextDate { get; set; }
        public string Url { get; set; }

        public static ClubResult From(Club club, string postcode, double? distanceKm)
        {
            var prices = club.ActivePrices;
            var dates = club.UpcomingDates;
            CountryInfo country = club.Country;
            string url = VirtualPathUtility.ToAbsolute("~/c/" + club.Slug);
            if (!string.IsNullOrEmpty(postcode))
                url += "?postcode=" + HttpUtility.UrlEncode(postcode);
            string region = club.Region == "" ? "" : (club.CountryCode == "IE" ? "Co. " + club.Region : club.Region);
            return new ClubResult
            {
                Name = club.ClubName,
                Place = string.Join(", ", new[] { club.Town, region }.Where(s => !string.IsNullOrEmpty(s))),
                DistanceText = distanceKm.HasValue ? country.Distance(distanceKm.Value) + " from you" : "",
                RadiusText = "collects within " + club.RadiusInDisplayUnits + " " + country.DistanceUnit,
                PriceFrom = prices.Count == 0 ? "" : "From " + club.Money(prices.Min(p => p.Price)) + " per tree",
                NextDate = dates.Count == 0 ? "" : "Collecting " + dates[0].CollectionDate.ToString("ddd d MMM"),
                Url = url,
            };
        }
    }
}
