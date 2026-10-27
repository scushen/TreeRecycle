using System;
using System.Linq;
using System.Text;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.Admin
{
    public partial class ClubReview : AdminPage
    {
        private Club _club;

        protected void Page_Load(object sender, EventArgs e)
        {
            _club = Club.FindById(QInt("id"));
            if (_club == null)
            {
                Go("~/admin/");
                return;
            }
            if (!IsPostBack)
            {
                txtReason.Text = _club.StatusReason;
                txtNotes.Text = _club.AdminNotes;
            }
            Bind();
        }

        private void Bind()
        {
            Club c = _club;
            Title = c.ClubName;
            litName.Text = Enc(c.ClubName);
            string pill = c.StatusEnum == ClubStatus.Approved ? "" : c.StatusEnum == ClubStatus.Pending ? "gold" : "red";
            litStatus.Text = "<span class=\"pill " + pill + "\" style=\"vertical-align:middle;font-family:Inter,sans-serif\">" + c.StatusEnum + "</span>";

            var sb = new StringBuilder();
            Row(sb, "Country", Enc(c.Country.Name + " (" + c.Country.Currency.ToUpperInvariant() + ")"));
            Row(sb, "Type", Enc(c.ClubType));
            Row(sb, "Registration no.", Enc(c.RegistrationNumber == "" ? "-" : c.RegistrationNumber));
            Row(sb, "Website", c.Website == "" ? "-" : "<a href=\"" + Enc(c.Website.StartsWith("http") ? c.Website : "https://" + c.Website) + "\" target=\"_blank\" rel=\"noopener nofollow\">" + Enc(c.Website) + "</a>");
            Row(sb, "Address", Enc(c.FullAddress) + (c.Geocoded
                ? "<div class=\"small\"><a href=\"" + Geo.GoogleMapsLink(c.Latitude, c.Longitude) + "\" target=\"_blank\" rel=\"noopener\">See on map</a></div>"
                : "<div class=\"small\" style=\"color:#b8322a\">Address not found on the map</div>"));
            Row(sb, "Contact", Enc(c.ContactName));
            Row(sb, "Phone", "<a href=\"tel:" + Enc(c.ContactPhone.Replace(" ", "")) + "\">" + Enc(c.ContactPhone) + "</a>");
            Row(sb, "Email", "<a href=\"mailto:" + Enc(c.ContactEmail) + "\">" + Enc(c.ContactEmail) + "</a>");
            Row(sb, "Registered", AdminDate(c.CreatedDate, "d MMM yyyy HH:mm"));
            Row(sb, "Approved", c.ApprovedDate.HasValue ? AdminDate(c.ApprovedDate, "d MMM yyyy") : "-");
            Row(sb, "Last login", c.LastLoginDate.HasValue ? AdminDate(c.LastLoginDate, "d MMM yyyy HH:mm") : "Never");
            litDetails.Text = sb.ToString();

            var orders = TreeOrder.ForClub(c.ClubId);
            var sales = orders.Where(o => o.IsSale).ToList();
            var act = new StringBuilder();
            Row(act, "Stripe", c.StripeChargesEnabled ? "Taking payments" : c.StripeAccountId == "" ? "Not connected" : "Onboarding not finished");
            Row(act, "Stripe account", Enc(c.StripeAccountId == "" ? "-" : c.StripeAccountId));
            Row(act, "Booking page", c.IsOrderable ? "Live" : "Not live: " + Enc(string.Join(" ", c.OrderBlockers())));
            Row(act, "Trees booked", sales.Sum(o => o.Quantity).ToString());
            Row(act, "Collected", sales.Where(o => o.StatusEnum == OrderStatus.Collected).Sum(o => o.Quantity).ToString());
            Row(act, "Not out", sales.Where(o => o.StatusEnum == OrderStatus.NotOut).Sum(o => o.Quantity).ToString());
            Row(act, "Refunded", orders.Count(o => o.StatusEnum == OrderStatus.Refunded).ToString());
            Row(act, "Club takings", Enc(c.Money(sales.Sum(o => o.ClubAmount))));
            Row(act, "Our booking fees", Enc(c.Money(sales.Sum(o => o.PlatformFee))));
            litActivity.Text = act.ToString();
            lnkPublic.NavigateUrl = "~/c/" + c.Slug;
            lnkPublicUrl.NavigateUrl = "~/c/" + c.Slug;
            lnkPublicUrl.Text = c.PublicUrl;

            var emails = EmailLog.ForClub(c.ClubId, 15);
            if (emails.Count == 0)
                litEmails.Text = "<p class=\"muted mb0\">No emails sent to this club yet.</p>";
            else
            {
                var em = new StringBuilder("<div class=\"tablewrap\"><table class=\"data\"><tr><th>When</th><th>To / subject</th><th>Result</th></tr>");
                foreach (EmailLog log in emails)
                    em.Append("<tr><td class=\"nowrap\">").Append(AdminDate(log.CreatedDate, "d MMM HH:mm"))
                      .Append("</td><td>").Append(Enc(log.ToEmail)).Append("<div class=\"small muted\">").Append(Enc(log.Subject)).Append("</div></td><td>")
                      .Append(log.Success ? "<span class=\"pill\">Sent</span>"
                                          : "<span class=\"pill red\">Failed</span><div class=\"small\">" + Enc(log.ErrorSummary) + "</div>")
                      .Append("</td></tr>");
                litEmails.Text = em.Append("</table></div>").ToString();
            }

            btnApprove.Visible = c.StatusEnum != ClubStatus.Approved;
            btnApprove.Text = c.StatusEnum == ClubStatus.Pending ? "Approve" : "Reinstate";
            btnReject.Visible = c.StatusEnum == ClubStatus.Pending;
            btnSuspend.Visible = c.StatusEnum == ClubStatus.Approved;
        }

        private static void Row(StringBuilder sb, string label, string html)
        {
            sb.Append("<tr><td class=\"muted nowrap\">").Append(Enc(label)).Append("</td><td>").Append(html).Append("</td></tr>");
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            bool first = !_club.ApprovedDate.HasValue;
            _club.StatusEnum = ClubStatus.Approved;
            _club.StatusReason = string.Empty;
            if (first)
                _club.ApprovedDate = DateTime.UtcNow;
            _club.UpdatedBy = "ADM";
            _club.Update();

            bool sent = Mailer.SendSystem(_club.ContactEmail, first ? "You're verified - set up payments to go live" : "Your account has been reinstated",
                "Hi " + _club.ContactName + ",\n\n" + _club.ClubName + " is now verified on " + Settings.SiteName + ".\n\n"
                + "To start taking bookings:\n1. Connect your Stripe account (Payments page)\n2. Check your prices and collection dates\n3. Switch on 'Accepting orders' (Club details page)\n\n"
                + "Log in: " + AbsoluteUrl("~/login.aspx") + "\n\nYour booking page will be: " + _club.PublicUrl + "\n\nThanks,\n" + Settings.SiteName, _club.ClubId);

            ShowOutcome(first ? "Approved" : "Reinstated", sent);
            Bind();
        }

        /// <summary>Says plainly whether the club was told; a failed email used to be reported as sent.</summary>
        private void ShowOutcome(string action, bool emailSent)
        {
            if (emailSent)
            {
                litMsg.Text = "<div class=\"msg ok\">" + Enc(action) + " and emailed to " + Enc(_club.ContactEmail) + ".</div>";
                return;
            }
            EmailLog last = EmailLog.ForClub(_club.ClubId, 1).FirstOrDefault();
            litMsg.Text = "<div class=\"msg warn\"><strong>" + Enc(action) + ", but the email to " + Enc(_club.ContactEmail) + " failed.</strong> "
                          + Enc(last == null ? "" : last.ErrorSummary) + " Let the club know another way, or fix the email settings and try again.</div>";
        }

        protected void btnReject_Click(object sender, EventArgs e)
        {
            SetBlocked(ClubStatus.Rejected, "Your registration wasn't approved",
                "we weren't able to verify " + _club.ClubName + ", so we can't approve it for " + Settings.SiteName + " at this time.");
        }

        protected void btnSuspend_Click(object sender, EventArgs e)
        {
            SetBlocked(ClubStatus.Suspended, "Your account has been suspended",
                "we've suspended " + _club.ClubName + " on " + Settings.SiteName + ". Your booking page is offline.");
        }

        private void SetBlocked(ClubStatus status, string subject, string sentence)
        {
            string reason = Clean(txtReason.Text, 500);
            _club.StatusEnum = status;
            _club.StatusReason = reason;
            _club.UpdatedBy = "ADM";
            _club.Update();
            bool sent = Mailer.SendSystem(_club.ContactEmail, subject,
                "Hi " + _club.ContactName + ",\n\nSorry - " + sentence + (reason == "" ? "" : "\n\nReason: " + reason)
                + "\n\nIf you think this is a mistake, just reply to this email.\n\n" + Settings.SiteName, _club.ClubId);
            ShowOutcome(status.ToString(), sent);
            Bind();
        }

        protected void btnNotes_Click(object sender, EventArgs e)
        {
            _club.AdminNotes = Clean(txtNotes.Text, 2000);
            _club.UpdatedBy = "ADM";
            _club.Update();
            litMsg.Text = "<div class=\"msg ok\">Notes saved.</div>";
        }
    }
}
