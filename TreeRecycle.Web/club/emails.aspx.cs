using System;
using System.Linq;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    /// <summary>
    /// Request validation is off on the four template boxes so clubs can type characters like &lt; in their text.
    /// That's safe because templates are always HTML-encoded when rendered (EmailTemplate.RenderHtml).
    /// </summary>
    public partial class Emails : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            litTokens.Text = string.Join(" &nbsp; ", EmailTemplate.Tokens.Select(t => "<code>" + Enc(t) + "</code>"));
            if (!IsPostBack)
            {
                Bind(EmailTemplateType.Confirmation);
                Bind(EmailTemplateType.Reminder);
            }
        }

        private TextBox SubjectBox(EmailTemplateType type) { return type == EmailTemplateType.Confirmation ? txtConfSubject : txtRemSubject; }
        private TextBox BodyBox(EmailTemplateType type) { return type == EmailTemplateType.Confirmation ? txtConfBody : txtRemBody; }

        private void Bind(EmailTemplateType type)
        {
            EmailTemplate t = EmailTemplate.ForClub(CurrentClub.ClubId, type);
            SubjectBox(type).Text = t.Subject;
            BodyBox(type).Text = t.Body;
        }

        private static EmailTemplateType TypeOf(CommandEventArgs e)
        {
            return e.CommandArgument.ToString() == "2" ? EmailTemplateType.Reminder : EmailTemplateType.Confirmation;
        }

        protected void Save_Command(object sender, CommandEventArgs e)
        {
            EmailTemplateType type = TypeOf(e);
            string subject = Clean(SubjectBox(type).Text, 200);
            string body = (BodyBox(type).Text ?? "").Trim();
            if (subject == "" || body == "")
            {
                litMsg.Text = "<div class=\"msg err\">Subject and message can't be empty.</div>";
                return;
            }
            if (body.Length > 8000)
            {
                litMsg.Text = "<div class=\"msg err\">Keep the message under 8,000 characters.</div>";
                return;
            }

            EmailTemplate t = EmailTemplate.ForClub(CurrentClub.ClubId, type);
            t.Subject = subject;
            t.Body = body;
            t.UpdatedBy = "CLB";
            if (t.IsSaved)
                t.Update();
            else
            {
                t.CreatedBy = "CLB";
                t.Create();
            }

            var unknown = t.UnknownTokens();
            litMsg.Text = unknown.Count == 0
                ? "<div class=\"msg ok\">Saved.</div>"
                : "<div class=\"msg warn\">Saved, but these placeholders aren't recognised and will be sent as typed: " + Enc(string.Join(", ", unknown)) + "</div>";
        }

        protected void Test_Command(object sender, CommandEventArgs e)
        {
            EmailTemplateType type = TypeOf(e);
            // Test what's in the box now, saved or not.
            var t = new EmailTemplate { ClubId = CurrentClub.ClubId, TemplateType = (int)type, Subject = SubjectBox(type).Text, Body = BodyBox(type).Text };
            var sample = new TreeOrder
            {
                OrderRef = "XT-SAMPLE",
                CustomerName = "Mary Murphy",
                SizeLabel = "6ft - 7ft",
                Quantity = 1,
                TotalAmount = 17m,
                AddressLine1 = "12 Main Street",
                Town = CurrentClub.Town,
                Eircode = "A65 F4E2",
            };
            ClubCollectionDate date = CurrentClub.UpcomingDates.FirstOrDefault()
                                      ?? new ClubCollectionDate { CollectionDate = Settings.TodayIrish.AddDays(14) };
            var values = EmailTemplate.TokenValues(CurrentClub, sample, date);
            bool ok = Mailer.Send(CurrentClub.ContactEmail, "[TEST] " + t.RenderSubject(values), t.RenderHtml(values), t.RenderText(values),
                                  CurrentClub.ContactEmail, CurrentClub.ClubId, 0, (int)type);
            litMsg.Text = ok
                ? "<div class=\"msg ok\">Test sent to " + Enc(CurrentClub.ContactEmail) + ".</div>"
                : "<div class=\"msg err\">The test email couldn't be sent. Please try again later.</div>";
        }

        protected void Reset_Command(object sender, CommandEventArgs e)
        {
            EmailTemplateType type = TypeOf(e);
            EmailTemplate saved = EmailTemplate.ForClub(CurrentClub.ClubId, type);
            if (saved.IsSaved)
                saved.Delete();
            Bind(type);
            litMsg.Text = "<div class=\"msg ok\">Reset to the default text.</div>";
        }
    }
}
