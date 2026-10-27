using System;
using System.Collections.Generic;
using System.Linq;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class Register : BasePage
    {
        protected string RegionInputId
        {
            get { return Country.HasRegionList ? ddlRegion.ClientID : txtRegion.ClientID; }
        }

        private CountryInfo Country { get { return Countries.Get(ddlCountry.SelectedValue); } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                foreach (string t in Club.ClubTypes)
                    ddlType.Items.Add(t);
                CountryFields.FillCountries(ddlCountry);
                string start = Countries.IsSupported(Q("country")) ? Q("country").ToUpperInvariant()
                    : (Countries.FromHost(Request.Url.Host) ?? Countries.DefaultCode);
                ddlCountry.SelectedValue = start;
                BindCountry();
            }
        }

        protected void ddlCountry_Changed(object sender, EventArgs e)
        {
            BindCountry();
        }

        private void BindCountry()
        {
            CountryInfo c = Country;
            litFee.Text = c.Money(c.BookingFee);
            litCurrency.Text = Enc(c.Currency.ToUpperInvariant());
            litRegionLabel.Text = Enc(c.RegionName);
            litPostcodeLabel.Text = Enc(char.ToUpperInvariant(c.PostcodeName[0]) + c.PostcodeName.Substring(1));
            txtPostcode.Attributes["placeholder"] = "e.g. " + c.PostcodeExample;
            litPostcodeHint.Text = Enc("Collections are measured from this address, so use where your trailer sets out from.");
            CountryFields.BindRegion(ddlRegion, txtRegion, c, "");
            CountryFields.BindTimeZones(ddlTimeZone, phTimeZone, c, null);
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            CountryInfo country = Country;
            var errors = new List<string>();
            string name = Clean(txtClubName.Text, 150);
            string email = Clean(txtEmail.Text, 200).ToLowerInvariant();
            string postcode = country.NormalisePostcode(txtPostcode.Text);
            string region = Clean(CountryFields.ReadRegion(ddlRegion, txtRegion, country), 100);

            if (name.Length < 3) errors.Add("Enter your club's name.");
            if (!LooksLikeEmail(email)) errors.Add("Enter a valid email address.");
            else if (Club.FindByEmail(email) != null) errors.Add("That email is already registered. Try logging in, or reset your password.");
            if (Clean(txtContactName.Text, 100) == "") errors.Add("Enter a contact name.");
            if (Clean(txtPhone.Text, 30).Length < 7) errors.Add("Enter a contact phone number.");
            if (Clean(txtAddress1.Text, 150) == "" || Clean(txtTown.Text, 100) == "") errors.Add("Enter the club's address and town.");
            if (region == "") errors.Add("Enter your " + country.RegionName.ToLowerInvariant() + ".");
            if (postcode == "") errors.Add("Enter a valid " + country.PostcodeName + ", like " + country.PostcodeExample + ".");
            if ((txtPassword.Text ?? "").Length < 10) errors.Add("Choose a password of at least 10 characters.");
            else if (txtPassword.Text != txtPassword2.Text) errors.Add("The passwords don't match.");
            if (!chkTerms.Checked) errors.Add("Please confirm you're authorised to register this organisation.");

            if (errors.Count > 0)
            {
                litError.Text = "<div class=\"msg err\"><strong>Please check the following:</strong><ul>" + string.Concat(errors.Select(x => "<li>" + Enc(x) + "</li>")) + "</ul></div>";
                return;
            }

            var club = new Club
            {
                ClubName = name,
                Slug = Club.MakeUniqueSlug(name, 0),
                ClubType = ddlType.SelectedValue,
                RegistrationNumber = Clean(txtRegNo.Text, 50),
                Website = Clean(txtWebsite.Text, 250),
                ContactName = Clean(txtContactName.Text, 100),
                ContactEmail = email,
                ContactPhone = Clean(txtPhone.Text, 30),
                AddressLine1 = Clean(txtAddress1.Text, 150),
                AddressLine2 = Clean(txtAddress2.Text, 150),
                Town = Clean(txtTown.Text, 100),
                Region = region,
                Postcode = postcode,
                CountryCode = country.Code,
                TimeZone = CountryFields.ReadTimeZone(ddlTimeZone, country),
                // 20 km in Ireland, Australia and New Zealand; 12 miles (stored as km) in the UK and US.
                CollectionRadiusKm = (int)Math.Round(country.FromDisplayUnits(country.UsesMiles ? 12 : 20)),
                TrailerCapacity = 25,
                StatusEnum = ClubStatus.Pending,
                PasswordHash = Passwords.Hash(txtPassword.Text),
                CreatedBy = "WEB",
                UpdatedBy = "WEB",
            };

            try
            {
                GeoPoint point = Geo.GeocodeAddress(club.AddressLine1, club.Town, postcode, country.Code);
                if (point != null)
                {
                    club.Latitude = point.Latitude;
                    club.Longitude = point.Longitude;
                    club.Geocoded = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Geocode at registration failed: " + ex);
            }

            club.Create();
            ClubPrice.CreateDefaults(club.ClubId);
            ClubPage.SignIn(Session, club);

            Mailer.SendSystem(club.ContactEmail, "We've received your registration",
                "Hi " + club.ContactName + ",\n\nThanks for registering " + club.ClubName + " with " + Settings.SiteName + ".\n\n"
                + "We verify every club before it can take payments. We'll email you once that's done, usually within one working day.\n\n"
                + "Meanwhile you can log in and set up your prices, collection dates and emails:\n" + AbsoluteUrl("~/login.aspx") + "\n\nThanks,\n" + Settings.SiteName, club.ClubId);

            if (Settings.AdminNotifyEmail != "")
                Mailer.SendSystem(Settings.AdminNotifyEmail, "New club to verify: " + club.ClubName + " (" + country.Name + ")",
                    club.ClubName + " (" + club.ClubType + ")\n" + club.FullAddress + ", " + country.Name + "\n" + club.ContactName + " - " + club.ContactPhone + " - " + club.ContactEmail
                    + "\nReg no: " + club.RegistrationNumber + "\nWebsite: " + club.Website + "\n\nReview: " + AbsoluteUrl("~/admin/club.aspx") + "?id=" + club.ClubId, club.ClubId);

            phForm.Visible = false;
            phDone.Visible = true;
        }
    }
}
