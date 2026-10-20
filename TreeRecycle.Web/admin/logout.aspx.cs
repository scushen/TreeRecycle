using System;

namespace TreeRecycle.Web.Admin
{
    public partial class Logout : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AdminAuth.SignOut(Session);
            Go("~/admin/login.aspx");
        }
    }
}
