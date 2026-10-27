using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class _Default : BasePage
    {
        private const string CountryCookie = "tr_country";

        /// <summary>
        /// 
        /// </summary>
        protected string PostcodeHintsJson
        {
            get
            {
                return JsonConvert.SerializeObject(Countries.All.ToDictionary(c => c.Code,
                    c => "Your " + c.PostcodeName + ", e.g. " + c.PostcodeExample), new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                foreach (CountryInfo c in Countries.All)
                {
                    ddlCountry.Items.Add(new ListItem(c.Name, c.Code));
                    ddlBrowseCountry.Items.Add(new ListItem(c.Name, c.Code));
                }
                string start = StartingCountry();
                ddlCountry.SelectedValue = start;
                ddlBrowseCountry.SelectedValue = start;
                BindRegions();
            }
        }

        /// <summary>?country=GB, then the visitor's last choice, then the domain (.ie / .co.uk / .com.au ...), then Ireland.</summary>
        private string StartingCountry()
        {
            if (Countries.IsSupported(Q("country")))
                return Q("country").ToUpperInvariant();
            HttpCookie cookie = Request.Cookies[CountryCookie];
            if (cookie != null && Countries.IsSupported(cookie.Value))
                return cookie.Value.ToUpperInvariant();
            return Countries.FromHost(Request.Url.Host) ?? Countries.DefaultCode;
        }

        private void RememberCountry(string code)
        {
            Response.Cookies.Add(new HttpCookie(CountryCookie, code) { Expires = DateTime.UtcNow.AddYears(1), HttpOnly = true, SameSite = SameSiteMode.Lax });
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            CountryInfo country = Countries.Get(ddlCountry.SelectedValue);
            RememberCountry(country.Code);
            string postcode = country.NormalisePostcode(txtPostcode.Text);
            phResults.Visible = false;
            if (postcode == string.Empty)
            {
                string help = country.Code == "IE"
                    ? " You can look yours up at <a href=\"https://finder.eircode.ie\" target=\"_blank\" rel=\"noopener\">finder.eircode.ie</a>."
                    : "";
                litSearchMsg.Text = "<div class=\"msg err\">That doesn't look like a " + Enc(country.Name) + " " + Enc(country.PostcodeName)
                                    + ". It should look like <strong>" + Enc(country.PostcodeExample) + "</strong>." + help + "</div>";
                return;
            }
            txtPostcode.Text = postcode;

            GeoPoint point;
            try
            {
                point = Geo.Geocode(postcode, country.Code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Geocode failed: " + ex);
                point = null;
            }
            if (point == null)
            {
                litSearchMsg.Text = "<div class=\"msg err\">We couldn't find " + Enc(postcode) + " on the map. Please check it, or browse by area below.</div>";
                return;
            }

            var clubs = Club.FindCovering(point.Latitude, point.Longitude, country.Code);
            phResults.Visible = true;
            if (clubs.Count == 0)
            {
                litResultsTitle.Text = "No clubs collect at " + Enc(postcode) + " yet";
                litSearchMsg.Text = "<div class=\"msg warn\">Sorry, none of our clubs cover your address yet. Try browsing your area below - a club nearby may still be able to help. Know a local club that would like to run a collection? <a href=\"" + ResolveUrl("~/register.aspx") + "\">Send them our way</a>.</div>";
                rptResults.DataSource = null;
            }
            else
            {
                litSearchMsg.Text = "";
                litResultsTitle.Text = clubs.Count == 1 ? "1 club collects at " + Enc(postcode) : clubs.Count + " clubs collect at " + Enc(postcode);
                rptResults.DataSource = clubs.Select(c => ClubResult.From(c, postcode, c.DistanceKmTo(point.Latitude, point.Longitude))).ToList();
            }
            rptResults.DataBind();

            // Line the browse lists up with where they searched.
            ddlBrowseCountry.SelectedValue = country.Code;
            BindRegions();
            if (point.Region != "" && ddlRegion.Items.FindByValue(point.Region) != null)
            {
                ddlRegion.SelectedValue = point.Region;
                BindTowns();
            }
        }

        protected void ddlBrowseCountry_Changed(object sender, EventArgs e)
        {
            RememberCountry(ddlBrowseCountry.SelectedValue);
            ddlCountry.SelectedValue = ddlBrowseCountry.SelectedValue;
            BindRegions();
        }

        protected void ddlRegion_Changed(object sender, EventArgs e)
        {
            BindTowns();
        }

        protected void ddlTown_Changed(object sender, EventArgs e)
        {
            BindBrowse();
        }

        /// <summary>Regions that actually have a live club, so nobody browses into an empty county or state.</summary>
        private void BindRegions()
        {
            CountryInfo country = Countries.Get(ddlBrowseCountry.SelectedValue);
            litRegionLabel.Text = Enc(country.RegionName);
            ddlRegion.Items.Clear();
            ddlRegion.Items.Add(new ListItem("Choose " + country.RegionName.ToLowerInvariant(), ""));
            foreach (string region in Club.FindOrderable(country.Code, null).Select(c => c.Region).Where(r => r != "")
                                          .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(r => r))
                ddlRegion.Items.Add(new ListItem(region, region));
            BindTowns();
        }

        private void BindTowns()
        {
            ddlTown.Items.Clear();
            ddlTown.Items.Add(new ListItem("All towns", ""));
            if (ddlRegion.SelectedValue != "")
            {
                foreach (string town in Club.FindOrderable(ddlBrowseCountry.SelectedValue, ddlRegion.SelectedValue).Select(c => c.Town).Where(t => t != "")
                                            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t))
                    ddlTown.Items.Add(new ListItem(town, town));
            }
            BindBrowse();
        }

        private void BindBrowse()
        {
            litBrowseMsg.Text = "";
            phBrowse.Visible = false;
            CountryInfo country = Countries.Get(ddlBrowseCountry.SelectedValue);
            if (ddlRegion.Items.Count <= 1)
            {
                litBrowseMsg.Text = "<p class=\"muted\">No clubs are taking bookings in " + Enc(country.Name) + " yet.</p>";
                return;
            }
            if (ddlRegion.SelectedValue == "")
                return;
            var clubs = Club.FindOrderable(country.Code, ddlRegion.SelectedValue)
                            .Where(c => ddlTown.SelectedValue == "" || c.Town.Equals(ddlTown.SelectedValue, StringComparison.OrdinalIgnoreCase))
                            .ToList();
            phBrowse.Visible = clubs.Count > 0;
            rptBrowse.DataSource = clubs.Select(c => ClubResult.From(c, null, null)).ToList();
            rptBrowse.DataBind();
        }
    }
}
