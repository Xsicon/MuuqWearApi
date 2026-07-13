# MuuqWearApi

## Affiliate tier settings

**Migrations (run in order in Supabase):**

1. `docs/scripts/create-affiliate-tiers.sql`
2. `docs/scripts/seed-affiliate-tiers.sql`

**Canonical defaults** match historical `GetCommissionRate` (not Milestones marketing copy):

| Slug | Commission | Threshold | Referral discount |
|------|------------|-----------|-------------------|
| bronze | 5% | 0 | 5% |
| silver | 10% | 150 | 10% |
| gold | 15% | 500 | 15% |

**Admin (JWT + role `admin`):**

- `GET /api/Affiliate/admin/tiers`
- `GET /api/Affiliate/admin/tiers/{slug}`
- `PUT /api/Affiliate/admin/tiers/{slug}` — partial body OK

**Public:**

- `GET /api/Affiliate/tiers` — active tiers only (Milestones sync)

`GetCommissionRate` reads `profiles.affiliate_tier` → `affiliate_tiers.commission_rate_percent` (5‑minute memory cache). Falls back to bronze / 5% if the row is missing.

---

## Affiliate admin payouts

**Migrations (run in order in Supabase):**

1. `docs/scripts/create-affiliate-payouts.sql`
2. `docs/scripts/process-affiliate-payout-rpc.sql` — atomic `process_affiliate_payout` + badge count RPC

Does **not** change `profiles.affiliate_commission_earned` (lifetime total). Pending = sum of referrals with `status = pending`.

**Admin endpoints:**

| Method | Route |
|--------|-------|
| GET | `/api/Affiliate/admin/payouts` — pending, one row per affiliate |
| GET | `/api/Affiliate/admin/payouts/{code}/referrals` — line items |
| POST | `/api/Affiliate/admin/payouts/{code}/process` — atomic mark-all-pending + batch (RPC) |
| GET | `/api/Affiliate/admin/payouts/history` — paginated batches |

Badge: `AffiliateCounts.PendingPayouts` = distinct **approved** affiliates with pending referrals (same filter as list).

