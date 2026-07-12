-- Journal article editorial fields for /journal + admin content editor.
-- REQUIRED: run this migration before deploying API changes that read/write these columns.
-- Run in Supabase SQL editor against the MuuqWear schema.
-- IDEMPOTENT: safe to re-run (IF NOT EXISTS).

ALTER TABLE "MuuqWear".journal_articles
    ADD COLUMN IF NOT EXISTS author text NULL,
    ADD COLUMN IF NOT EXISTS excerpt text NULL,
    ADD COLUMN IF NOT EXISTS slug text NULL,
    ADD COLUMN IF NOT EXISTS seo_title text NULL,
    ADD COLUMN IF NOT EXISTS tags text[] NULL DEFAULT '{}',
    ADD COLUMN IF NOT EXISTS is_featured boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS scheduled_at timestamptz NULL,
    ADD COLUMN IF NOT EXISTS read_time_minutes int NULL;

-- Unique slug when present
CREATE UNIQUE INDEX IF NOT EXISTS idx_journal_articles_slug_unique
    ON "MuuqWear".journal_articles (slug)
    WHERE slug IS NOT NULL;

-- Fast lookup + enforce at most one featured article
DROP INDEX IF EXISTS "MuuqWear".idx_journal_articles_is_featured;
CREATE UNIQUE INDEX IF NOT EXISTS idx_journal_articles_one_featured
    ON "MuuqWear".journal_articles ((true))
    WHERE is_featured = true;

-- Scheduled publishing queries
CREATE INDEX IF NOT EXISTS idx_journal_articles_status_scheduled_at
    ON "MuuqWear".journal_articles (status, scheduled_at);
