# TreeRecycle: Christmas tree collection for clubs

A standalone site where clubs, schools and charities run a Christmas tree collection fundraiser. Customers find a
club by Eircode and pay by card. The club gets its own back office for orders, routes, reminder emails and money.
"TreeRecycle" is a working name: change `SiteName` in Web.config.

Built 20 Oct 2026 on the KennelBooker stack: .NET Framework 4.8 WebForms, Castle ActiveRecord / NHibernate 2.1
(the same DLLs, copied into `lib\`), the MyGeneration DAO + proxy + partial pattern, SQL Server, Stripe.net 47.4,
and Postmark.

## Layout

| Path | What |
|---|---|
| `TreeRecycle.sln` | Opens both projects |
| `TreeRecycle_DataAccessObjects\` | Data layer. `TR\Generated\DAO` + `TR\Generated\Proxy` follow the MyGeneration shape; `TR\*.cs` are the hand-written partials; `Services\` holds Payments (Stripe Connect), Mailer (Postmark), RoutePlanner; `Helpers\` holds Geo (counties, Eircode, Google geocoding), Passwords, Settings, BusinessObjectsFactory |
| `TreeRecycle.Web\` | The website: public pages at the root, `club\` (session login, one per club), `admin\` (hard-coded login) |
| `SQL\01_create_tables.sql` | Schema: 8 tables named `TreeRecycle_<name>_tbl` (constraints and indexes are prefixed too), snake_case, NOT NULL + defaults, IDENTITY PKs, no FKs. Creates only what's missing, so it's safe to re-run. No stored procedures |
| `lib\` | Third-party DLLs, copied from `KMS\bin` so the versions match KennelBooker exactly |
| `tools\gen_entities.py` | **One spec → SQL + DAO + Proxy.** Change a table there and re-run. The partials are never touched |
| `tools\gen_designers.py` | Regenerates every `.aspx.designer.cs` from the markup |
| `tools\hash_password.py` | Makes the admin password hash |
| `tools\dev_seed.sql`, `tools\dev_test_accounts.txt` | Local test data only |
| `ADMIN_LOGIN.txt` | **The generated admin password. Move it to a password manager and delete the file.** |

IDENTITY PKs: every generated DAO already has `[PrimaryKey(PrimaryKeyType.Identity, ...)]`, so no hand edit is needed.
If you regenerate with MyGeneration instead of `gen_entities.py`, swap PKIDGen for Identity as usual.

## How the money works

Each club connects a **Standard** Stripe account (its own dashboard, its own ID checks; it is the merchant of record).
Customers pay through Stripe Checkout as a **direct charge** on the club's account, and our fee is taken as an
**application fee**:

| 6ft tree, club price €15 | |
|---|---|
| Customer pays | €15 + €2 booking fee = **€17** |
| Stripe fee (EEA card, about 1.5% + €0.25, charged to the club) | about €0.51 |
| Our platform balance | **€2.00** |
| Club's payout | about €14.49 |

- The €2 is `PlatformFeePerTree` in Web.config. Each order stores the fee it was charged, so changing the setting
  never rewrites history.
- Each order records Stripe's real fee (read from the balance transaction), so the club's Money page shows actual
  figures, not estimates.
- `RefundPlatformFee` (default false) decides whether a refund from the club back office also refunds our €2.

**Separate Stripe account: yes.** Create a new account for this under the same Stripe login (Stripe supports
several accounts per login, and it can sit under Shanree Solutions Ltd). It keeps KennelBooker's analytics, payouts and
Connect branding clean. Clubs will see this account's name and logo on the onboarding screens. Connect's
Standard account type and direct charges are what KennelBooker already uses.

An order is marked paid by whichever comes first, the success-page redirect or the `checkout.session.completed`
webhook. Both call one idempotent `Payments.SettleCheckoutSession`. That's the lesson from the KennelBooker Connect
work: never rely on the redirect alone.

## Eircodes and distance

- **Yes, it works.** The Google Geocoding API resolves an Eircode to a point (`components=country:IE`). That's one
  lookup per order or club save, and 10,000 a month are free.
- "Within 20 km" = straight-line (haversine) distance from the club's Eircode ≤ `collection_radius_km`. Each club
  sets its own radius. Road distance is typically 1.2–1.4× straight line.
- Customers can search by Eircode (nearest clubs that cover them) or browse Province → County → Town.
  Province is derived from county, so clubs don't have to enter it.
- Google's terms limit how long geocodes may be cached. For a seasonal service you could delete coordinates after
  the season (see retention below). If that's a concern, Autoaddress (the official Eircode reseller) is the precise
  alternative, priced per lookup on quote.

## Route planning

`RoutePlanner` uses the classic sweep method. It sorts the day's stops by compass bearing from the club, cuts them
into runs of `trailer_capacity` trees, then orders each run nearest-neighbour from the club. Each run covers one
wedge of the map, so the crew never zig-zags across town. The Routes page shows a Leaflet/OpenStreetMap map coloured
by run, Google Maps directions links per run (split every 10 stops), "mark whole run collected", and a print layout
with one run per page and tick boxes.

## Setup (in order)

**Secrets are never in git.** `Web.config` holds placeholders only. Two git-ignored files sit next to it in
`TreeRecycle.Web\`:
- `connectionStrings.config` (**required**, or the site won't start). Copy it from `connectionStrings.example.config`.
- `secrets.config` (optional). Copy it from `secrets.example.config`. Any key in it overrides the Web.config value with the same name.

1. **Database:** the tables live in the **KennelBooker database**, kept apart by the `TreeRecycle_` prefix. Run
   `SQL\01_create_tables.sql` against that database and point `dbConnection` at it, either in Web.config or in the App Service
   setting of the same name (which overrides Web.config). TreeRecycle runs as its own IIS site / app pool, so its
   ActiveRecord setup never touches KennelBooker's.
2. **Stripe** (new platform account): enable Connect, set branding, copy the secret key into `StripeSecretKey`.
   Add **one** webhook endpoint `https://<site>/stripe_webhook.ashx` with **"Listen to events on Connected accounts"**
   ticked, for `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `charge.refunded` and
   `account.updated`. Put its `whsec_` in `StripeConnectWebhookSecret`.
3. **Google:** create a Maps Platform key restricted to the Geocoding API, and set `GoogleGeocodingApiKey`.
4. **Postmark:** a server token in `PostmarkServerToken`; verify the `EmailFromAddress` domain; set `AdminNotifyEmail`.
5. **Admin:** the login email is jamescushen@gmail.com (`AdminAuth.AdminEmail` in `TreeRecycle.Web\Code\AdminPage.cs`). The password
   is in `ADMIN_LOGIN.txt` (only its hash is in the code).
6. `SiteUrl` = the public https address (used in emails and Stripe return URLs). Bump `site.css?id=` when CSS changes.

Local run: SQL LocalDB instance `(localdb)\TreeRecycle` with database `treerecycle` (Web.config default), IIS Express
on port 8090 (`treerecycle` in `Fable\.claude\launch.json`).

## Club journey

Register → (admin verifies) → connect Stripe → check prices (3 starter sizes are created) → add collection
day(s) → edit the confirmation/reminder emails → tick "Accepting orders" → share `/c/<club-slug>` on WhatsApp/Facebook.
The dashboard checklist lists whatever is still missing. Pending clubs can log in and set up while they wait, but
can't connect Stripe until approved, so we never create Stripe accounts for unchecked registrations.

On the day: Collection dates → **Send reminder** (only to paid, not-yet-reminded customers) → Routes → **Plan runs**
→ print → mark stops Collected / Not out (or a whole run at once). Orders can be filtered, searched, refunded,
re-emailed and downloaded as CSV.

## Extras added beyond the brief

- Shareable club page link (`/c/st-marys-rfc`). Clubs will market it themselves, which is the real growth channel.
- Per-day tree limits, an order closing date, and an "Accepting orders" pause switch.
- Multiple trees per order (1–5), with the fee charged per tree.
- "Not out" status. It records the crew's visit, so forfeits are defensible.
- Club costs (skip hire, diesel) on the Money page, giving a true "raised for the club" figure.
- Club owners can preview their page before going live. Orders outside the radius are refused with the distance shown.
- Email log table (every send, success or failure), webhook idempotency table, CSV formula-injection guard.
- Admin overview: clubs by status, trees and fees across the platform, a verification checklist, private notes, and
  suspend (which takes the page offline immediately).

## Not done / your call

- **Verified locally:** registration, club login, dashboard, every club page, route planning (30 seeded orders →
  3 runs), collected/not-out, admin login, suspend/reinstate, order-form validation, forged webhook → 400.
  **Not verified:** anything needing real keys, i.e. a live Eircode lookup, Stripe onboarding, Checkout, the success
  page and webhook settlement, refunds, and email delivery. Test those on Stripe test mode before launch.
- **Reminders are a button, not automatic.** A scheduled job (Azure WebJob/Function calling one endpoint the day
  before each collection date) is a small add-on. Reminders send synchronously, about 0.2s each, so over roughly
  400 on one day they would need Postmark's batch API.
- **Terms and privacy pages are drafts** naming Shanree Solutions Ltd. They need a legal read, especially the forfeit
  clause and the data-sharing with clubs.
- **VAT:** if Shanree is VAT-registered, the €2 fee is a VAT-inclusive supply (€1.63 + 23%). Decide how to invoice it.
- **Data retention:** nothing deletes customer addresses yet. Suggest a post-season purge (privacy page says 12 months).
- Club email changes aren't re-verified; there's no 2FA; admin lockout is per-IP and in memory (it resets on app restart).
- Suspending a club doesn't refund its open paid orders. Handle those case by case.
