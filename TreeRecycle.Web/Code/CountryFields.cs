using System.Linq;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// The country-dependent inputs shared by registration and Club details: region (a dropdown where the country has
    /// a fixed list, free text in the UK) and time zone (only offered where a country has more than one).
    /// </summary>
    public static class CountryFields
    {
        public static void FillCountries(DropDownList ddl)
        {
            ddl.Items.Clear();
            foreach (CountryInfo c in Countries.All)
                ddl.Items.Add(new ListItem(c.Name, c.Code));
        }

        public static void BindRegion(DropDownList ddl, TextBox txt, CountryInfo country, string selected)
        {
            ddl.Items.Clear();
            ddl.Visible = country.HasRegionList;
            txt.Visible = !country.HasRegionList;
            if (country.HasRegionList)
            {
                ddl.Items.Add(new ListItem("Choose", ""));
                foreach (string r in country.Regions)
                    ddl.Items.Add(r);
                string match = country.MatchRegion(selected);
                if (match != "" && ddl.Items.FindByValue(match) != null)
                    ddl.SelectedValue = match;
            }
            else
                txt.Text = selected ?? "";
        }

        public static string ReadRegion(DropDownList ddl, TextBox txt, CountryInfo country)
        {
            return country.HasRegionList ? ddl.SelectedValue : (txt.Text ?? "").Trim();
        }

        /// <summary>Fills the time-zone list; hides the whole field (wrapper) when the country has just one zone.</summary>
        public static void BindTimeZones(DropDownList ddl, System.Web.UI.Control wrapper, CountryInfo country, string selected)
        {
            ddl.Items.Clear();
            foreach (var tz in country.TimeZones)
                ddl.Items.Add(new ListItem(tz.Value, tz.Key));
            ddl.SelectedValue = country.TimeZones.Any(t => t.Key == selected) ? selected : country.DefaultTimeZone;
            wrapper.Visible = country.TimeZones.Length > 1;
        }

        public static string ReadTimeZone(DropDownList ddl, CountryInfo country)
        {
            return country.TimeZones.Any(t => t.Key == ddl.SelectedValue) ? ddl.SelectedValue : country.DefaultTimeZone;
        }
    }
}
