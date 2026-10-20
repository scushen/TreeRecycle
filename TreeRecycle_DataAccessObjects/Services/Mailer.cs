using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Sends email through Postmark's HTTP API (same provider as KennelBooker) and logs every attempt to TreeRecycle_email_log_tbl.
    /// Customer emails go out "From" the platform address with Reply-To set to the club, so replies reach the club.
    /// </summary>
    public static class Mailer
    {
        public static bool Send(string to, string subject, string html, string text, string replyTo,
                                int clubId, int treeOrderId, int templateType)
        {
            var log = new EmailLog
            {
                ClubId = clubId,
                TreeOrderId = treeOrderId,
                TemplateType = templateType,
                ToEmail = Trunc(to, 200),
                Subject = Trunc(subject, 200),
                CreatedBy = "SYS",
                UpdatedBy = "SYS",
            };

            try
            {
                if (string.IsNullOrEmpty(Settings.PostmarkServerToken))
                    throw new InvalidOperationException("PostmarkServerToken is not set in web.config.");

                var payload = new JObject
                {
                    ["From"] = Settings.SiteName + " <" + Settings.EmailFromAddress + ">",
                    ["To"] = to,
                    ["Subject"] = subject,
                    ["HtmlBody"] = html,
                    ["TextBody"] = text ?? string.Empty,
                    ["MessageStream"] = "outbound",
                };
                if (!string.IsNullOrWhiteSpace(replyTo))
                    payload["ReplyTo"] = replyTo;

                var request = (HttpWebRequest)WebRequest.Create("https://api.postmarkapp.com/email");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.Headers.Add("X-Postmark-Server-Token", Settings.PostmarkServerToken);
                request.Timeout = 15000;
                byte[] body = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));
                using (Stream s = request.GetRequestStream())
                    s.Write(body, 0, body.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    JObject result = JObject.Parse(reader.ReadToEnd());
                    log.Success = (int?)result["ErrorCode"] == 0;
                    log.ProviderMessageId = Trunc((string)result["MessageID"], 100);
                    if (!log.Success)
                        log.ErrorMessage = Trunc((string)result["Message"], 1000);
                }
            }
            catch (WebException ex)
            {
                string detail = ex.Message;
                if (ex.Response != null)
                    using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                        detail += " " + reader.ReadToEnd();
                log.Success = false;
                log.ErrorMessage = Trunc(detail, 1000);
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ErrorMessage = Trunc(ex.Message, 1000);
            }

            try { log.Create(); }
            catch (Exception ex) { Trace.TraceError("TreeRecycle_email_log_tbl insert failed: " + ex); }

            return log.Success;
        }

        /// <summary>Sends a club's confirmation or reminder template for one order. Stamps the order on success.</summary>
        public static bool SendOrderEmail(TreeOrder order, EmailTemplateType type)
        {
            Club club = Club.FindById(order.ClubId);
            ClubCollectionDate date = order.ClubCollectionDateId > 0 ? ClubCollectionDate.FindByPrimaryKey(order.ClubCollectionDateId, false) : null;
            EmailTemplate template = EmailTemplate.ForClub(club.ClubId, type);
            var values = EmailTemplate.TokenValues(club, order, date);

            bool ok = Send(order.CustomerEmail, template.RenderSubject(values), template.RenderHtml(values), template.RenderText(values),
                           club.ContactEmail, club.ClubId, order.TreeOrderId, (int)type);
            if (ok)
            {
                if (type == EmailTemplateType.Confirmation)
                    order.ConfirmationSentDate = DateTime.UtcNow;
                else
                    order.ReminderSentDate = DateTime.UtcNow;
                order.Update();
            }
            return ok;
        }

        /// <summary>Plain platform email (registration, approval, password reset, admin alerts).</summary>
        public static bool SendSystem(string to, string subject, string text, int clubId)
        {
            string html = "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.5;color:#1f2a24;max-width:600px\">"
                          + WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br>\n") + "</div>";
            return Send(to, subject, html, text, null, clubId, 0, 0);
        }

        private static string Trunc(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
