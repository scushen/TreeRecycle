using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Details : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                foreach (string t in Club.ClubTypes)
                    ddlType.Items.Add(t);
                foreach (string c in Geo.AllCounties)
                    ddlCounty.Items.Add(c);
                Bind();
            }
        }

        private void Bind()
        {
            Club c = CurrentClub;
            chkAccepting.Checked = c.AcceptingOrders;
            txtCloseDate.Text = c.OrdersCloseDate.HasValue ? c.OrdersCloseDate.Value.ToString("yyyy-MM-dd") : "";
            txtRadius.Text = c.CollectionRadiusKm.ToString();
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
            if (ddlCounty.Items.FindByValue(c.County) != null)
                ddlCounty.SelectedValue = c.County;
            txtEircode.Text = c.Eircode;
            txtContactName.Text = c.ContactName;
            txtPhone.Text = c.ContactPhone;
            txtEmail.Text = c.ContactEmail;
            litGeo.Text = c.Geocoded
                ? "Mapped at <a href=\"" + Geo.GoogleMapsLink(c.Latitude, c.Longitude) + "\" target=\"_blank\" rel=\"noopener\">this point</a>. Collections are measured from here."
                : "<strong style=\"color:#b8322a\">We couldn't place this Eircode on the map - please check it.</strong>";
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Club c = CurrentClub;
            var errors = new List<string>();

            int radius, capacity;
            if (!int.TryParse(txtRadius.Text, out radius) || radius < 1 || radius > 100) errors.Add("Collection radius must be between 1 and 100 km.");
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
            string eircode = Geo.NormaliseEircode(txtEircode.Text);
            if (eircode == "") errors.Add("Enter a valid Eircode.");

            if (errors.Count > 0)
            {
                litMsg.Text = "<div class=\"msg err\"><ul>" + string.Concat(errors.Select(x => "<li>" + Enc(x) + "</li>")) + "</ul></div>";
                return;
            }

            bool eircodeChanged = eircode != c.Eircode || !c.Geocoded;
            c.AcceptingOrders = chkAccepting.Checked;
            c.OrdersCloseDate = txtCloseDate.Text == "" ? (DateTime?)null : closeDate;
            c.CollectionRadiusKm = radius;
            c.TrailerCapacity = capacity;
            c.PublicDescription = Clean(txtDescription.Text, 2000);
            c.CollectionInfo = Clean(txtCollectionInfo.Text, 2000);
            c.ClubName = name;
            c.ClubType = ddlType.SelectedValue;
            c.RegistrationNumber = Clean(txtRegNo.Text, 50);
            c.Website = Clean(txtWebsite.Text, 250);
            c.AddressLine1 = Clean(txtAddress1.Text, 150);
            c.AddressLine2 = Clean(txtAddress2.Text, 150);
            c.Town = Clean(txtTown.Text, 100);
            c.County = ddlCounty.SelectedValue;
            c.Eircode = eircode;
            c.ContactName = Clean(txtContactName.Text, 100);
            c.ContactPhone = Clean(txtPhone.Text, 30);
            c.ContactEmail = email;
            c.UpdatedBy = "CLB";

            string warning = "";
            if (eircodeChanged)
            {
                GeoPoint p = null;
                try { p = Geo.Geocode(eircode); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("Geocode failed: " + ex); }
                c.Geocoded = p != null;
                if (p != null)
                {
                    c.Latitude = p.Latitude;
                    c.Longitude = p.Longitude;
                }
                else
                    warning = " But we couldn't place your Eircode on the map, so your page can't go live until it's fixed.";
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
