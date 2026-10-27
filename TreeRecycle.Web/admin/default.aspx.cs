using System;
using System.Collections.Generic;
using System.Linq;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.Admin
{
    public class ClubRow
    {
        public Club Club { get; set; }
        public string Registered { get; set; }
        public string StatusClass { get; set; }
        public int Trees { get; set; }
        public string Fees { get; set; }
    }

    public partial class Clubs : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Club.FindByStatus(ClubStatus.Pending).Count == 0)
                    ddlStatus.SelectedValue = "1";
                Bind();
            }
        }

        protected void ddlStatus_Changed(object sender, EventArgs e)
        {
            Bind();
        }

        private void Bind()
        {
            IList<Club> all = Club.FindAllClubs();
            IList<TreeOrder> sales = TreeOrder.AllSales();
            var byClub = sales.GroupBy(o => o.ClubId).ToDictionary(g => g.Key, g => g.ToList());

            litPending.Text = all.Count(c => c.StatusEnum == ClubStatus.Pending).ToString();
            litLive.Text = all.Count(c => c.StatusEnum == ClubStatus.Approved && c.StripeChargesEnabled && c.AcceptingOrders).ToString();
            litTrees.Text = sales.Sum(o => o.Quantity).ToString();
            litFees.Text = Enc(Countries.MoneyByCurrency(sales, o => o.Currency, o => o.PlatformFee));

            var shown = all.Where(c => ddlStatus.SelectedValue == "" || c.Status.ToString() == ddlStatus.SelectedValue)
                           .OrderByDescending(c => c.CreatedDate).ToList();
            rptClubs.DataSource = shown.Select(c =>
            {
                List<TreeOrder> mine;
                byClub.TryGetValue(c.ClubId, out mine);
                mine = mine ?? new List<TreeOrder>();
                return new ClubRow
                {
                    Club = c,
                    Registered = AdminDate(c.CreatedDate, "d MMM yyyy"),
                    StatusClass = c.StatusEnum == ClubStatus.Approved ? "" : c.StatusEnum == ClubStatus.Pending ? "gold" : "red",
                    Trees = mine.Sum(o => o.Quantity),
                    Fees = c.Money(mine.Sum(o => o.PlatformFee)),
                };
            }).ToList();
            rptClubs.DataBind();
            litEmpty.Text = shown.Count == 0 ? "<p class=\"muted\" style=\"padding:16px\">Nothing here.</p>" : "";
        }
    }
}
