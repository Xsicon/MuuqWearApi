-- Phase 1 affiliate admin dashboard: tier perks/bonus/capacity,
-- platinum seed, profiles.affiliate_is_active + approval/payment prefs.
-- IDEMPOTENT: safe to re-run.
-- Run in Supabase SQL editor against the MuuqWear schema.

-- 1) Tier columns
ALTER TABLE "MuuqWear".affiliate_tiers
  ADD COLUMN IF NOT EXISTS quarterly_bonus_percent numeric(5,2) NOT NULL DEFAULT 0
    CHECK (quarterly_bonus_percent >= 0 AND quarterly_bonus_percent <= 100);

ALTER TABLE "MuuqWear".affiliate_tiers
  ADD COLUMN IF NOT EXISTS max_affiliates int NULL
    CHECK (max_affiliates IS NULL OR max_affiliates > 0);

ALTER TABLE "MuuqWear".affiliate_tiers
  ADD COLUMN IF NOT EXISTS perks jsonb NOT NULL DEFAULT '[]'::jsonb;

-- 2) Backfill sensible default perks when empty
UPDATE "MuuqWear".affiliate_tiers
SET perks = CASE lower(slug)
  WHEN 'bronze' THEN '["Custom referral link","Monthly newsletter","Muuqwear branded kit"]'::jsonb
  WHEN 'silver' THEN '["All Bronze perks","Priority support","Early access to drops","Quarterly bonus"]'::jsonb
  WHEN 'gold' THEN '["All Silver perks","Dedicated account manager","Exclusive campaign invites","Co-branded content"]'::jsonb
  WHEN 'platinum' THEN '["All Gold perks","Revenue share program","Product collaboration rights","Annual retreat invite","First look at new collections"]'::jsonb
  ELSE perks
END
WHERE perks = '[]'::jsonb OR perks IS NULL;

-- 3) Seed Platinum (optional 4th tier)
INSERT INTO "MuuqWear".affiliate_tiers (
    id,
    slug,
    display_name,
    items_sold_threshold,
    commission_rate_percent,
    referral_discount_percent,
    quarterly_bonus_percent,
    max_affiliates,
    perks,
    sort_order,
    is_active,
    updated_at
) VALUES (
    'a0000001-0000-4000-8000-000000000004',
    'platinum',
    'Platinum',
    1500,
    20.00,
    20.00,
    5.00,
    NULL,
    '["All Gold perks","Revenue share program","Product collaboration rights","Annual retreat invite","First look at new collections"]'::jsonb,
    4,
    true,
    now()
)
ON CONFLICT (slug) DO NOTHING;

-- Keep existing bronze/silver/gold bonus defaults if still 0
UPDATE "MuuqWear".affiliate_tiers
SET quarterly_bonus_percent = CASE lower(slug)
  WHEN 'silver' THEN 2.00
  WHEN 'gold' THEN 3.00
  WHEN 'platinum' THEN 5.00
  ELSE quarterly_bonus_percent
END
WHERE quarterly_bonus_percent = 0
  AND lower(slug) IN ('silver', 'gold', 'platinum');

-- Fill platinum perks only when empty
UPDATE "MuuqWear".affiliate_tiers
SET perks = '["All Gold perks","Revenue share program","Product collaboration rights","Annual retreat invite","First look at new collections"]'::jsonb
WHERE lower(slug) = 'platinum'
  AND (perks = '[]'::jsonb OR perks IS NULL);

-- 4) Profile affiliate activity + join date + preferred payout method
ALTER TABLE "MuuqWear".profiles
  ADD COLUMN IF NOT EXISTS affiliate_is_active boolean NOT NULL DEFAULT true;

ALTER TABLE "MuuqWear".profiles
  ADD COLUMN IF NOT EXISTS affiliate_approved_at timestamptz NULL;

ALTER TABLE "MuuqWear".profiles
  ADD COLUMN IF NOT EXISTS affiliate_preferred_payment_method text NOT NULL DEFAULT 'manual';

-- Backfill join dates from approved applications when missing
UPDATE "MuuqWear".profiles p
SET affiliate_approved_at = COALESCE(
    p.affiliate_approved_at,
    a.reviewed_at,
    a.submitted_at
)
FROM "MuuqWear".affiliate_applications a
WHERE a.user_id = p.id
  AND lower(a.status) = 'approved'
  AND p.affiliate_approved_at IS NULL;

UPDATE "MuuqWear".profiles
SET affiliate_preferred_payment_method = 'manual'
WHERE affiliate_preferred_payment_method IS NULL
   OR btrim(affiliate_preferred_payment_method) = '';

CREATE INDEX IF NOT EXISTS idx_profiles_affiliate_tier_active
  ON "MuuqWear".profiles (affiliate_tier, affiliate_is_active)
  WHERE affiliate_application_status = 'approved';
