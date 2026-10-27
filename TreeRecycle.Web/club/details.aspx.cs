using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Details : ClubPage
    {
        protected string RegionInputId
        {
            get { return Country.HasRegionList ? ddlRegion.ClientID : txtRegion.ClientID; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                foreach (string t in Club.ClubTypes)
                    ddlType.Items.Add(t);
                Bind();
            }
        }

        private void Bind()
        {
            Club c = CurrentClub;
            CountryInfo country = c.Country;
            chkAccepting.Checked = c.AcceptingOrders;
            txtCloseDate.Text = c.OrdersCloseDate.HasValue ? c.OrdersCloseDate.Value.ToString("yyyy-MM-dd") : "";
            litRadiusUnit.Text = country.DistanceUnit;
            txtRadius.Text = c.RadiusInDisplayUnits.ToString();
            txtCapacity.Text = c.TrailerCapacity.ToString();
            txtDescription.Text = c.PublicDescription;
            txtCollectionInfo.Text = c.CollectionInfo;
            txtName.Text = c.ClubName;
            if (ddlType.Items.FindByValue(c.ClubType) == null)
                ddlType.Items.Add(c.ClubType);
            ddlType.SelectedValue = c.ClubType;
            txtRegNo.Text = c.RegistrationNumber;
            txtWebsite.Text = c.Website;
            txtAddress1.Text = c.AddressLine1;
            txtAddress2.Text = c.AddressLine2;
            txtTown.Text = c.Town;
            litRegionLabel.Text = Enc(country.RegionName);
            CountryFields.BindRegion(ddlRegion, txtRegion, country, c.Region);
            litPostcodeLabel.Text = Enc(char.ToUpperInvariant(country.PostcodeName[0]) + country.PostcodeName.Substring(1));
            txtPostcode.Text = c.Postcode;
            litCountry.Text = Enc(country.Name + " (" + country.Currency.ToUpperInvariant() + ")");
            CountryFields.BindTimeZones(ddlTimeZone, phTimeZone, country, c.TimeZone);
            txtContactName.Text = c.ContactName;
            txtPhone.Text = c.ContactPhone;
            txtEmail.Text = c.ContactEmail;
            litGeo.Text = c.Geocoded
                ? "Mapped at <a href=\"" + Geo.GoogleMapsLink(c.Latitude, c.Longitude) + "\" target=\"_blank\" rel=\"noopener\">this point</a>. Collections are measured from here."
                : "<strong style=\"color:#b8322a\">We couldn't place this address on the map - please check it.</strong>";
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Club c = CurrentClub;
            CountryInfo country = c.Country;
            var errors = new List<string>();

            int radius, capacity;
            if (!int.TryParse(txtRadius.Text, out radius) || radius < 1 || radius > 100) errors.Add("Collection radius must be between 1 and 100 " + country.DistanceUnit + ".");
            if (!int.TryParse(txtCapacity.Text, out capacity) || capacity < 1 || capacity > 500) errors.Add("Trees per load must be between 1 and 500.");
            DateTime closeDate = DateTime.MinValue;
            if (txtCloseDate.Text != "" && !DateTime.TryParseExact(txtCloseDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out closeDate))
                errors.Add("Enter a valid closing date.");
            string name = Clean(txtName.Text, 150);
            if (name.Length < 3) errors.Add("Enter your club's name.");
            string email = Clean(txtEmail.Text, 200).ToLowerInvariant();
            if (!LooksLikeEmail(email)) errors.Add("Enter a valid email.");
            else
            {
                Club other = Club.FindByEmail(email);
                if (other != null && other.ClubId != c.ClubId) errors.Add("That email is used by another club.");
            }
            string postcode = country.NormalisePostcode(txtPostcode.Text);
            if (postcode == "") errors.Add("Enter a valid " + country.PostcodeName + ", like " + country.PostcodeExample + ".");
            string region = Clean(CountryFields.ReadRegion(ddlRegion, txtRegion, country), 100);
            if (region == "") errors.Add("Enter your " + country.RegionName.ToLowerInvariant() + ".");

            if (errors.Count > 0)
            {
                litMsg.Text = "<div class=\"msg err\"><ul>" + string.Concat(errors.Select(x => "<li>" + Enc(x) + "</li>")) + "</ul></div>";
                return;
            }

            string address1 = Clean(txtAddress1.Text, 150), town = Clean(txtTown.Text, 100);
            // Outside Ireland the street address matters for the map point, not just the postcode.
            bool locationChanged = postcode != c.Postcode || !c.Geocoded
                                   || (!country.PostcodeIsExact && (address1 != c.AddressLine1 || town != c.Town));
            c.AcceptingOrders = chkAccepting.Checked;
            c.OrdersCloseDate = txtCloseDate.Text == "" ? (DateTime?)null : closeDate;
            c.CollectionRadiusKm = (int)Math.Round(country.FromDisplayUnits(radius));
            c.TrailerCapacity = capacity;
            c.PublicDescription = Clean(txtDescription.Text, 2000);
            c.CollectionInfo = Clean(txtCollectionInfo.Text, 2000);
            c.ClubName = name;
            c.ClubType = ddlType.SelectedValue;
            c.RegistrationNumber = Clean(txtRegNo.Text, 50);
            c.Website = Clean(txtWebsite.Text, 250);
            c.AddressLine1 = address1;
            c.AddressLine2 = Clean(txtAddress2.Text, 150);
            c.Town = town;
            c.Region = region;
            c.Postcode = postcode;
            c.TimeZone = CountryFields.ReadTimeZone(ddlTimeZone, country);
            c.ContactName = Clean(txtContactName.Text, 100);
            c.ContactPhone = Clean(txtPhone.Text, 30);
            c.ContactEmail = email;
            c.UpdatedBy = "CLB";

            string warning = "";
            if (locationChanged)
            {
                GeoPoint p = null;
                try { p = Geo.GeocodeAddress(address1, town, postcode, country.Code); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("Geocode failed: " + ex); }
                c.Geocoded = p != null;
                if (p != null)
                {
                    c.Latitude = p.Latitude;
                    c.Longitude = p.Longitude;
                }
                else
                    warning = " But we couldn't place your address on the map, so your page can't go live until it's fixed.";
            }
            c.Update();
            Bind();
            litMsg.Text = "<div class=\"msg " + (warning == "" ? "ok" : "warn") + "\">Saved." + Enc(warning) + "</div>";
        }

        protected void btnPassword_Click(object sender, EventArgs e)
        {
            if ((txtNewPassword.Text ?? "").Length < 10)
            {
                litMsg.Text = "<div class=\"msg err\">Use at least 10 characters for your password.</div>";
                return;
            }
            if (txtNewPassword.Text != txtNewPassword2.Text)
            {
                litMsg.Text = "<div class=\"msg err\">The passwords don't match.</div>";
                return;
            }
            CurrentClub.PasswordHash = Passwords.Hash(txtNewPassword.Text);
            CurrentClub.Update();
            litMsg.Text = "<div class=\"msg ok\">Password changed.</div>";
        }
    }
}
