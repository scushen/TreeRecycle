using System;
using System.Collections.Generic;
using System.Diagnostics;
using Stripe;

namespace TreeRecycle.DataAccessObjects
{
    /// <summary>
    /// Stripe Connect, platform side.
    ///
    /// Clubs get a Standard connected account (their own Stripe dashboard, their own KYC, they are the merchant
    /// of record). Customers pay through Stripe Checkout as a DIRECT charge on the club's account, with our
    /// per-tree fee taken as an application fee. So for a €20 tree + €2 fee the customer pays €22, Stripe takes
    /// its processing fee from the club's balance, €2 lands in our platform balance, the club keeps the rest.
    ///
    /// An order is marked paid by whichever arrives first: the success-page redirect or the
    /// checkout.session.completed webhook. Both call SettleCheckoutSession, which is idempotent.
    /// </summary>
    public static class Payments
    {
        private static long Cents(decimal euro)
        {
            return (long)Math.Round(euro * 100m, MidpointRounding.AwayFromZero);
        }

        #region Connect onboarding

        /// <summary>Creates the club's Standard account on first use, then returns a Stripe-hosted onboarding link.</summary>
        public static string CreateOnboardingLink(Club club, string returnUrl, string refreshUrl)
        {
            if (club.StripeAccountId == string.Empty)
            {
                var account = new AccountService().Create(new AccountCreateOptions
                {
                    Type = "standard",
                    Country = "IE",
                    Email = club.ContactEmail,
                    BusinessProfile = new AccountBusinessProfileOptions
                    {
                        Name = club.ClubName,
                        Url = string.IsNullOrWhiteSpace(club.Website) ? club.PublicUrl : club.Website,
                    },
                    Metadata = new Dictionary<string, string> { { "club_id", club.ClubId.ToString() } },
                });
                club.StripeAccountId = account.Id;
                club.Update();
            }

            AccountLink link = new AccountLinkService().Create(new AccountLinkCreateOptions
            {
                Account = club.StripeAccountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            });
            return link.Url;
        }

        /// <summary>Pulls charges_enabled / details_submitted from Stripe onto the club row.</summary>
        public static void RefreshAccountStatus(Club club)
        {
            if (club.StripeAccountId == string.Empty)
                return;
            Account account = new AccountService().Get(club.StripeAccountId);
            ApplyAccount(club, account);
        }

        private static void ApplyAccount(Club club, Account account)
        {
            if (club.StripeChargesEnabled != account.ChargesEnabled || club.StripeDetailsSubmitted != account.DetailsSubmitted)
            {
                club.StripeChargesEnabled = account.ChargesEnabled;
                club.StripeDetailsSubmitted = account.DetailsSubmitted;
                club.Update();
            }
        }

        /// <summary>One-time login link to the club's own Stripe dashboard (Standard accounts log in at dashboard.stripe.com).</summary>
        public const string StandardDashboardUrl = "https://dashboard.stripe.com/";

        #endregion

        #region Checkout

