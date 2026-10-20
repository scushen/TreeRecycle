using System;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Logout : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Remove(ClubPage.SessionKey);
            Go("~/login.aspx");
        }
    }
}
