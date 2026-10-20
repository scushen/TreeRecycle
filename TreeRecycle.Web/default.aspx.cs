using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class _Default : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ddlProvince.Items.Add(new ListItem("All provinces", ""));
                foreach (string p in Geo.Provinces)
                    ddlProvince.Items.Add(new ListItem(p, p));
                BindCounties();
                BindTowns();
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            string eircode = Geo.NormaliseEircode(txtEircode.Text);
            phResults.Visible = false;
            if (eircode == string.Empty)
            {
                litSearchMsg.Text = "<div class=\"msg err\">That doesn't look like an Eircode. It's 7 characters, like <strong>A65 F4E2</strong>. You can look yours up at <a href=\"https://finder.eircode.ie\" target=\"_blank\" rel=\"noopener\">finder.eircode.ie</a>.</div>";
                return;
            }
            txtEircode.Text = eircode;

            GeoPoint point;
            try
            {
                point = Geo.Geocode(eircode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Geocode failed: " + ex);
                point = null;
            }
            if (point == null)
            {
                litSearchMsg.Text = "<div class=\"msg err\">We couldn't find " + Enc(eircode) + " on the map. Please check it, or browse by county below.</div>";
                return;
            }

            var clubs = Club.FindCovering(point.Latitude, point.Longitude);
            phResults.Visible = true;
            if (clubs.Count == 0)
            {
                litResultsTitle.Text = "No clubs collect at " + Enc(eircode) + " yet";
                litSearchMsg.Text = "<div class=\"msg warn\">Sorry, none of our clubs cover your address yet. Try browsing your county below - a club nearby may still be able to help. Know a local club that would like to fundraise? <a href=\"" + ResolveUrl("~/register.aspx") + "\">Send them our way</a>.</div>";
                rptResults.DataSource = null;
            }
            else
            {
                litSearchMsg.Text = "";
                litResultsTitle.Text = clubs.Count == 1 ? "1 club collects at " + Enc(eircode) : clubs.Count + " clubs collect at " + Enc(eircode);
                rptResults.DataSource = clubs.Select(c => ClubResult.From(c, eircode, c.DistanceKmTo(point.Latitude, point.Longitude))).ToList();
            }
            rptResults.DataBind();

            if (!string.IsNullOrEmpty(point.County))
            {
                ddlProvince.SelectedValue = Geo.ProvinceOf(point.County);
                BindCounties();
                ddlCounty.SelectedValue = point.County;
                BindTowns();
            }
        }

        protected void ddlProvince_Changed(object sender, EventArgs e)
        {
            BindCounties();
            BindTowns();
        }

        protected void ddlCounty_Changed(object sender, EventArgs e)
        {
            BindTowns();
        }

        protected void ddlTown_Changed(object sender, EventArgs e)
        {
            BindBrowse();
        }

        private void BindCounties()
        {
            ddlCounty.Items.Clear();
            ddlCounty.Items.Add(new ListItem("Choose county", ""));
            foreach (string c in Geo.CountiesIn(ddlProvince.SelectedValue))
                ddlCounty.Items.Add(new ListItem(c, c));
        }

        private void BindTowns()
        {
            ddlTown.Items.Clear();
            ddlTown.Items.Add(new ListItem("All towns", ""));
            if (ddlCounty.SelectedValue != "")
            {
                foreach (string town in Club.FindOrderable(ddlCounty.SelectedValue).Select(c => c.Town).Where(t => t != "")
                                            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t))
                    ddlTown.Items.Add(new ListItem(town, town));
            }
            BindBrowse();
        }

        private void BindBrowse()
        {
            litBrowseMsg.Text = "";
            phBrowse.Visible = false;
            if (ddlCounty.SelectedValue == "")
                return;
            var clubs = Club.FindOrderable(ddlCounty.SelectedValue)
                            .Where(c => ddlTown.SelectedValue == "" || c.Town.Equals(ddlTown.SelectedValue, StringComparison.OrdinalIgnoreCase))
                            .ToList();
            if (clubs.Count == 0)
            {
                litBrowseMsg.Text = "<p class=\"muted\">No clubs are taking bookings in Co. " + Enc(ddlCounty.SelectedValue) + " yet.</p>";
                return;
            }
            phBrowse.Visible = true;
            rptBrowse.DataSource = clubs.Select(c => ClubResult.From(c, null, null)).ToList();
            rptBrowse.DataBind();
        }
    }
}
