using System;
using System.IO;
using System.Web;
using System.Web.UI;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class ClubMaster : MasterPage
    {
        private Club Club { get { return ((ClubPage)Page).CurrentClub; } }

        protected string ClubName { get { return Club == null ? "" : Club.ClubName; } }

        protected string NavLink(string file, string text)
        {
            bool on = Path.GetFileName(Request.Path).Equals(file, StringComparison.OrdinalIgnoreCase)
                      || (file == "default.aspx" && Request.Path.EndsWith("/club/", StringComparison.OrdinalIgnoreCase));
            return "<a href=\"" + ResolveUrl("~/club/" + file) + "\"" + (on ? " class=\"on\"" : "") + ">" + HttpUtility.HtmlEncode(text) + "</a>";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Club != null && Club.StatusEnum == ClubStatus.Pending)
            {
                litStatusBanner.Text = "<div class=\"msg warn no-print\"><strong>Awaiting verification.</strong> We check every club before it can take payments. "
                    + "You can set up your prices, dates and emails now - we'll email you as soon as you're approved.</div>";
            }
        }
    }
}
