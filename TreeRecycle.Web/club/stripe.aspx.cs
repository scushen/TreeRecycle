using System;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    /// <summary>
    /// Stripe Connect onboarding (Standard accounts via Account Links). Only verified clubs can start it,
    /// so we never create Stripe accounts for registrations we haven't checked.
    /// Stripe sends the club back to ?return=1 (finished or left) or ?refresh=1 (link expired).
    /// </summary>
    public partial class StripeConnect : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Club c = CurrentClub;
            if (!IsPostBack && Q("refresh") == "1" && c.StatusEnum == ClubStatus.Approved)
            {
                // Link expired - mint a fresh one straight away.
                StartOnboarding();
                return;
            }
            if (!IsPostBack && Q("return") == "1")
            {
                try { Payments.RefreshAccountStatus(c); }
                catch (Stripe.StripeException ex) { System.Diagnostics.Trace.TraceError("Stripe refresh failed: " + ex); }
            }
            Bind();
        }

        private void Bind()
        {
            Club c = CurrentClub;
            lnkDashboard.NavigateUrl = Payments.StandardDashboardUrl;
            lnkDashboard.Visible = c.StripeAccountId != "";

            if (c.StatusEnum != ClubStatus.Approved)
            {
                litStatus.Text = "<span class=\"pill grey\">Waiting for verification</span>";
                btnConnect.Text = "Connect Stripe";
                btnConnect.Enabled = false;
                litMsg.Text = "<div class=\"msg warn\">You can connect Stripe once we've verified your club.</div>";
            }
            else if (c.StripeChargesEnabled)
            {
                litStatus.Text = "<span class=\"pill\">Connected - you can take payments</span>";
                btnConnect.Text = "Update Stripe details";
            }
            else if (c.StripeDetailsSubmitted)
            {
                litStatus.Text = "<span class=\"pill gold\">Stripe is reviewing your details</span>";
                btnConnect.Text = "Check or finish Stripe setup";
            }
            else if (c.StripeAccountId != "")
            {
                litStatus.Text = "<span class=\"pill gold\">Setup not finished</span>";
                btnConnect.Text = "Continue Stripe setup";
            }
            else
            {
                litStatus.Text = "<span class=\"pill red\">Not connected</span>";
                btnConnect.Text = "Connect Stripe";
            }
        }

        protected void btnConnect_Click(object sender, EventArgs e)
        {
            if (CurrentClub.StatusEnum != ClubStatus.Approved)
                return;
            StartOnboarding();
        }

        private void StartOnboarding()
        {
            string page = AbsoluteUrl("~/club/stripe.aspx");
            string url;
            try
            {
                url = Payments.CreateOnboardingLink(CurrentClub, page + "?return=1", page + "?refresh=1");
            }
            catch (Stripe.StripeException ex)
            {
                System.Diagnostics.Trace.TraceError("Stripe onboarding link failed: " + ex);
                litMsg.Text = "<div class=\"msg err\">Stripe couldn't be reached just now. Please try again in a minute.</div>";
                Bind();
                return;
            }
            Go(url);
        }
    }
}
