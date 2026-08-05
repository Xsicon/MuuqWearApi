-- Temporary customer account suspension (1 day – 12 months).
-- IDEMPOTENT: safe to re-run.
-- Run in Supabase SQL editor against the MuuqWear schema.
--
-- Re-suspend policy (API): SuspendedUntil is always recomputed from NOW + DurationDays
-- (does not stack onto the previous end date).
--
-- Note: get_customers / get_customers_count RPCs are left unchanged. The API
-- enriches list/detail from these profile columns and filters status in the service.

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS account_status text NOT NULL DEFAULT 'active';

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS suspended_until timestamptz NULL;

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS suspension_reason text NULL;

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS suspended_by_user_id uuid NULL;

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS suspended_at timestamptz NULL;

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS reactivated_at timestamptz NULL;

ALTER TABLE "MuuqWear".profiles
    ADD COLUMN IF NOT EXISTS reactivated_by_user_id uuid NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'profiles_account_status_check'
          AND conrelid = '"MuuqWear".profiles'::regclass
    ) THEN
        ALTER TABLE "MuuqWear".profiles
            ADD CONSTRAINT profiles_account_status_check
            CHECK (account_status IN ('active', 'suspended'));
    END IF;
END $$;

UPDATE "MuuqWear".profiles
SET account_status = 'active'
WHERE account_status IS NULL
   OR account_status NOT IN ('active', 'suspended');

CREATE INDEX IF NOT EXISTS idx_profiles_account_status
    ON "MuuqWear".profiles (account_status);

CREATE INDEX IF NOT EXISTS idx_profiles_suspended_until
    ON "MuuqWear".profiles (suspended_until)
    WHERE account_status = 'suspended';
