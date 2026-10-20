using System;
using System.Collections.Concurrent;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>Base for everything under /admin except admin/login.aspx.</summary>
    public class AdminPage : BasePage
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (!AdminAuth.IsSignedIn(Session))
                Response.Redirect("~/admin/login.aspx", true);
        }

    }

    /// <summary>
    /// The single platform administrator. Credentials are hard-coded by design (no admin table):
    /// the email below plus a PBKDF2 hash of the password. To change the password run
    /// tools/hash_password.py and paste the output into AdminPasswordHash.
    /// </summary>
    public static class AdminAuth
    {
        public const string AdminEmail = "jamescushen@gmail.com";
        public const string AdminPasswordHash = "v1.120000.DtN6/daFbb/N0sS4HtXXmg==.iqCldapeiCXI4kz/gdU5LjpLhMn/0yL7cChJOlyvIlI=";

        private const string SessionKey = "IsAdmin";
        private const int MaxFailures = 5;
        private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);
        private static readonly ConcurrentDictionary<string, Tuple<int, DateTime>> Failures = new ConcurrentDictionary<string, Tuple<int, DateTime>>();

        public static bool IsSignedIn(System.Web.SessionState.HttpSessionState session)
        {
            return session[SessionKey] is bool && (bool)session[SessionKey];
        }

        public static void SignOut(System.Web.SessionState.HttpSessionState session)
        {
            session.Remove(SessionKey);
        }

        public static bool IsLockedOut(string ip)
        {
            Tuple<int, DateTime> f;
            return Failures.TryGetValue(ip, out f) && f.Item1 >= MaxFailures && DateTime.UtcNow - f.Item2 < Lockout;
        }

        /// <summary>Checks the hard-coded credentials, with a 15-minute lockout per IP after 5 failures.</summary>
        public static bool TrySignIn(System.Web.SessionState.HttpSessionState session, string ip, string email, string password)
        {
            if (IsLockedOut(ip))
                return false;
            bool ok = string.Equals((email ?? "").Trim(), AdminEmail, StringComparison.OrdinalIgnoreCase)
                      && Passwords.Verify(password, AdminPasswordHash);
            if (ok)
            {
                Tuple<int, DateTime> removed;
                Failures.TryRemove(ip, out removed);
                session[SessionKey] = true;
                return true;
            }
            Failures.AddOrUpdate(ip, new Tuple<int, DateTime>(1, DateTime.UtcNow),
                (k, f) => DateTime.UtcNow - f.Item2 >= Lockout ? new Tuple<int, DateTime>(1, DateTime.UtcNow) : new Tuple<int, DateTime>(f.Item1 + 1, DateTime.UtcNow));
            return false;
        }
    }
}
