-- Seed canonical Bronze / Silver / Gold / Platinum tiers.
-- Defaults match AffiliateService.GetCommissionRate (5% / 10% / 15% / 20%), NOT Milestones UI (2.5/5/10).
-- IDEMPOTENT: ON CONFLICT (slug) DO NOTHING so admin edits are preserved on re-run.
-- Prefer also running extend-affiliate-admin-dashboard.sql for perks/bonus/capacity columns.

INSERT INTO "MuuqWear".affiliate_tiers (
    id,
    slug,
    display_name,
    items_sold_threshold,
    commission_rate_percent,
    referral_discount_percent,
    sort_order,
    is_active,
    updated_at
) VALUES
    (
        'a0000001-0000-4000-8000-000000000001',
        'bronze',
        'Bronze',
        0,
        5.00,
        5.00,
        1,
        true,
        now()
    ),
    (
        'a0000001-0000-4000-8000-000000000002',
        'silver',
        'Silver',
        150,
        10.00,
        10.00,
        2,
        true,
        now()
    ),
    (
        'a0000001-0000-4000-8000-000000000003',
        'gold',
        'Gold',
        500,
        15.00,
        15.00,
        3,
        true,
        now()
    ),
    (
        'a0000001-0000-4000-8000-000000000004',
        'platinum',
        'Platinum',
        1500,
        20.00,
        20.00,
        4,
        true,
        now()
    )
ON CONFLICT (slug) DO NOTHING;
