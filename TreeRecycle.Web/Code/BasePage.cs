using System;
using System.Globalization;
using System.Web;
using System.Web.UI;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// Every page derives from this. Ties ViewState to the session (CSRF protection for postbacks)
    /// and carries the small helpers pages share.
    /// </summary>
    public class BasePage : Page
    {
        protected override void OnInit(EventArgs e)
        {
            // Session id is only stable once something is stored in it.
            if (Session["_init"] == null)
                Session["_init"] = DateTime.UtcNow;
            ViewStateUserKey = Session.SessionID;
            base.OnInit(e);
        }

        protected string Q(string key)
        {
            return Request.QueryString[key] ?? string.Empty;
        }

        protected int QInt(string key)
        {
            int value;
            return int.TryParse(Q(key), out value) ? value : 0;
        }

        protected static string Enc(object value)
        {
            return HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        /// <summary>A UTC time shown in the admin's (Irish) time. Club pages use ClubPage.LocalDate instead.</summary>
        protected static string AdminDate(DateTime? utc, string format)
        {
            return utc.HasValue ? Settings.ToLocal(utc.Value, Settings.AdminTimeZone).ToString(format) : "";
        }

        protected static string Clean(string value, int maxLength)
        {
            string v = (value ?? string.Empty).Trim();
            return v.Length > maxLength ? v.Substring(0, maxLength) : v;
        }

        protected static bool LooksLikeEmail(string email)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        protected static bool TryMoney(string text, out decimal amount)
        {
            // Accept "15", "15.00", "€15", "£15", "A$15"...
            string number = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"[^\d.,-]", "");
            return decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) && amount >= 0;
        }

        /// <summary>Redirect without the ThreadAbortException that Response.Redirect(url) throws.</summary>
        protected void Go(string url)
        {
            Response.Redirect(url, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        protected string AbsoluteUrl(string path)
        {
            return Settings.SiteUrl + VirtualPathUtility.ToAbsolute(path);
        }
    }
}
