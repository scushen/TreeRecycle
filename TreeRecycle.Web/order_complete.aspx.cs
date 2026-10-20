using System;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web
{
    /// <summary>Stripe Checkout success URL. Settles the order (idempotent - the webhook may already have).</summary>
    public partial class OrderComplete : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            TreeOrder order = TreeOrder.FindByRef(Q("ref"));
            string sessionId = Q("session_id");

            // The ref and session must belong together, so a guessed ref shows nothing.
            if (order == null || sessionId == "" || order.StripeCheckoutSessionId != sessionId)
            {
                phUnknown.Visible = true;
                return;
            }

            if (order.StatusEnum == OrderStatus.PendingPayment)
            {
                try
                {
                    order = Payments.SettleCheckoutSession(sessionId, null) ?? order;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Settle on success page failed for " + order.OrderRef + ": " + ex);
                }
            }

            if (order.StatusEnum == OrderStatus.PendingPayment)
            {
                phPending.Visible = true;
                return;
            }

            Club club = Club.FindById(order.ClubId);
            ClubCollectionDate date = ClubCollectionDate.FindByPrimaryKey(order.ClubCollectionDateId, false);
            phPaid.Visible = true;
            litClub.Text = Enc(club.ClubName);
            litEmail.Text = Enc(order.CustomerEmail);
            litRef.Text = Enc(order.OrderRef);
            litTree.Text = Enc(order.Quantity + " x " + order.SizeLabel);
            litDate.Text = date == null ? "To be confirmed" : Enc(date.DisplayName);
            litAddress.Text = Enc(order.FullAddress);
            litPaid.Text = Money(order.TotalAmount);
        }
    }
}
