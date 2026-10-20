using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>A club's public booking page: /c/{slug} (or club.aspx?slug=).</summary>
    public partial class ClubOrder : BasePage
    {
        private Club _club;
        private IList<ClubPrice> _prices;
        private IList<ClubCollectionDate> _dates;

        protected int SelectedPriceId { get; private set; }

        protected string FeePerTreeJs
        {
            get { return Settings.PlatformFeePerTree.ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>The club's own login can preview its page before it goes live, but not pay.</summary>
        private bool IsOwnerPreview
        {
            get { return Session[ClubPage.SessionKey] is int && (int)Session[ClubPage.SessionKey] == _club.ClubId; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string slug = Page.RouteData.Values["slug"] as string ?? Q("slug");
            _club = Club.FindBySlug(slug);
            if (_club == null || (_club.StatusEnum != ClubStatus.Approved && !(Session[ClubPage.SessionKey] is int && (int)Session[ClubPage.SessionKey] == _club.ClubId)))
            {
                Response.StatusCode = 404;
                phClub.Visible = false;
                phNotFound.Visible = true;
                litClubName.Text = "Club not found";
                return;
            }

            _prices = _club.ActivePrices;
            _dates = _club.UpcomingDates;

            int posted;
            SelectedPriceId = int.TryParse(Request.Form["size"], out posted) ? posted : (_prices.Count == 1 ? _prices[0].ClubPriceId : 0);

            Title = "Christmas tree collection - " + _club.ClubName;
            litMeta.Text = "<meta name=\"description\" content=\"" + Enc("Book " + _club.ClubName + " to collect your Christmas tree. Support the club and skip the mess.") + "\" />";
            litClubName.Text = Enc(_club.ClubName) + " Christmas tree collection";
            litPlace.Text = Enc((_club.Town == "" ? "" : _club.Town + ", ") + "Co. " + _club.County) + " &middot; every booking supports the club";
            litPayee.Text = Enc(_club.ClubName);
            litRadius.Text = _club.CollectionRadiusKm.ToString(CultureInfo.InvariantCulture);
            litDescription.Text = Paragraphs(_club.PublicDescription);
            litCollectionInfo.Text = Paragraphs(_club.CollectionInfo);
            litContact.Text = Enc(_club.ContactName) + "<br />" +
                              "<a href=\"tel:" + Enc(_club.ContactPhone.Replace(" ", "")) + "\">" + Enc(_club.ContactPhone) + "</a><br />" +
                              "<a href=\"mailto:" + Enc(_club.ContactEmail) + "\">" + Enc(_club.ContactEmail) + "</a>";
            litDates.Text = _dates.Count == 0 ? "<p class=\"muted\">To be announced.</p>"
                : "<ul>" + string.Concat(_dates.Select(d => "<li>" + Enc(d.DisplayName) + "</li>")) + "</ul>";

            rptSizes.DataSource = _prices;
            rptSizes.DataBind();

            if (!IsPostBack)
            {
                foreach (ClubCollectionDate d in _dates)
                {
                    var item = new ListItem(d.DisplayName, d.ClubCollectionDateId.ToString());
                    if (d.IsFull(1))
                    {
                        item.Text += " - fully booked";
                        item.Enabled = false;
                    }
                    ddlDate.Items.Add(item);
                }
                txtEircode.Text = Geo.NormaliseEircode(Q("eircode"));
            }

            IList<string> blockers = _club.OrderBlockers();
            if (blockers.Count > 0)
            {
                btnPay.Enabled = false;
                litClosed.Text = IsOwnerPreview
                    ? "<div class=\"msg warn\"><strong>Preview.</strong> This is how your page will look. It isn't live yet:<ul>"
                      + string.Concat(blockers.Select(b => "<li>" + Enc(b) + "</li>")) + "</ul></div>"
                    : "<div class=\"msg warn\">" + Enc(_club.ClubName) + " isn't taking bookings at the moment.</div>";
            }
        }

        protected void btnPay_Click(object sender, EventArgs e)
        {
            if (_club == null || !_club.IsOrderable)
                return;

            var errors = new List<string>();
            ClubPrice price = _prices.FirstOrDefault(p => p.ClubPriceId == SelectedPriceId);
            if (price == null)
                errors.Add("Choose a tree size.");

            int qty;
            if (!int.TryParse(ddlQty.SelectedValue, out qty) || qty < 1 || qty > 5)
                qty = 1;

            int dateId;
            int.TryParse(ddlDate.SelectedValue, out dateId);
            ClubCollectionDate date = _dates.FirstOrDefault(d => d.ClubCollectionDateId == dateId);
            if (date == null)
                errors.Add("Choose a collection day.");
            else if (date.IsFull(qty))
                errors.Add("Sorry, " + date.DisplayName + " is fully booked.");

            string eircode = Geo.NormaliseEircode(txtEircode.Text);
            if (eircode == string.Empty)
                errors.Add("Enter a valid Eircode, like A65 F4E2.");
            string name = Clean(txtName.Text, 100), email = Clean(txtEmail.Text, 200).ToLowerInvariant(), phone = Clean(txtPhone.Text, 30);
            if (name == "") errors.Add("Enter your name.");
            if (!LooksLikeEmail(email)) errors.Add("Enter a valid email address.");
            if (phone.Length < 7) errors.Add("Enter a mobile number so the crew can reach you on the day.");
            if (Clean(txtAddress1.Text, 150) == "") errors.Add("Enter your address.");
            if (Clean(txtTown.Text, 100) == "") errors.Add("Enter your town or area.");
            if (!chkTerms.Checked) errors.Add("Please tick to confirm you'll have the tree out on the day.");

            GeoPoint point = null;
            if (eircode != string.Empty && errors.Count == 0)
            {
                try { point = Geo.Geocode(eircode); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError("Geocode failed: " + ex); }
                if (point == null)
                    errors.Add("We couldn't find " + eircode + " on the map. Please check your Eircode.");
                else if (!_club.Covers(point.Latitude, point.Longitude))
                {
                    double km = _club.DistanceKmTo(point.Latitude, point.Longitude);
                    errors.Add(string.Format(CultureInfo.InvariantCulture,
                        "Sorry, {0} is {1:0.0} km from {2}, outside their {3} km collection area. Search your Eircode on our home page to find a club that covers you.",
                        eircode, km, _club.ClubName, _club.CollectionRadiusKm));
                }
            }

            if (errors.Count > 0)
            {
                ShowErrors(errors);
                return;
            }

            decimal fee = Settings.PlatformFeePerTree;
            var order = new TreeOrder
            {
                OrderRef = TreeOrder.NewOrderRef(),
                ClubId = _club.ClubId,
                ClubPriceId = price.ClubPriceId,
                ClubCollectionDateId = date.ClubCollectionDateId,
                SizeLabel = price.Label,
                Quantity = qty,
                UnitPrice = price.Price,
                PlatformFeePerTree = fee,
                ClubAmount = price.Price * qty,
                PlatformFee = fee * qty,
                TotalAmount = (price.Price + fee) * qty,
                Currency = "eur",
                CustomerName = name,
                CustomerEmail = email,
                CustomerPhone = phone,
                AddressLine1 = Clean(txtAddress1.Text, 150),
                AddressLine2 = Clean(txtAddress2.Text, 150),
                Town = Clean(txtTown.Text, 100),
                County = point.County,
                Eircode = eircode,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                DistanceKm = Math.Round((decimal)_club.DistanceKmTo(point.Latitude, point.Longitude), 2),
                LocationNotes = Clean(txtNotes.Text, 500),
                StatusEnum = OrderStatus.PendingPayment,
                CreatedBy = "WEB",
                UpdatedBy = "WEB",
            };
            order.Create();

            string success = Settings.SiteUrl + VirtualPathUtility.ToAbsolute("~/order_complete.aspx") + "?ref=" + order.OrderRef + "&session_id={CHECKOUT_SESSION_ID}";
            string cancel = _club.PublicUrl + "?eircode=" + HttpUtility.UrlEncode(eircode);
            string checkoutUrl;
            try
            {
                checkoutUrl = Payments.CreateCheckout(order, _club, success, cancel);
            }
            catch (Stripe.StripeException ex)
            {
                System.Diagnostics.Trace.TraceError("Checkout create failed for " + order.OrderRef + ": " + ex);
                ShowErrors(new[] { "We couldn't start the payment just now. Please try again in a minute." });
                return;
            }
            Go(checkoutUrl);
        }

        private void ShowErrors(IEnumerable<string> errors)
        {
            litError.Text = "<div class=\"msg err\"><strong>Please check the following:</strong><ul>"
                            + string.Concat(errors.Select(x => "<li>" + Enc(x) + "</li>")) + "</ul></div>";
        }

        private static string Paragraphs(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            var sb = new StringBuilder();
            foreach (string para in text.Replace("\r\n", "\n").Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
                sb.Append("<p>").Append(HttpUtility.HtmlEncode(para.Trim()).Replace("\n", "<br />")).Append("</p>");
            return sb.ToString();
        }
    }
}
