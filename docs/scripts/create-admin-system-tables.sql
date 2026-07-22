-- Admin System & Technology: logs, metadata, background jobs, sync job history.
-- IDEMPOTENT: safe to re-run.
-- Run in Supabase SQL editor against the MuuqWear schema.

-- Required for gen_random_uuid() used by admin system tables.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 1) Key/value metadata (e.g. last backup timestamp)
CREATE TABLE IF NOT EXISTS "MuuqWear".system_metadata (
    key         text PRIMARY KEY,
    value       text,
    updated_at  timestamptz NOT NULL DEFAULT now()
);

-- 2) Structured system logs for admin Logs tab
CREATE TABLE IF NOT EXISTS "MuuqWear".system_logs (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    logged_at   timestamptz NOT NULL DEFAULT now(),
    level       text NOT NULL CHECK (level IN ('Error', 'Warning', 'Info')),
    message     text NOT NULL,
    source      text
);

CREATE INDEX IF NOT EXISTS idx_system_logs_logged_at
    ON "MuuqWear".system_logs (logged_at DESC);

CREATE INDEX IF NOT EXISTS idx_system_logs_level
    ON "MuuqWear".system_logs (level);

-- Admin tables are accessed by the API using Supabase `service_role`.
-- New tables don't automatically get grants/policies.
GRANT USAGE ON SCHEMA "MuuqWear" TO service_role;

ALTER TABLE "MuuqWear".system_metadata ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS system_metadata_service_role_all ON "MuuqWear".system_metadata;
CREATE POLICY system_metadata_service_role_all
    ON "MuuqWear".system_metadata
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".system_metadata TO service_role;

ALTER TABLE "MuuqWear".system_logs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS system_logs_service_role_all ON "MuuqWear".system_logs;
CREATE POLICY system_logs_service_role_all
    ON "MuuqWear".system_logs
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".system_logs TO service_role;

-- 3) Scheduled / recurring background jobs registry
CREATE TABLE IF NOT EXISTS "MuuqWear".background_jobs (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_key           text NOT NULL UNIQUE,
    name              text NOT NULL,
    schedule          text NOT NULL,
    status            text NOT NULL DEFAULT 'idle'
        CHECK (status IN ('idle', 'running', 'failed', 'completed')),
    last_run_at       timestamptz,
    next_run_at       timestamptz,
    last_run_message  text
);

-- 4) Manual sync job execution history (for audit + optional polling)
CREATE TABLE IF NOT EXISTS "MuuqWear".sync_jobs (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    job_key           text NOT NULL,
    status            text NOT NULL
        CHECK (status IN ('queued', 'running', 'completed', 'failed')),
    message           text NOT NULL DEFAULT '',
    started_at        timestamptz NOT NULL DEFAULT now(),
    completed_at      timestamptz,
    records_affected  int
);

CREATE INDEX IF NOT EXISTS idx_sync_jobs_started_at
    ON "MuuqWear".sync_jobs (started_at DESC);

ALTER TABLE "MuuqWear".background_jobs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS background_jobs_service_role_all ON "MuuqWear".background_jobs;
CREATE POLICY background_jobs_service_role_all
    ON "MuuqWear".background_jobs
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".background_jobs TO service_role;

ALTER TABLE "MuuqWear".sync_jobs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS sync_jobs_service_role_all ON "MuuqWear".sync_jobs;
CREATE POLICY sync_jobs_service_role_all
    ON "MuuqWear".sync_jobs
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".sync_jobs TO service_role;

-- 5) Seed background jobs (do not overwrite existing rows)
INSERT INTO "MuuqWear".background_jobs (job_key, name, schedule, status, next_run_at)
VALUES
    ('daily-backup', 'Daily database backup', '0 2 * * * (Daily 2:00 AM UTC)', 'idle',
        (date_trunc('day', now() AT TIME ZONE 'UTC') + interval '1 day' + interval '2 hours')),
    ('stripe-orders', 'Stripe order sync', '0 */6 * * * (Every 6 hours)', 'idle',
        (date_trunc('hour', now() AT TIME ZONE 'UTC') + interval '6 hours')),
    ('inventory-erp', 'Inventory ERP sync', '0 3 * * * (Daily 3:00 AM UTC)', 'idle',
        (date_trunc('day', now() AT TIME ZONE 'UTC') + interval '1 day' + interval '3 hours')),
    ('affiliate-commissions', 'Affiliate commission recalculation', '0 4 * * 0 (Sundays 4:00 AM UTC)', 'idle',
        (date_trunc('week', now() AT TIME ZONE 'UTC') + interval '7 days' + interval '4 hours'))
ON CONFLICT (job_key) DO NOTHING;

-- 6) Seed starter logs only when table is empty
INSERT INTO "MuuqWear".system_logs (level, message, source, logged_at)
SELECT v.level, v.message, v.source, v.logged_at
FROM (VALUES
    ('Info',    'System logs table initialized', 'System', now() - interval '1 day'),
    ('Info',    'Stripe webhook endpoint verified', 'StripeWebhook', now() - interval '12 hours'),
    ('Warning', 'SendGrid API key not configured — email delivery disabled', 'SendGrid', now() - interval '6 hours'),
    ('Info',    'Supabase connection healthy', 'Supabase', now() - interval '2 hours')
) AS v(level, message, source, logged_at)
WHERE NOT EXISTS (SELECT 1 FROM "MuuqWear".system_logs LIMIT 1);