        /// <summary>Creates the Checkout Session for a pending order and returns the URL to redirect the customer to.</summary>
        public static string CreateCheckout(TreeOrder order, Club club, string successUrl, string cancelUrl)
        {
            var metadata = new Dictionary<string, string>
            {
                { "tree_order_id", order.TreeOrderId.ToString() },
                { "order_ref", order.OrderRef },
                { "club_id", club.ClubId.ToString() },
            };

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Mode = "payment",
                CustomerEmail = order.CustomerEmail,
                ClientReferenceId = order.OrderRef,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Quantity = order.Quantity,
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            UnitAmount = Cents(order.UnitPrice),
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Christmas tree collection - " + order.SizeLabel,
                                Description = club.ClubName,
                            },
                        },
                    },
                },
                PaymentIntentData = new Stripe.Checkout.SessionPaymentIntentDataOptions
                {
                    ApplicationFeeAmount = Cents(order.PlatformFee),
                    Description = "Tree collection " + order.OrderRef,
                    Metadata = metadata,
                },
            };

            if (order.PlatformFee > 0)
            {
                options.LineItems.Add(new Stripe.Checkout.SessionLineItemOptions
                {
                    Quantity = order.Quantity,
                    PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = Cents(order.PlatformFeePerTree),
                        ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Booking fee",
                        },
                    },
                });
            }

            Stripe.Checkout.Session session = new Stripe.Checkout.SessionService()
                .Create(options, new RequestOptions { StripeAccount = club.StripeAccountId });

            order.StripeCheckoutSessionId = session.Id;
            order.Update();
            return session.Url;
        }

        /// <summary>
        /// Marks the order paid if Stripe says the session is paid. Safe to call any number of times, from the
        /// success page or the webhook. Returns the order (whatever its state) or null if the session is unknown.
        /// </summary>
        public static TreeOrder SettleCheckoutSession(string sessionId, string stripeAccountId)
        {
            TreeOrder order = TreeOrder.FindByCheckoutSession(sessionId);
            if (order == null)
                return null;
            if (order.StatusEnum != OrderStatus.PendingPayment)
                return order;

            Club club = Club.FindById(order.ClubId);
            if (string.IsNullOrEmpty(stripeAccountId))
                stripeAccountId = club.StripeAccountId;
            var requestOptions = new RequestOptions { StripeAccount = stripeAccountId };

            Stripe.Checkout.Session session = new Stripe.Checkout.SessionService().Get(sessionId, null, requestOptions);
            if (session.PaymentStatus != "paid")
                return order;

            // Re-read and re-check: the redirect and the webhook can land in the same second.
            order = TreeOrder.FindById(order.TreeOrderId);
            if (order.StatusEnum != OrderStatus.PendingPayment)
                return order;

            order.StatusEnum = OrderStatus.Paid;
            order.PaidDate = DateTime.UtcNow;
            order.StripePaymentIntentId = session.PaymentIntentId ?? string.Empty;
            order.UpdatedBy = "STR";
            order.StripeFee = TryGetStripeFee(order.StripePaymentIntentId, requestOptions);
            order.Update();

            try
            {
                Mailer.SendOrderEmail(order, EmailTemplateType.Confirmation);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Confirmation email failed for " + order.OrderRef + ": " + ex);
            }
            return order;
        }

        /// <summary>Stripe's processing fee on the club's side of the charge, for the club's profit figures.</summary>
        private static decimal TryGetStripeFee(string paymentIntentId, RequestOptions requestOptions)
        {
            if (string.IsNullOrEmpty(paymentIntentId))
                return 0m;
            try
            {
                var options = new PaymentIntentGetOptions();
                options.AddExpand("latest_charge.balance_transaction");
                PaymentIntent intent = new PaymentIntentService().Get(paymentIntentId, options, requestOptions);
                BalanceTransaction bt = intent.LatestCharge != null ? intent.LatestCharge.BalanceTransaction : null;
                if (bt == null)
                    return 0m;
                // bt.Fee includes our application fee; strip it out to leave Stripe's own fee.
                long stripeOnly = 0;
                foreach (BalanceTransactionFeeDetail detail in bt.FeeDetails)
                    if (detail.Type == "stripe_fee")
                        stripeOnly += detail.Amount;
                return stripeOnly / 100m;
            }
            catch (StripeException ex)
            {
                Trace.TraceWarning("Could not read Stripe fee for " + paymentIntentId + ": " + ex.Message);
                return 0m;
            }
        }

        /// <summary>
        /// Full refund from the club's back office. The platform fee is only refunded when
        /// RefundPlatformFee=true in web.config - otherwise the club bears it.
        /// </summary>
        public static void Refund(TreeOrder order, Club club)
        {
            if (!order.IsSale || order.StripePaymentIntentId == string.Empty)
                throw new InvalidOperationException("Only paid orders can be refunded.");

            bool refundFee = Settings.Get("RefundPlatformFee").Equals("true", StringComparison.OrdinalIgnoreCase);
            new RefundService().Create(new RefundCreateOptions
            {
                PaymentIntent = order.StripePaymentIntentId,
                RefundApplicationFee = refundFee,
                Metadata = new Dictionary<string, string> { { "order_ref", order.OrderRef } },
            }, new RequestOptions { StripeAccount = club.StripeAccountId });

            order.StatusEnum = OrderStatus.Refunded;
            order.Update();
        }

        #endregion

        #region Webhook

        /// <summary>
        /// Handles a Connect webhook. Returns false if the signature is bad (caller answers 400).
        /// One endpoint in the platform dashboard with "Listen to events on Connected accounts" covers every club.
        /// </summary>
        public static bool HandleWebhook(string json, string signatureHeader)
        {
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, Settings.StripeConnectWebhookSecret, 300, false);
            }
            catch (StripeException)
            {
                return false;
            }

            StripeEvent record = StripeEvent.FindByEventId(stripeEvent.Id);
            if (record != null && record.Processed)
                return true;
            if (record == null)
            {
                record = new StripeEvent
                {
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type ?? string.Empty,
                    StripeAccountId = stripeEvent.Account ?? string.Empty,
                    CreatedBy = "STR",
                    UpdatedBy = "STR",
                };
                record.Create();
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                    case "checkout.session.async_payment_succeeded":
                        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        if (session != null)
                            SettleCheckoutSession(session.Id, stripeEvent.Account);
                        break;

                    case "charge.refunded":
                        var charge = stripeEvent.Data.Object as Charge;
                        if (charge != null && charge.Refunded)
                        {
                            TreeOrder refunded = TreeOrder.FindByPaymentIntent(charge.PaymentIntentId);
                            if (refunded != null && refunded.StatusEnum != OrderStatus.Refunded)
                            {
                                refunded.StatusEnum = OrderStatus.Refunded;
                                refunded.UpdatedBy = "STR";
                                refunded.Update();
                            }
                        }
                        break;

                    case "account.updated":
                        var account = stripeEvent.Data.Object as Account;
                        Club club = account == null ? null : Club.FindByStripeAccount(account.Id);
                        if (club != null)
                            ApplyAccount(club, account);
                        break;
                }
                record.Processed = true;
                record.ErrorMessage = string.Empty;
                record.Update();
            }
            catch (Exception ex)
            {
                record.ErrorMessage = ex.Message.Length > 1000 ? ex.Message.Substring(0, 1000) : ex.Message;
                record.Update();
                throw; // 500 back to Stripe so it retries
            }
            return true;
        }

        #endregion
    }
}
