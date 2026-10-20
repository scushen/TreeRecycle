using System;
using System.Linq;
using System.Text;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Dashboard : ClubPage
    {
        protected string PublicUrl { get { return CurrentClub.PublicUrl; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            Club club = CurrentClub;
            var blockers = club.OrderBlockers();
            phSetup.Visible = blockers.Count > 0;
            phLive.Visible = blockers.Count == 0;
            litBlockers.Text = string.Concat(blockers.Select(b => "<li>" + Enc(b) + "</li>"));

            var orders = TreeOrder.ForClub(club.ClubId);
            var sales = orders.Where(o => o.IsSale).ToList();
            var money = ClubMoney.For(club, orders);
            litTrees.Text = sales.Sum(o => o.Quantity).ToString();
            litToCollect.Text = sales.Where(o => o.IsOutstanding).Sum(o => o.Quantity).ToString();
            litCollected.Text = sales.Where(o => o.StatusEnum == OrderStatus.Collected).Sum(o => o.Quantity).ToString();
            litNet.Text = Money(money.Net);

            var dates = ClubCollectionDate.ForClub(club.ClubId, true);
            if (dates.Count == 0)
                litDates.Text = "<p class=\"muted\">No collection days yet.</p>";
            else
            {
                var sb = new StringBuilder("<table class=\"data\"><tr><th>Day</th><th class=\"num\">Trees</th><th class=\"num\">To collect</th></tr>");
                foreach (var d in dates)
                {
                    var dayOrders = sales.Where(o => o.ClubCollectionDateId == d.ClubCollectionDateId).ToList();
                    sb.AppendFormat("<tr><td><a href=\"routes.aspx?date={0}\">{1}</a></td><td class=\"num\">{2}</td><td class=\"num\">{3}</td></tr>",
                        d.ClubCollectionDateId, Enc(d.DisplayName), dayOrders.Sum(o => o.Quantity), dayOrders.Where(o => o.IsOutstanding).Sum(o => o.Quantity));
                }
                litDates.Text = sb.Append("</table><br />").ToString();
            }

            var latest = sales.Take(8).ToList();
            if (latest.Count == 0)
                litLatest.Text = "<p class=\"muted\">No bookings yet.</p>";
            else
            {
                var sb = new StringBuilder("<table class=\"data\"><tr><th>When</th><th>Who</th><th>Tree</th><th class=\"num\">Paid</th></tr>");
                foreach (var o in latest)
                    sb.AppendFormat("<tr><td class=\"nowrap\">{0}</td><td>{1}<div class=\"small muted\">{2}</div></td><td>{3}</td><td class=\"num\">{4}</td></tr>",
                        IrishDate(o.PaidDate ?? o.CreatedDate, "d MMM HH:mm"), Enc(o.CustomerName), Enc(o.Town + " " + o.Eircode), Enc(o.Quantity + " x " + o.SizeLabel), Money(o.TotalAmount));
                litLatest.Text = sb.Append("</table><br />").ToString();
            }
        }
    }
}
