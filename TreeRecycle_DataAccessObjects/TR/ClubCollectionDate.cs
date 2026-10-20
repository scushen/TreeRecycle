using System.Collections.Generic;
using System.Linq;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// A day the club goes out collecting. collection_date holds the Irish calendar date at midnight (not UTC).
    /// </summary>
    public partial class ClubCollectionDate
    {
        public string DisplayName
        {
            get
            {
                string date = CollectionDate.ToString("dddd d MMMM yyyy");
                return string.IsNullOrWhiteSpace(Label) ? date : date + " (" + Label + ")";
            }
        }

        public static IList<ClubCollectionDate> ForClub(int clubId, bool activeOnly)
        {
            var criteria = new List<ICriterion> { Expression.Eq(PropertyNames.ClubId, clubId) };
            if (activeOnly)
                criteria.Add(Expression.Eq(PropertyNames.Active, true));
            return FindAll(criteria.ToArray(), new[] { Order.Asc(PropertyNames.CollectionDate) });
        }

        /// <summary>Trees already booked (paid or collected) for this date.</summary>
        public int TreesBooked()
        {
            return TreeOrder.ForCollectionDate(ClubCollectionDateId)
                .Where(o => o.StatusEnum == OrderStatus.Paid || o.StatusEnum == OrderStatus.Collected || o.StatusEnum == OrderStatus.NotOut)
                .Sum(o => o.Quantity);
        }

        public bool IsFull(int extraTrees)
        {
            return MaxTrees > 0 && TreesBooked() + extraTrees > MaxTrees;
        }
    }
}
