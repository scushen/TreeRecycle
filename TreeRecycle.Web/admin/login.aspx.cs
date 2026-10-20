using System;

namespace TreeRecycle.Web.Admin
{
    public partial class AdminLogin : BasePage
    {
        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string ip = Request.UserHostAddress ?? "";
            if (AdminAuth.IsLockedOut(ip))
            {
                litMsg.Text = "<div class=\"msg err\">Too many attempts. Try again in 15 minutes.</div>";
                return;
            }
            Session.Clear();
            if (AdminAuth.TrySignIn(Session, ip, txtEmail.Text, txtPassword.Text))
            {
                Go("~/admin/");
                return;
            }
            litMsg.Text = "<div class=\"msg err\">That email and password don't match.</div>";
        }
    }
}
