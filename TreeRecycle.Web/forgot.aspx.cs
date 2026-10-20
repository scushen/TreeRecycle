using System;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class Forgot : BasePage
    {
        protected void btnSend_Click(object sender, EventArgs e)
        {
            Club club = Club.FindByEmail(txtEmail.Text);
            if (club != null && club.StatusEnum != ClubStatus.Rejected)
            {
                club.ResetToken = Passwords.NewToken();
                club.ResetTokenExpiry = DateTime.UtcNow.AddHours(2);
                club.Update();
                Mailer.SendSystem(club.ContactEmail, "Reset your " + Settings.SiteName + " password",
                    "Hi " + club.ContactName + ",\n\nUse this link to choose a new password. It works for 2 hours:\n"
                    + AbsoluteUrl("~/reset.aspx") + "?token=" + club.ResetToken
                    + "\n\nIf you didn't ask for this, you can ignore this email.", club.ClubId);
            }
            // Same answer either way, so the form can't be used to discover which emails are registered.
            pnlForm.Visible = false;
            litMsg.Text = "<div class=\"msg ok\">If that email is registered, a reset link is on its way.</div>";
        }
    }
}
