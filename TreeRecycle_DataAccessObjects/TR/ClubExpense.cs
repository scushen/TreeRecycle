using System.Collections.Generic;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>Costs a club logs against its collection (skip hire, trailer hire, diesel...) for the profit figure.</summary>
    public partial class ClubExpense
    {
        public static IList<ClubExpense> ForClub(int clubId)
        {
            return FindAll(new ICriterion[] { Expression.Eq(PropertyNames.ClubId, clubId) }, new[] { Order.Asc(PropertyNames.CreatedDate) });
        }
    }
}
