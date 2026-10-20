using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>Webhook idempotency: one row per Stripe event id.</summary>
    public partial class StripeEvent
    {
        public static StripeEvent FindByEventId(string eventId)
        {
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.EventId, eventId) });
        }
    }
}
