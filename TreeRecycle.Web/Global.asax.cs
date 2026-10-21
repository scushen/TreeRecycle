using System;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Web.Routing;
using Stripe;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    public class Global : HttpApplication
    {
        private static readonly BusinessObjectsFactory service = new BusinessObjectsFactory();

        protected void Application_Start(object sender, EventArgs e)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            NHibernate.Cfg.Environment.UseReflectionOptimizer = false;
            StripeConfiguration.ApiKey = Settings.StripeSecretKey;

            // Short shareable link each club puts on WhatsApp / Facebook: /c/st-marys-rfc
            RouteTable.Routes.MapPageRoute("club-page", "c/{slug}", "~/club.aspx");

            // A failure here must not kill the whole app: Application_Start never runs again, so a bad
            // connection string would leave the site broken until a restart. BeginRequest retries instead.
            try { service.InitializeActiveRecord(); }
            catch (Exception ex) { Trace.TraceError("ActiveRecord start-up failed: " + ex); }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (BusinessObjectsFactory.IsInitialized)
                return;
            // Static files and the health check work without the database.
            string path = Request.AppRelativeCurrentExecutionFilePath ?? "";
            if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                || path.Equals("~/health.ashx", StringComparison.OrdinalIgnoreCase))
                return;
            service.InitializeActiveRecord();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex is HttpException && ((HttpException)ex).GetHttpCode() == 404)
                return;
            Trace.TraceError("Unhandled: " + Request.RawUrl + " " + ex);
        }
    }
}
