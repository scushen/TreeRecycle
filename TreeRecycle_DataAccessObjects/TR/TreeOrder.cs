using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    public enum OrderStatus
    {
        /// <summary>Sent to Stripe Checkout, not paid yet. Abandoned ones just stay here.</summary>
        PendingPayment = 0,
        Paid = 1,
        Collected = 2,
        /// <summary>Crew called but the tree wasn't out. Forfeited per the terms in the reminder email.</summary>
        NotOut = 3,
        Cancelled = 4,
        Refunded = 5,
    }

    public partial class TreeOrder
    {
        public OrderStatus StatusEnum
        {
            get { return (OrderStatus)Status; }
            set { Status = (int)value; }
        }

        public string StatusText
        {
            get
            {
                switch (StatusEnum)
                {
                    case OrderStatus.PendingPayment: return "Awaiting payment";
                    case OrderStatus.Paid: return "To collect";
                    case OrderStatus.Collected: return "Collected";
                    case OrderStatus.NotOut: return "Not out";
                    case OrderStatus.Cancelled: return "Cancelled";
                    case OrderStatus.Refunded: return "Refunded";
                    default: return StatusEnum.ToString();
                }
            }
        }

        /// <summary>Paid orders the crew still has to call to.</summary>
        public bool IsOutstanding { get { return StatusEnum == OrderStatus.Paid; } }

        /// <summary>Orders that count as income (paid, whatever happened on the day).</summary>
        public bool IsSale
        {
            get { return StatusEnum == OrderStatus.Paid || StatusEnum == OrderStatus.Collected || StatusEnum == OrderStatus.NotOut; }
        }

        public string FullAddress
        {
            get
            {
                return string.Join(", ", new[] { AddressLine1, AddressLine2, Town, Postcode }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        public CountryInfo Country { get { return Countries.Get(CountryCode); } }

        public string Money(decimal amount) { return Country.Money(amount); }

        public string MapsLink { get { return Geo.GoogleMapsLink(Latitude, Longitude); } }

        #region Finders

        public static TreeOrder FindById(int id)
        {
            return FindByPrimaryKey(id, false);
        }

        public static TreeOrder FindByRef(string orderRef)
        {
            if (string.IsNullOrWhiteSpace(orderRef))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.OrderRef, orderRef.Trim().ToUpperInvariant()) });
        }

        public static TreeOrder FindByCheckoutSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.StripeCheckoutSessionId, sessionId) });
        }

        public static TreeOrder FindByPaymentIntent(string paymentIntentId)
        {
            if (string.IsNullOrEmpty(paymentIntentId))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.StripePaymentIntentId, paymentIntentId) });
        }

        /// <summary>All of a club's orders except abandoned checkouts.</summary>
        public static IList<TreeOrder> ForClub(int clubId)
        {
            return FindAll(new ICriterion[]
            {
                Expression.Eq(PropertyNames.ClubId, clubId),
                Expression.Not(Expression.Eq(PropertyNames.Status, (int)OrderStatus.PendingPayment)),
            }, new[] { Order.Desc(PropertyNames.CreatedDate) });
        }

        public static IList<TreeOrder> ForCollectionDate(int clubCollectionDateId)
        {
            return FindAll(new ICriterion[]
            {
                Expression.Eq(PropertyNames.ClubCollectionDateId, clubCollectionDateId),
                Expression.Not(Expression.Eq(PropertyNames.Status, (int)OrderStatus.PendingPayment)),
            }, new[] { Order.Asc(PropertyNames.RunNumber), Order.Asc(PropertyNames.RunSequence) });
        }

        /// <summary>Every sale across every club, for the admin overview.</summary>
        public static IList<TreeOrder> AllSales()
        {
            return FindAll(new ICriterion[]
            {
                Expression.In(PropertyNames.Status, new object[] { (int)OrderStatus.Paid, (int)OrderStatus.Collected, (int)OrderStatus.NotOut }),
            }, new[] { Order.Desc(PropertyNames.CreatedDate) });
        }

        #endregion

        // No 0/O/1/I/L so references read cleanly over the phone.
        private const string RefAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

        /// <summary>Short customer-facing reference, e.g. "XT-7K3Q9M".</summary>
        public static string NewOrderRef()
        {
            byte[] bytes = new byte[6];
            for (int attempt = 0; attempt < 10; attempt++)
            {
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(bytes);
                char[] chars = bytes.Select(b => RefAlphabet[b % RefAlphabet.Length]).ToArray();
                string candidate = "XT-" + new string(chars);
                if (FindByRef(candidate) == null)
                    return candidate;
            }
            throw new InvalidOperationException("Could not allocate a unique order reference.");
        }
    }
}
