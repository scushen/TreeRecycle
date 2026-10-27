using System;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// /health.ashx - plain-text start-up diagnostics for a fresh deployment. It says where the connection string came
    /// from, whether the database answers, and which settings are still placeholders. It never prints connection strings,
    /// server names or key values: only "set"/"placeholder", test/live mode, and SQL error numbers.
    /// </summary>
    public class Health : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("TreeRecycle health " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            sb.AppendLine();

            string connectionString = null;
            try
            {
                connectionString = BusinessObjectsFactory.GetConnectionString();
            }
            catch (Exception ex)
            {
                sb.AppendLine("Connection string: MISSING - " + ex.Message);
            }
            if (connectionString != null)
                sb.AppendLine("Connection string: from " + BusinessObjectsFactory.ConnectionSource);

            sb.AppendLine("ActiveRecord started: " + (BusinessObjectsFactory.IsInitialized ? "yes" : "NO"));
            if (BusinessObjectsFactory.StartupError != null)
                sb.AppendLine("Last start-up error: " + Describe(BusinessObjectsFactory.StartupError));

            if (connectionString != null)
            {
                try
                {
                    using (var conn = new SqlConnection(connectionString))
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TreeRecycle_club_tbl", conn))
                    {
                        conn.Open();
                        sb.AppendLine("Database: OK - " + cmd.ExecuteScalar() + " club(s) in TreeRecycle_club_tbl");
                    }
                }
                catch (SqlException ex)
                {
                    sb.AppendLine("Database: FAILED - SQL error " + ex.Number + " " + Explain(ex.Number));
                }
                catch (Exception ex)
                {
                    sb.AppendLine("Database: FAILED - " + ex.GetType().Name);
                }
            }

            sb.AppendLine("Eircode lookups since start: " + Geo.GeocodeGoogleCalls + " billed by Google, " + Geo.GeocodeCacheHits + " answered from cache");

            sb.AppendLine();
            sb.AppendLine("SiteUrl: " + Settings.SiteUrl);
            sb.AppendLine("StripeSecretKey: " + StripeMode(Settings.StripeSecretKey));
            sb.AppendLine("StripeConnectWebhookSecret: " + SetOrNot(Settings.StripeConnectWebhookSecret));
            sb.AppendLine("GoogleGeocodingApiKey: " + SetOrNot(Settings.GoogleGeocodingApiKey));
            sb.AppendLine("PostmarkServerToken: " + SetOrNot(Settings.PostmarkServerToken));
            sb.AppendLine("EmailFromAddress: " + SetOrNot(Settings.EmailFromAddress));

            context.Response.ContentType = "text/plain";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Write(sb.ToString());
        }

        private static string Describe(Exception ex)
        {
            Exception inner = ex;
            while (inner.InnerException != null)
                inner = inner.InnerException;
            var sql = inner as SqlException;
            if (sql != null)
                return "SQL error " + sql.Number + " " + Explain(sql.Number);
            string message = inner.Message ?? "";
            return inner.GetType().Name + ": " + (message.Length > 200 ? message.Substring(0, 200) : message);
        }

        private static string Explain(int number)
        {
            switch (number)
            {
                case 40615: return "(Azure SQL firewall is blocking this app - allow Azure services, or add the app's outbound IPs)";
                case 18456: return "(login failed - wrong user or password)";
                case 4060: return "(cannot open that database - check the database name and the user's access)";
                case 208: return "(table missing - run SQL\\01_create_tables.sql against this database)";
                case 53:
                case 40:
                case -1:
                case 11001: return "(server not found / not reachable - check the server name; a local server like XPS14\\SQLEXPRESS isn't reachable from Azure)";
                case -2: return "(timed out)";
                default: return "";
            }
        }

        private static string SetOrNot(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.Contains("REPLACE") ? "PLACEHOLDER" : "set";
        }

        private static string StripeMode(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Contains("REPLACE")) return "PLACEHOLDER";
            if (key.StartsWith("sk_live_") || key.StartsWith("rk_live_")) return "set (LIVE)";
            if (key.StartsWith("sk_test_") || key.StartsWith("rk_test_")) return "set (test)";
            return "set (unrecognised format)";
        }
    }
}
