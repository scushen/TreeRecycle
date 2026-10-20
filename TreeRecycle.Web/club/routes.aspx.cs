using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public class RunView
    {
        public int Number { get; set; }
        public string Colour { get; set; }
        public int Trees { get; set; }
        public int Outstanding { get; set; }
        public List<TreeOrder> Orders { get; set; }
        public string LinksHtml { get; set; }
    }

    public partial class Routes : ClubPage
    {
        private static readonly string[] Colours = { "#2f6b4f", "#b8322a", "#2b6cb0", "#c98a1b", "#7b3fa0", "#0f8a8a", "#d1495b", "#5a6b1f", "#8a5a2b", "#3d4f9a" };

        protected string MapJson = "null";
        protected string ClubName { get { return CurrentClub.ClubName; } }
        protected string DayName { get { return ddlDate.SelectedItem == null ? "" : ddlDate.SelectedItem.Text; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            litCapacity.Text = CurrentClub.TrailerCapacity.ToString();
            if (!IsPostBack)
            {
                var dates = ClubCollectionDate.ForClub(CurrentClub.ClubId, false);
                foreach (var d in dates)
                    ddlDate.Items.Add(new ListItem(d.DisplayName, d.ClubCollectionDateId.ToString()));
                if (ddlDate.Items.FindByValue(Q("date")) != null)
                    ddlDate.SelectedValue = Q("date");
                else
                {
                    var next = dates.FirstOrDefault(d => d.CollectionDate >= Settings.TodayIrish);
                    if (next != null)
                        ddlDate.SelectedValue = next.ClubCollectionDateId.ToString();
                }
                Bind();
            }
        }

        private int SelectedDateId
        {
            get
            {
                int id;
                return int.TryParse(ddlDate.SelectedValue, out id) ? id : 0;
            }
        }

        private List<TreeOrder> DayOrders()
        {
            int id = SelectedDateId;
            if (id == 0)
                return new List<TreeOrder>();
            return TreeOrder.ForCollectionDate(id)
                .Where(o => o.ClubId == CurrentClub.ClubId && (o.IsOutstanding || o.StatusEnum == OrderStatus.Collected || o.StatusEnum == OrderStatus.NotOut))
                .ToList();
        }

        private void Bind()
        {
            var orders = DayOrders();
            btnPlan.Enabled = orders.Count > 0 && CurrentClub.Geocoded;
            if (orders.Count == 0)
            {
                litEmpty.Text = "<div class=\"card\"><p class=\"muted mb0\">No paid bookings for this day yet.</p></div>";
                rptRuns.DataSource = null;
                rptRuns.DataBind();
                MapJson = "null";
                return;
            }
            litEmpty.Text = orders.Any(o => o.RunNumber == 0)
                ? "<div class=\"msg warn no-print\">Some bookings haven't been planned into a run yet. Press <strong>Plan runs</strong>.</div>"
                : "";

            var runs = orders.GroupBy(o => o.RunNumber).OrderBy(g => g.Key == 0 ? int.MaxValue : g.Key).Select(g =>
            {
                var list = g.OrderBy(o => o.RunSequence).ToList();
                var links = g.Key == 0 ? new List<string>() : RoutePlanner.DirectionsLinks(CurrentClub, list);
                return new RunView
                {
                    Number = g.Key,
                    Colour = g.Key == 0 ? "#888" : Colours[(g.Key - 1) % Colours.Length],
                    Trees = list.Sum(o => o.Quantity),
                    Outstanding = list.Count(o => o.IsOutstanding),
                    Orders = list,
                    LinksHtml = string.Concat(links.Select((url, i) => "<a class=\"btn small light\" target=\"_blank\" rel=\"noopener\" href=\"" + HttpUtility.HtmlAttributeEncode(url) + "\">"
                                                                    + (links.Count == 1 ? "Directions" : "Directions " + (i + 1) + "/" + links.Count) + "</a>")),
                };
            }).ToList();
            rptRuns.DataSource = runs;
            rptRuns.DataBind();

            var colourOf = runs.ToDictionary(r => r.Number, r => r.Colour);
            MapJson = JsonConvert.SerializeObject(new
            {
                club = new { lat = CurrentClub.Latitude, lng = CurrentClub.Longitude, name = CurrentClub.ClubName },
                stops = orders.Select(o => new
                {
                    lat = o.Latitude,
                    lng = o.Longitude,
                    run = o.RunNumber,
                    seq = o.RunSequence,
                    colour = colourOf[o.RunNumber],
                    label = HttpUtility.HtmlEncode(o.CustomerName + ", " + o.AddressLine1 + " (" + o.Quantity + " x " + o.SizeLabel + ")"),
                }),
            }, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
        }

        protected void ddlDate_Changed(object sender, EventArgs e)
        {
            Bind();
        }

        protected void btnPlan_Click(object sender, EventArgs e)
        {
            int runs = RoutePlanner.Plan(CurrentClub, DayOrders());
            litMsg.Text = "<div class=\"msg ok\">Planned into " + runs + " run(s).</div>";
            Bind();
        }

        protected void rptRuns_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "collectrun")
                return;
            int run;
            int.TryParse(Convert.ToString(e.CommandArgument), out run);
            int count = 0;
            foreach (TreeOrder o in DayOrders().Where(o => o.RunNumber == run && o.IsOutstanding))
            {
                o.StatusEnum = OrderStatus.Collected;
                o.CollectedDate = DateTime.UtcNow;
                o.UpdatedBy = "CLB";
                o.Update();
                count++;
            }
            litMsg.Text = "<div class=\"msg ok\">Run " + run + ": " + count + " stop(s) marked collected.</div>";
            Bind();
        }
    }
}
