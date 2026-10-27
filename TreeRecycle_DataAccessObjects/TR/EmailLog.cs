using System.Collections.Generic;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>Every send attempt, with Postmark's error when it failed. Shown on the admin club page and health.ashx.</summary>
    public partial class EmailLog
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clubId"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static IList<EmailLog> ForClub(int clubId, int max)
        {
            return FindTop(max, new ICriterion[] { Expression.Eq(PropertyNames.ClubId, clubId) },
                           new[] { Order.Desc(PropertyNames.EmailLogId) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static EmailLog Latest()
        {
            return FindFirst(new ICriterion[] { }, new[] { Order.Desc(PropertyNames.EmailLogId) });
        }

        /// <summary>Postmark's own explanation from the stored error ("Request does not contain a valid Server token.").</summary>
        public string ErrorSummary
        {
            get
            {
                string e = ErrorMessage ?? "";
                var m = System.Text.RegularExpressions.Regex.Match(e, "\"Message\"\\s*:\\s*\"([^\"]*)\"");
                return m.Success ? m.Groups[1].Value : e;
            }
        }
    }
}
