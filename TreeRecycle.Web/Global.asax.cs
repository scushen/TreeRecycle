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
            service.InitializeActiveRecord();
            StripeConfiguration.ApiKey = Settings.StripeSecretKey;

            // Short shareable link each club puts on WhatsApp / Facebook: /c/st-marys-rfc
            RouteTable.Routes.MapPageRoute("club-page", "c/{slug}", "~/club.aspx");
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
