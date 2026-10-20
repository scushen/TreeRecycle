using System.Collections.Generic;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    public partial class ClubPrice
    {
        public string PriceText { get { return Settings.Money(Price); } }

        public static IList<ClubPrice> ForClub(int clubId, bool activeOnly)
        {
            var criteria = new List<ICriterion> { Expression.Eq(PropertyNames.ClubId, clubId) };
            if (activeOnly)
                criteria.Add(Expression.Eq(PropertyNames.Active, true));
            return FindAll(criteria.ToArray(), new[] { Order.Asc(PropertyNames.SortOrder), Order.Asc(PropertyNames.SizeFeet) });
        }

        /// <summary>Starter price list for a newly registered club; they edit it before going live.</summary>
        public static void CreateDefaults(int clubId)
        {
            var defaults = new[]
            {
                new { Label = "Up to 5ft", Feet = 5, Price = 10m },
                new { Label = "6ft - 7ft", Feet = 7, Price = 15m },
                new { Label = "8ft and over", Feet = 8, Price = 20m },
            };
            int sort = 1;
            foreach (var d in defaults)
            {
                new ClubPrice { ClubId = clubId, Label = d.Label, SizeFeet = d.Feet, Price = d.Price, SortOrder = sort++, Active = true, CreatedBy = "SYS", UpdatedBy = "SYS" }.Create();
            }
        }
    }
}
