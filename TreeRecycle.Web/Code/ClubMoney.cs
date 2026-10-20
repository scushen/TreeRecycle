using System.Collections.Generic;
using System.Linq;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// The club's money picture. Gross = what customers paid for collections (our booking fee excluded).
    /// Refunded orders drop out entirely.
    /// </summary>
    public class ClubMoney
    {
        public decimal Gross { get; private set; }
        public decimal StripeFees { get; private set; }
        public decimal Expenses { get; private set; }
        public decimal Net { get { return Gross - StripeFees - Expenses; } }
        public decimal PlatformFees { get; private set; }
        public int Trees { get; private set; }

        public static ClubMoney For(Club club, IList<TreeOrder> orders)
        {
            var sales = orders.Where(o => o.IsSale).ToList();
            return new ClubMoney
            {
                Gross = sales.Sum(o => o.ClubAmount),
                StripeFees = sales.Sum(o => o.StripeFee),
                PlatformFees = sales.Sum(o => o.PlatformFee),
                Trees = sales.Sum(o => o.Quantity),
                Expenses = ClubExpense.ForClub(club.ClubId).Sum(x => x.Amount),
            };
        }
    }
}
