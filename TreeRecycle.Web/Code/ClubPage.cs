using System;
using System.Web;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// Base for everything under /club. One login per club: Session["ClubId"].
    /// Pending clubs can log in and set up (prices, dates, emails) while they wait for verification;
    /// rejected and suspended clubs are signed out.
    /// </summary>
    public class ClubPage : BasePage
    {
        public const string SessionKey = "ClubId";

        private Club _club;

        public Club CurrentClub { get { return _club; } }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            int clubId = Session[SessionKey] is int ? (int)Session[SessionKey] : 0;
            _club = clubId > 0 ? Club.FindById(clubId) : null;
            if (_club == null || _club.StatusEnum == ClubStatus.Rejected || _club.StatusEnum == ClubStatus.Suspended)
            {
                Session.Remove(SessionKey);
                // endResponse: true - nothing after this point may run for a signed-out visitor.
                Response.Redirect("~/login.aspx?r=" + HttpUtility.UrlEncode(Request.RawUrl), true);
            }
        }

        public static void SignIn(System.Web.SessionState.HttpSessionState session, Club club)
        {
            session[SessionKey] = club.ClubId;
        }
    }
}
