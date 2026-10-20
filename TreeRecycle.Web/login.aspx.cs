using System;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class Login : BasePage
    {
        private const int MaxFailedLogins = 10;

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            Club club = Club.FindByEmail(txtEmail.Text);
            const string generic = "<div class=\"msg err\">That email and password don't match.</div>";

            if (club == null)
            {
                litMsg.Text = generic;
                return;
            }
            if (club.FailedLoginCount >= MaxFailedLogins)
            {
                litMsg.Text = "<div class=\"msg err\">Too many failed attempts. Please <a href=\"" + ResolveUrl("~/forgot.aspx") + "\">reset your password</a>.</div>";
                return;
            }
            if (!Passwords.Verify(txtPassword.Text, club.PasswordHash))
            {
                club.FailedLoginCount++;
                club.Update();
                litMsg.Text = generic;
                return;
            }
            if (club.StatusEnum == ClubStatus.Rejected || club.StatusEnum == ClubStatus.Suspended)
            {
                litMsg.Text = "<div class=\"msg err\">This account is " + (club.StatusEnum == ClubStatus.Rejected ? "not approved" : "suspended") + "."
                              + (club.StatusReason == "" ? "" : " " + Enc(club.StatusReason)) + " Please contact us if you think this is a mistake.</div>";
                return;
            }

            club.FailedLoginCount = 0;
            club.LastLoginDate = DateTime.UtcNow;
            club.Update();

            // Drop anything held from before login (e.g. an admin session on the same browser).
            Session.Clear();
            ClubPage.SignIn(Session, club);

            string r = Q("r");
            Go(r.StartsWith("/") && !r.StartsWith("//") ? r : "~/club/");
        }
    }
}
