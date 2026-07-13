-- Affiliate payout batches + referral paid audit columns.
-- REQUIRED: run before deploying admin payout API.
-- Run in Supabase SQL editor against the MuuqWear schema.
-- IDEMPOTENT: safe to re-run.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "MuuqWear".affiliate_payouts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    affiliate_code text NOT NULL,
    profile_id uuid NOT NULL REFERENCES "MuuqWear".profiles(id),
    total_amount numeric(12,2) NOT NULL CHECK (total_amount > 0),
    referral_count int NOT NULL CHECK (referral_count > 0),
    status text NOT NULL DEFAULT 'completed'
        CHECK (status IN ('completed', 'cancelled')),
    payment_method text NULL,
    admin_notes text NULL,
    processed_at timestamptz NOT NULL DEFAULT now(),
    processed_by uuid NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_affiliate_payouts_code
    ON "MuuqWear".affiliate_payouts (affiliate_code);

CREATE INDEX IF NOT EXISTS idx_affiliate_payouts_processed_at
    ON "MuuqWear".affiliate_payouts (processed_at DESC);

ALTER TABLE "MuuqWear".affiliate_referrals
    ADD COLUMN IF NOT EXISTS paid_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS payout_id uuid NULL
        REFERENCES "MuuqWear".affiliate_payouts(id),
    ADD COLUMN IF NOT EXISTS processed_by uuid NULL;

CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_status
    ON "MuuqWear".affiliate_referrals (status);

CREATE INDEX IF NOT EXISTS idx_affiliate_referrals_affiliate_code_status
    ON "MuuqWear".affiliate_referrals (affiliate_code, status);

ALTER TABLE "MuuqWear".affiliate_payouts ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "MuuqWear" TO anon, authenticated, service_role;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'affiliate_payouts'
      AND grantee NOT IN ('postgres');

    IF grant_count = 0 THEN
        GRANT SELECT ON TABLE "MuuqWear".affiliate_payouts TO authenticated;
        GRANT ALL ON TABLE "MuuqWear".affiliate_payouts TO service_role;
    END IF;
END $$;

DROP POLICY IF EXISTS affiliate_payouts_service_role_all ON "MuuqWear".affiliate_payouts;
CREATE POLICY affiliate_payouts_service_role_all
    ON "MuuqWear".affiliate_payouts
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);
