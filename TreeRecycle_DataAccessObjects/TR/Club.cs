using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NHibernate.Criterion;

namespace TreeRecycle.DataAccessObjects
{
    public enum ClubStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Suspended = 3,
    }

    public partial class Club
    {
        public static readonly string[] ClubTypes = { "Sports club", "School / PTA", "Scouts / Guides", "Charity", "Community group", "Business", "Other" };

        public ClubStatus StatusEnum
        {
            get { return (ClubStatus)Status; }
            set { Status = (int)value; }
        }

        /// <summary>Currency, postcode format, distance unit and booking fee all follow the club's country.</summary>
        public CountryInfo Country { get { return Countries.Get(CountryCode); } }

        /// <summary>"Today" in the club's own time zone - collection dates are local calendar dates.</summary>
        public DateTime Today { get { return Settings.TodayIn(TimeZone); } }

        public DateTime ToLocal(DateTime utc) { return Settings.ToLocal(utc, TimeZone); }

        public string Money(decimal amount) { return Country.Money(amount); }

        /// <summary>Collection radius in the country's unit (miles in the UK and US); stored in km.</summary>
        public int RadiusInDisplayUnits { get { return Country.ToDisplayUnits(CollectionRadiusKm); } }

        public string PublicUrl { get { return Settings.SiteUrl + "/c/" + Slug; } }

        public string FullAddress
        {
            get
            {
                string region = Region == "" ? "" : (CountryCode == "IE" ? "Co. " + Region : Region);
                return string.Join(", ", new[] { AddressLine1, AddressLine2, Town, region, Postcode }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }

        public IList<ClubPrice> ActivePrices
        {
            get { return ClubPrice.ForClub(ClubId, true); }
        }

        public IList<ClubCollectionDate> UpcomingDates
        {
            get { return ClubCollectionDate.ForClub(ClubId, true).Where(d => d.CollectionDate >= Today).ToList(); }
        }

        /// <summary>Stripe can take money on this club's behalf.</summary>
        public bool StripeReady
        {
            get { return StripeAccountId != string.Empty && StripeChargesEnabled; }
        }

        public bool OrdersClosedByDate
        {
            get { return OrdersCloseDate.HasValue && Today > OrdersCloseDate.Value.Date; }
        }

        /// <summary>
        /// Everything that must be true before the public can order. Returns the blockers in plain English
        /// (used for the club dashboard checklist); empty list = live.
        /// </summary>
        public IList<string> OrderBlockers()
        {
            var blockers = new List<string>();
            if (StatusEnum != ClubStatus.Approved)
                blockers.Add("Your club has not been verified yet.");
            if (!Geocoded)
                blockers.Add("We couldn't place your club's " + Country.PostcodeName + " on the map. Check it on the Club details page.");
            if (!StripeReady)
                blockers.Add("Connect a Stripe account so you can be paid.");
            if (ActivePrices.Count == 0)
                blockers.Add("Add at least one tree size and price.");
            if (UpcomingDates.Count == 0)
                blockers.Add("Add a collection date.");
            if (!AcceptingOrders)
                blockers.Add("Switch on 'Accepting orders' on the Club details page.");
            if (OrdersClosedByDate)
                blockers.Add("Your order closing date has passed.");
            return blockers;
        }

        public bool IsOrderable { get { return OrderBlockers().Count == 0; } }

        public double DistanceKmTo(decimal lat, decimal lng)
        {
            return Geo.DistanceKm(Latitude, Longitude, lat, lng);
        }

        public bool Covers(decimal lat, decimal lng)
        {
            return Geocoded && DistanceKmTo(lat, lng) <= CollectionRadiusKm;
        }

        #region Finders

        public static Club FindById(int clubId)
        {
            return FindByPrimaryKey(clubId, false);
        }

        public static Club FindByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.ContactEmail, email.Trim().ToLowerInvariant()) });
        }

        public static Club FindBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.Slug, slug.Trim().ToLowerInvariant()) });
        }

        public static Club FindByStripeAccount(string stripeAccountId)
        {
            if (string.IsNullOrEmpty(stripeAccountId))
                return null;
            return FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.StripeAccountId, stripeAccountId) });
        }

        public static Club FindByResetToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;
            Club club = FindFirst(new ICriterion[] { Expression.Eq(PropertyNames.ResetToken, token) });
            if (club == null || !club.ResetTokenExpiry.HasValue || club.ResetTokenExpiry.Value < DateTime.UtcNow)
                return null;
            return club;
        }

        public static IList<Club> FindAllClubs()
        {
            return FindAll(new ICriterion[] { }, new[] { Order.Asc(PropertyNames.ClubName) });
        }

        public static IList<Club> FindByStatus(ClubStatus status)
        {
            return FindAll(new ICriterion[] { Expression.Eq(PropertyNames.Status, (int)status) }, new[] { Order.Asc(PropertyNames.ClubName) });
        }

        /// <summary>Approved clubs in a country whose public order page is live, optionally limited to one region.</summary>
        public static IList<Club> FindOrderable(string countryCode, string region)
        {
            var criteria = new List<ICriterion>
            {
                Expression.Eq(PropertyNames.Status, (int)ClubStatus.Approved),
                Expression.Eq(PropertyNames.AcceptingOrders, true),
                Expression.Eq(PropertyNames.StripeChargesEnabled, true),
                Expression.Eq(PropertyNames.Geocoded, true),
            };
            if (!string.IsNullOrEmpty(countryCode))
                criteria.Add(Expression.Eq(PropertyNames.CountryCode, countryCode));
            if (!string.IsNullOrEmpty(region))
                criteria.Add(Expression.Eq(PropertyNames.Region, region));
            return FindAll(criteria.ToArray(), new[] { Order.Asc(PropertyNames.ClubName) })
                .Where(c => c.IsOrderable).ToList();
        }

        /// <summary>Orderable clubs whose collection radius covers the point, nearest first.</summary>
        public static IList<Club> FindCovering(decimal lat, decimal lng, string countryCode)
        {
            return FindOrderable(countryCode, null).Where(c => c.Covers(lat, lng)).OrderBy(c => c.DistanceKmTo(lat, lng)).ToList();
        }

        #endregion

        /// <summary>"St Mary's RFC, Templeogue" -> "st-marys-rfc-templeogue", made unique with -2, -3...</summary>
        public static string MakeUniqueSlug(string name, int ignoreClubId)
        {
            string baseSlug = Regex.Replace(Regex.Replace((name ?? "").ToLowerInvariant().Replace("'", "").Replace("’", ""), @"[^a-z0-9]+", "-"), "-+", "-").Trim('-');
            if (baseSlug.Length > 70)
                baseSlug = baseSlug.Substring(0, 70).Trim('-');
            if (baseSlug == string.Empty)
                baseSlug = "club";
            string slug = baseSlug;
            for (int i = 2; ; i++)
            {
                Club existing = FindBySlug(slug);
                if (existing == null || existing.ClubId == ignoreClubId)
                    return slug;
                slug = baseSlug + "-" + i;
            }
        }
    }
}
