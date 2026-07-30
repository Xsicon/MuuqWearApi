-- Help Center knowledge base articles for admin + public /help page.
-- REQUIRED: run before deploying API changes that read/write help_articles.
-- Run in Supabase SQL editor against the MuuqWear schema.
-- IDEMPOTENT: safe to re-run.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "MuuqWear".help_articles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title text NOT NULL,
    category text NOT NULL,
    content text NOT NULL DEFAULT '',
    status text NOT NULL DEFAULT 'draft'
        CHECK (status IN ('draft', 'published')),
    hero_image_url text NULL,
    view_count int NOT NULL DEFAULT 0 CHECK (view_count >= 0),
    helpful_count int NOT NULL DEFAULT 0 CHECK (helpful_count >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    published_at timestamptz NULL
);

CREATE TABLE IF NOT EXISTS "MuuqWear".help_article_steps (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    article_id uuid NOT NULL
        REFERENCES "MuuqWear".help_articles(id) ON DELETE CASCADE,
    sort_order int NOT NULL DEFAULT 0 CHECK (sort_order >= 0),
    detail text NOT NULL DEFAULT '',
    image_url text NULL
);

CREATE INDEX IF NOT EXISTS idx_help_articles_status
    ON "MuuqWear".help_articles (status);

CREATE INDEX IF NOT EXISTS idx_help_articles_category_status
    ON "MuuqWear".help_articles (category, status);

CREATE INDEX IF NOT EXISTS idx_help_articles_updated_at
    ON "MuuqWear".help_articles (updated_at DESC);

CREATE INDEX IF NOT EXISTS idx_help_article_steps_article_sort
    ON "MuuqWear".help_article_steps (article_id, sort_order);

ALTER TABLE "MuuqWear".help_articles ENABLE ROW LEVEL SECURITY;
ALTER TABLE "MuuqWear".help_article_steps ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "MuuqWear" TO anon, authenticated, service_role;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'help_articles'
      AND grantee NOT IN ('postgres');

    IF grant_count = 0 THEN
        GRANT SELECT ON TABLE "MuuqWear".help_articles TO anon, authenticated;
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".help_articles TO authenticated;
        GRANT ALL ON TABLE "MuuqWear".help_articles TO service_role;
    END IF;
END $$;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'help_article_steps'
      AND grantee NOT IN ('postgres');

    IF grant_count = 0 THEN
        GRANT SELECT ON TABLE "MuuqWear".help_article_steps TO anon, authenticated;
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".help_article_steps TO authenticated;
        GRANT ALL ON TABLE "MuuqWear".help_article_steps TO service_role;
    END IF;
END $$;

DROP POLICY IF EXISTS help_articles_service_role_all ON "MuuqWear".help_articles;
CREATE POLICY help_articles_service_role_all
    ON "MuuqWear".help_articles
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

DROP POLICY IF EXISTS help_articles_public_read ON "MuuqWear".help_articles;
CREATE POLICY help_articles_public_read
    ON "MuuqWear".help_articles
    FOR SELECT
    TO anon, authenticated
    USING (status = 'published');

DROP POLICY IF EXISTS help_article_steps_service_role_all ON "MuuqWear".help_article_steps;
CREATE POLICY help_article_steps_service_role_all
    ON "MuuqWear".help_article_steps
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

DROP POLICY IF EXISTS help_article_steps_public_read ON "MuuqWear".help_article_steps;
CREATE POLICY help_article_steps_public_read
    ON "MuuqWear".help_article_steps
    FOR SELECT
    TO anon, authenticated
    USING (
        EXISTS (
            SELECT 1
            FROM "MuuqWear".help_articles a
            WHERE a.id = help_article_steps.article_id
              AND a.status = 'published'
        )
    );
