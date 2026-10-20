using System;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public class DateRow
    {
        public ClubCollectionDate Date { get; set; }
        public int Booked { get; set; }
        public int ToRemind { get; set; }
        public string ReminderText { get; set; }
    }

    public partial class Dates : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                Bind();
        }

        private void Bind()
        {
            var orders = TreeOrder.ForClub(CurrentClub.ClubId);
            var rows = ClubCollectionDate.ForClub(CurrentClub.ClubId, false).Select(d =>
            {
                var dayOrders = orders.Where(o => o.ClubCollectionDateId == d.ClubCollectionDateId && o.IsSale).ToList();
                int toRemind = dayOrders.Count(o => o.IsOutstanding && !o.ReminderSentDate.HasValue);
                int reminded = dayOrders.Count(o => o.ReminderSentDate.HasValue);
                return new DateRow
                {
                    Date = d,
                    Booked = dayOrders.Sum(o => o.Quantity),
                    ToRemind = d.CollectionDate.Date >= Settings.TodayIrish ? toRemind : 0,
                    ReminderText = reminded == 0 ? "Not sent" : reminded + " sent" + (d.ReminderSentDate.HasValue ? ", last " + IrishDate(d.ReminderSentDate, "d MMM HH:mm") : ""),
                };
            }).ToList();
            rptDates.DataSource = rows;
            rptDates.DataBind();
            litEmpty.Text = rows.Count == 0 ? "<p class=\"muted\">No collection days yet - add one below.</p>" : "";
        }

        private ClubCollectionDate MyDate(object arg)
        {
            int id;
            if (!int.TryParse(Convert.ToString(arg), out id))
                return null;
            ClubCollectionDate d = ClubCollectionDate.FindByPrimaryKey(id, false);
            return d != null && d.ClubId == CurrentClub.ClubId ? d : null;
        }

        protected void rptDates_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            ClubCollectionDate d = MyDate(e.CommandArgument);
            if (d == null)
                return;

            if (e.CommandName == "toggle")
            {
                d.Active = !d.Active;
                d.Update();
                litMsg.Text = "<div class=\"msg ok\">" + Enc(d.DisplayName) + (d.Active ? " is taking bookings again." : " is closed to new bookings.") + "</div>";
            }
            else if (e.CommandName == "remind")
            {
                var targets = TreeOrder.ForCollectionDate(d.ClubCollectionDateId).Where(o => o.IsOutstanding && !o.ReminderSentDate.HasValue).ToList();
                int sent = 0, failed = 0;
                foreach (TreeOrder o in targets)
                {
                    if (Mailer.SendOrderEmail(o, EmailTemplateType.Reminder)) sent++;
                    else failed++;
                }
                d.ReminderSentDate = DateTime.UtcNow;
                d.Update();
                litMsg.Text = failed == 0
                    ? "<div class=\"msg ok\">Reminder sent to " + sent + " customer(s).</div>"
                    : "<div class=\"msg warn\">Sent " + sent + ", but " + failed + " failed. Use the button again to retry the ones that failed.</div>";
            }
            Bind();
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            DateTime date;
            int max;
            if (!DateTime.TryParseExact(txtDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                litMsg.Text = "<div class=\"msg err\">Pick a date.</div>";
                return;
            }
            if (date < Settings.TodayIrish)
            {
                litMsg.Text = "<div class=\"msg err\">That date has passed.</div>";
                return;
            }
            int.TryParse(txtMax.Text, out max);
            new ClubCollectionDate
            {
                ClubId = CurrentClub.ClubId,
                CollectionDate = date.Date,
                Label = Clean(txtLabel.Text, 100),
                MaxTrees = Math.Max(0, max),
                Active = true,
                CreatedBy = "CLB",
                UpdatedBy = "CLB",
            }.Create();
            txtDate.Text = txtLabel.Text = "";
            txtMax.Text = "0";
            Bind();
            litMsg.Text = "<div class=\"msg ok\">Collection day added.</div>";
        }
    }
}
