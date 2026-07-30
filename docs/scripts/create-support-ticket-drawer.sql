-- Support ticket drawer: assignment fields + agent reply thread.
-- IDEMPOTENT: safe to re-run.
-- Run against the MuuqWear schema in Supabase SQL editor.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

ALTER TABLE "MuuqWear".support_tickets
    ADD COLUMN IF NOT EXISTS assigned_to uuid NULL;

ALTER TABLE "MuuqWear".support_tickets
    ADD COLUMN IF NOT EXISTS assigned_to_name text NULL;

ALTER TABLE "MuuqWear".support_tickets
    ADD COLUMN IF NOT EXISTS team text NULL;

ALTER TABLE "MuuqWear".support_tickets
    ADD COLUMN IF NOT EXISTS first_response_at timestamptz NULL;

CREATE TABLE IF NOT EXISTS "MuuqWear".support_ticket_replies (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id uuid NOT NULL
        REFERENCES "MuuqWear".support_tickets(id) ON DELETE CASCADE,
    sender_type text NOT NULL
        CHECK (sender_type IN ('customer', 'agent')),
    sender_id uuid NULL,
    sender_name text NULL,
    message text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_support_ticket_replies_ticket_created
    ON "MuuqWear".support_ticket_replies (ticket_id, created_at ASC);

ALTER TABLE "MuuqWear".support_ticket_replies ENABLE ROW LEVEL SECURITY;

GRANT USAGE ON SCHEMA "MuuqWear" TO service_role;

DO $$
DECLARE
    grant_count integer;
BEGIN
    SELECT count(*)
    INTO grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'support_ticket_replies'
      AND grantee = 'service_role';

    IF grant_count = 0 THEN
        GRANT ALL ON TABLE "MuuqWear".support_ticket_replies TO service_role;
    END IF;
END $$;

DROP POLICY IF EXISTS support_ticket_replies_service_role_all
    ON "MuuqWear".support_ticket_replies;
CREATE POLICY support_ticket_replies_service_role_all
    ON "MuuqWear".support_ticket_replies
    FOR ALL
    TO service_role
    USING (true)
    WITH CHECK (true);

-- Recreate list RPC with assignment fields + reply_count.
-- Drop known overloads first so return-type changes do not fail CREATE OR REPLACE.
DROP FUNCTION IF EXISTS "MuuqWear".get_support_tickets(text, integer, integer);
DROP FUNCTION IF EXISTS "MuuqWear".get_support_tickets(text, int, int);

CREATE OR REPLACE FUNCTION "MuuqWear".get_support_tickets(
    p_status text DEFAULT '',
    p_page_size int DEFAULT 20,
    p_offset int DEFAULT 0
)
RETURNS TABLE (
    id uuid,
    ticket_number text,
    name text,
    email text,
    category text,
    subject text,
    message text,
    priority text,
    status text,
    created_at timestamptz,
    updated_at timestamptz,
    assigned_to uuid,
    assigned_to_name text,
    team text,
    first_response_at timestamptz,
    reply_count bigint
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        t.id,
        t.ticket_number,
        t.name,
        t.email,
        t.category,
        t.subject,
        t.message,
        t.priority,
        t.status,
        t.created_at,
        t.updated_at,
        t.assigned_to,
        t.assigned_to_name,
        t.team,
        t.first_response_at,
        (
            SELECT count(*)::bigint
            FROM "MuuqWear".support_ticket_replies r
            WHERE r.ticket_id = t.id
        ) AS reply_count
    FROM "MuuqWear".support_tickets t
    WHERE (
        p_status IS NULL
        OR btrim(p_status) = ''
        OR t.status = btrim(p_status)
    )
    ORDER BY t.created_at DESC
    LIMIT GREATEST(p_page_size, 1)
    OFFSET GREATEST(p_offset, 0);
$$;
