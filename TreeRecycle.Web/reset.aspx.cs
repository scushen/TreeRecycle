using System;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public partial class Reset : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Club.FindByResetToken(Q("token")) == null)
            {
                pnlForm.Visible = false;
                litMsg.Text = "<div class=\"msg err\">This link has expired or already been used. <a href=\"" + ResolveUrl("~/forgot.aspx") + "\">Request a new one</a>.</div>";
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Club club = Club.FindByResetToken(Q("token"));
            if (club == null)
                return;
            if ((txtPassword.Text ?? "").Length < 10)
            {
                litMsg.Text = "<div class=\"msg err\">Use at least 10 characters.</div>";
                return;
            }
            if (txtPassword.Text != txtPassword2.Text)
            {
                litMsg.Text = "<div class=\"msg err\">The passwords don't match.</div>";
                return;
            }
            club.PasswordHash = Passwords.Hash(txtPassword.Text);
            club.ResetToken = string.Empty;
            club.ResetTokenExpiry = null;
            club.FailedLoginCount = 0;
            club.Update();
            pnlForm.Visible = false;
            litMsg.Text = "<div class=\"msg ok\">Password saved. <a href=\"" + ResolveUrl("~/login.aspx") + "\">Log in</a>.</div>";
        }
    }
}
