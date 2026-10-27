using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    public enum EmailTemplateType
    {
        Confirmation = 1,
        Reminder = 2,
    }

    /// <summary>
    /// One editable template per club per type. Clubs write plain text with {{tokens}};
    /// we HTML-encode it and turn line breaks into &lt;br&gt;, so a club can never inject markup.
    /// </summary>
    public partial class EmailTemplate
    {
        public EmailTemplateType TemplateTypeEnum
        {
            get { return (EmailTemplateType)TemplateType; }
            set { TemplateType = (int)value; }
        }

        public static readonly string[] Tokens =
        {
            "{{customer_name}}", "{{order_ref}}", "{{club_name}}", "{{tree_size}}", "{{quantity}}", "{{amount_paid}}",
            "{{collection_date}}", "{{address}}", "{{postcode}}", "{{eircode}}", "{{club_phone}}", "{{club_email}}", "{{collection_info}}",
        };

        /// <summary>The club's saved template, or the built-in default if they haven't edited it yet.</summary>
        public static EmailTemplate ForClub(int clubId, EmailTemplateType type)
        {
            EmailTemplate saved = FindFirst(new ICriterion[]
            {
                Expression.Eq(PropertyNames.ClubId, clubId),
                Expression.Eq(PropertyNames.TemplateType, (int)type),
            });
            return saved ?? Default(clubId, type);
        }

        public bool IsSaved { get { return EmailTemplateId > 0; } }

        public static EmailTemplate Default(int clubId, EmailTemplateType type)
        {
            var t = new EmailTemplate { ClubId = clubId, TemplateType = (int)type };
            if (type == EmailTemplateType.Confirmation)
            {
                t.Subject = "Your Christmas tree collection is booked ({{order_ref}})";
                t.Body =
@"Hi {{customer_name}},

Thanks for supporting {{club_name}}! Your Christmas tree collection is booked and paid.

Order: {{order_ref}}
Tree: {{quantity}} x {{tree_size}}
Paid: {{amount_paid}}
Collection: {{collection_date}}
Address: {{address}}

Collections take place over the day, so we can't give an exact time. Please make sure your tree is out by 8am and somewhere our crew can reach it without coming into your house - your front garden or driveway is ideal. Please remove all decorations, lights, tinsel and the stand.

{{collection_info}}

If you have any questions, contact us on {{club_phone}} or {{club_email}}.

Thanks again,
{{club_name}}";
            }
            else
            {
                t.Subject = "Reminder: your tree is collected tomorrow ({{order_ref}})";
                t.Body =
@"Hi {{customer_name}},

Just a reminder that {{club_name}} will collect your Christmas tree on {{collection_date}}.

Please have your tree in your front garden or driveway by 8am, with all decorations, lights and the stand removed.

If the tree isn't out when our crew calls, we won't be able to come back for it - collections run on this date only - and unfortunately the collection fee can't be refunded.

Address we have for you: {{address}}

Questions? Contact us on {{club_phone}} or {{club_email}}.

Thanks for supporting {{club_name}}!";
            }
            return t;
        }

        public static Dictionary<string, string> TokenValues(Club club, TreeOrder order, ClubCollectionDate date)
        {
            return new Dictionary<string, string>
            {
                { "{{customer_name}}", order.CustomerName },
                { "{{order_ref}}", order.OrderRef },
                { "{{club_name}}", club.ClubName },
                { "{{tree_size}}", order.SizeLabel },
                { "{{quantity}}", order.Quantity.ToString(CultureInfo.InvariantCulture) },
                { "{{amount_paid}}", order.Money(order.TotalAmount) },
                { "{{collection_date}}", date == null ? "to be confirmed" : date.DisplayName },
                { "{{address}}", order.FullAddress },
                { "{{postcode}}", order.Postcode },
                { "{{eircode}}", order.Postcode }, // pre-international templates
                { "{{club_phone}}", club.ContactPhone },
                { "{{club_email}}", club.ContactEmail },
                { "{{collection_info}}", club.CollectionInfo },
            };
        }

        public string RenderSubject(Dictionary<string, string> values)
        {
            return Replace(Subject, values).Replace("\r", " ").Replace("\n", " ");
        }

        public string RenderText(Dictionary<string, string> values)
        {
            return Replace(Body, values);
        }

        public string RenderHtml(Dictionary<string, string> values)
        {
            string text = RenderText(values);
            string encoded = WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br>\n");
            return "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.5;color:#1f2a24;max-width:600px\">" + encoded + "</div>";
        }

        private static string Replace(string template, Dictionary<string, string> values)
        {
            string result = template ?? string.Empty;
            foreach (KeyValuePair<string, string> kv in values)
                result = result.Replace(kv.Key, kv.Value ?? string.Empty);
            return result;
        }

        /// <summary>Tokens used in the text that we don't recognise - shown as a warning when the club saves.</summary>
        public IList<string> UnknownTokens()
        {
            var found = System.Text.RegularExpressions.Regex.Matches((Subject ?? "") + " " + (Body ?? ""), @"\{\{[^}]*\}\}")
                .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).Distinct();
            return found.Where(f => !Tokens.Contains(f)).ToList();
        }
    }
}
