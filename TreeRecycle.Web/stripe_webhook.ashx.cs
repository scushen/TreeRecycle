using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>
    /// Stripe Connect webhook. In the platform Stripe dashboard add ONE endpoint pointing here with
    /// "Listen to events on Connected accounts" ticked, for: checkout.session.completed,
    /// checkout.session.async_payment_succeeded, charge.refunded, account.updated.
    /// </summary>
    public class StripeWebhook : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.HttpMethod != "POST")
            {
                context.Response.StatusCode = 405;
                return;
            }

            string json;
            using (var reader = new StreamReader(context.Request.InputStream))
                json = reader.ReadToEnd();

            try
            {
                bool ok = Payments.HandleWebhook(json, context.Request.Headers["Stripe-Signature"]);
                context.Response.StatusCode = ok ? 200 : 400;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Stripe webhook failed: " + ex);
                context.Response.StatusCode = 500;
            }
        }
    }
}
