-- Seed canonical Bronze / Silver / Gold tiers.
-- Defaults match AffiliateService.GetCommissionRate (5% / 10% / 15%), NOT Milestones UI (2.5/5/10).
-- IDEMPOTENT: ON CONFLICT (slug) DO UPDATE.

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
    )
ON CONFLICT (slug) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    items_sold_threshold = EXCLUDED.items_sold_threshold,
    commission_rate_percent = EXCLUDED.commission_rate_percent,
    referral_discount_percent = EXCLUDED.referral_discount_percent,
    sort_order = EXCLUDED.sort_order,
    is_active = EXCLUDED.is_active,
    updated_at = now();
