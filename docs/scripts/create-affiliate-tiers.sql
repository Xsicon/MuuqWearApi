-- Affiliate tier configuration for admin Tier Settings + commission lookups.
-- REQUIRED: run before deploying API changes that read/write affiliate_tiers.
-- Run in Supabase SQL editor against the MuuqWear schema.
-- IDEMPOTENT: safe to re-run.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "MuuqWear".affiliate_tiers (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug text NOT NULL UNIQUE,
    display_name text NOT NULL,
    items_sold_threshold int NOT NULL CHECK (items_sold_threshold >= 0),
    commission_rate_percent numeric(5,2) NOT NULL CHECK (
        commission_rate_percent >= 0 AND commission_rate_percent <= 100
    ),
    referral_discount_percent numeric(5,2) NOT NULL DEFAULT 0 CHECK (
        referral_discount_percent >= 0 AND referral_discount_percent <= 100
    ),
    sort_order int NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    updated_at timestamptz NOT NULL DEFAULT now(),
    updated_by uuid NULL
);

CREATE INDEX IF NOT EXISTS idx_affiliate_tiers_sort
    ON "MuuqWear".affiliate_tiers (sort_order);

CREATE INDEX IF NOT EXISTS idx_affiliate_tiers_active_sort
    ON "MuuqWear".affiliate_tiers (is_active, sort_order);

ALTER TABLE "MuuqWear".affiliate_tiers ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "MuuqWear" TO anon, authenticated, service_role;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'affiliate_tiers'
      AND grantee NOT IN ('postgres');

    IF grant_count = 0 THEN
        GRANT SELECT ON TABLE "MuuqWear".affiliate_tiers TO anon, authenticated;
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".affiliate_tiers TO authenticated;
        GRANT ALL ON TABLE "MuuqWear".affiliate_tiers TO service_role;
    END IF;
END $$;

DROP POLICY IF EXISTS affiliate_tiers_service_role_all ON "MuuqWear".affiliate_tiers;
CREATE POLICY affiliate_tiers_service_role_all
    ON "MuuqWear".affiliate_tiers
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

DROP POLICY IF EXISTS affiliate_tiers_public_read ON "MuuqWear".affiliate_tiers;
CREATE POLICY affiliate_tiers_public_read
    ON "MuuqWear".affiliate_tiers
    FOR SELECT
    TO anon, authenticated
    USING (is_active = true);
