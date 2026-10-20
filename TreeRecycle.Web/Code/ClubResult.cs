using System.Globalization;
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
        public int RadiusKm { get; set; }
        public string PriceFrom { get; set; }
        public string NextDate { get; set; }
        public string Url { get; set; }

        public static ClubResult From(Club club, string eircode, double? distanceKm)
        {
            var prices = club.ActivePrices;
            var dates = club.UpcomingDates;
            string url = VirtualPathUtility.ToAbsolute("~/c/" + club.Slug);
            if (!string.IsNullOrEmpty(eircode))
                url += "?eircode=" + HttpUtility.UrlEncode(eircode);
            return new ClubResult
            {
                Name = club.ClubName,
                Place = string.IsNullOrEmpty(club.Town) ? "Co. " + club.County : club.Town + ", Co. " + club.County,
                DistanceText = distanceKm.HasValue ? distanceKm.Value.ToString("0.0", CultureInfo.InvariantCulture) + " km from you" : "",
                RadiusKm = club.CollectionRadiusKm,
                PriceFrom = prices.Count == 0 ? "" : "From " + Settings.Money(prices.Min(p => p.Price)) + " per tree",
                NextDate = dates.Count == 0 ? "" : "Collecting " + dates[0].CollectionDate.ToString("ddd d MMM"),
                Url = url,
            };
        }
    }
}
