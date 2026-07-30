-- Knowledge Base agent engagement: comments + votes (admin/support only).
-- Run AFTER create-help-articles.sql.
-- IDEMPOTENT: safe to re-run.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "MuuqWear".help_article_comments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    article_id uuid NOT NULL
        REFERENCES "MuuqWear".help_articles(id) ON DELETE CASCADE,
    author_id uuid NULL,
    author_name text NOT NULL,
    body text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_help_article_comments_article_created
    ON "MuuqWear".help_article_comments (article_id, created_at DESC);

CREATE TABLE IF NOT EXISTS "MuuqWear".help_article_votes (
    article_id uuid NOT NULL
        REFERENCES "MuuqWear".help_articles(id) ON DELETE CASCADE,
    voter_key text NOT NULL,
    vote text NOT NULL CHECK (vote IN ('like', 'dislike')),
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (article_id, voter_key)
);

CREATE INDEX IF NOT EXISTS idx_help_article_votes_article
    ON "MuuqWear".help_article_votes (article_id);

ALTER TABLE "MuuqWear".help_article_comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE "MuuqWear".help_article_votes ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "MuuqWear" TO service_role;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'help_article_comments'
      AND grantee = 'service_role';

    IF grant_count = 0 THEN
        GRANT ALL ON TABLE "MuuqWear".help_article_comments TO service_role;
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
      AND table_name = 'help_article_votes'
      AND grantee = 'service_role';

    IF grant_count = 0 THEN
        GRANT ALL ON TABLE "MuuqWear".help_article_votes TO service_role;
    END IF;
END $$;

DROP POLICY IF EXISTS help_article_comments_service_role_all
    ON "MuuqWear".help_article_comments;
CREATE POLICY help_article_comments_service_role_all
    ON "MuuqWear".help_article_comments
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

DROP POLICY IF EXISTS help_article_votes_service_role_all
    ON "MuuqWear".help_article_votes;
CREATE POLICY help_article_votes_service_role_all
    ON "MuuqWear".help_article_votes
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Optional demo comment on a published Shipping or Orders article.
DO $$
DECLARE
    target_id uuid;
BEGIN
    SELECT id INTO target_id
    FROM "MuuqWear".help_articles
    WHERE status = 'published'
      AND category IN ('Shipping', 'Orders')
    ORDER BY
        CASE WHEN category = 'Shipping' THEN 0 ELSE 1 END,
        updated_at DESC
    LIMIT 1;

    IF target_id IS NULL THEN
        RAISE NOTICE 'No published Shipping/Orders article — seed comment skipped.';
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "MuuqWear".help_article_comments
        WHERE article_id = target_id
          AND author_name = 'Priya Sharma'
          AND body LIKE 'Reminder: for pre-orders%'
    ) THEN
        RAISE NOTICE 'Priya Sharma seed comment already exists — skipped.';
        RETURN;
    END IF;

    INSERT INTO "MuuqWear".help_article_comments
        (article_id, author_id, author_name, body, created_at)
    VALUES
        (
            target_id,
            NULL,
            'Priya Sharma',
            'Reminder: for pre-orders the tracking number only appears after the release date.',
            now() - interval '100 days'
        );
END $$;
