-- =============================================================================
-- MUUQWEAR — Wishlist items table (Supabase SQL Editor)
--
-- HOW TO RUN
--   1. Supabase Dashboard → SQL → New query
--   2. Paste this entire script and click Run
--   3. Then run scripts/grant-wishlist-items-permissions.sql (included below)
--
-- SCHEMA
--   Tables live in the "MuuqWear" schema (same as the API Supabase client).
--
-- IDEMPOTENT
--   Safe to re-run: uses IF NOT EXISTS.
--
-- NOTE
--   The API reads the wishlist by querying this table and the products
--   table directly (no custom RPC required).
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS "MuuqWear".wishlist_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    product_id uuid NOT NULL REFERENCES "MuuqWear".products(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT wishlist_items_user_product_unique UNIQUE (user_id, product_id)
);

-- Enable RLS immediately after table creation so client keys cannot access
-- wishlist rows before policies are installed.
ALTER TABLE "MuuqWear".wishlist_items ENABLE ROW LEVEL SECURITY;

CREATE INDEX IF NOT EXISTS idx_wishlist_items_user_id
    ON "MuuqWear".wishlist_items (user_id);

CREATE INDEX IF NOT EXISTS idx_wishlist_items_product_id
    ON "MuuqWear".wishlist_items (product_id);

-- -----------------------------------------------------------------------------
-- Permissions — mirror cart_items (same grants + RLS policies)
-- See grant-wishlist-items-permissions.sql for the standalone version.
-- -----------------------------------------------------------------------------

GRANT USAGE ON SCHEMA "MuuqWear" TO anon, authenticated, service_role;

DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT DISTINCT grantee, privilege_type
        FROM information_schema.role_table_grants
        WHERE table_schema = 'MuuqWear'
          AND table_name = 'cart_items'
          AND grantee <> 'postgres'
    LOOP
        EXECUTE format(
            'GRANT %s ON TABLE "MuuqWear".wishlist_items TO %I',
            r.privilege_type,
            r.grantee
        );
    END LOOP;
END $$;

DO $$
DECLARE
    cart_rls boolean;
    cart_force boolean;
BEGIN
    SELECT c.relrowsecurity, c.relforcerowsecurity
    INTO cart_rls, cart_force
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'MuuqWear'
      AND c.relname = 'cart_items';

    -- wishlist_items is user-owned data, so keep RLS enabled even if an older
    -- cart_items setup does not report RLS in this environment.
    ALTER TABLE "MuuqWear".wishlist_items ENABLE ROW LEVEL SECURITY;

    IF cart_force IS TRUE THEN
        ALTER TABLE "MuuqWear".wishlist_items FORCE ROW LEVEL SECURITY;
    END IF;
END $$;

DO $$
DECLARE
    pol RECORD;
    role_list text;
    policy_sql text;
    wishlist_policy_name text;
    cmd_text text;
BEGIN
    FOR pol IN
        SELECT policyname
        FROM pg_policies
        WHERE schemaname = 'MuuqWear'
          AND tablename = 'wishlist_items'
    LOOP
        EXECUTE format(
            'DROP POLICY IF EXISTS %I ON "MuuqWear".wishlist_items',
            pol.policyname
        );
    END LOOP;

    FOR pol IN
        SELECT
            p.polname,
            p.polpermissive,
            p.polcmd,
            pg_get_expr(p.polqual, p.polrelid) AS qual_expr,
            pg_get_expr(p.polwithcheck, p.polrelid) AS with_check_expr,
            p.polroles
        FROM pg_policy p
        JOIN pg_class c ON c.oid = p.polrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'MuuqWear'
          AND c.relname = 'cart_items'
    LOOP
        wishlist_policy_name := replace(pol.polname, 'cart_items', 'wishlist_items');
        IF wishlist_policy_name = pol.polname THEN
            wishlist_policy_name := pol.polname || '_wishlist';
        END IF;

        IF pol.polroles = '{0}' THEN
            role_list := 'public';
        ELSE
            SELECT string_agg(quote_ident(r.rolname), ', ')
            INTO role_list
            FROM pg_roles r
            WHERE r.oid = ANY (pol.polroles);
        END IF;

        IF role_list IS NULL OR role_list = '' THEN
            role_list := 'public';
        END IF;

        cmd_text := CASE pol.polcmd
            WHEN 'r' THEN 'SELECT'
            WHEN 'a' THEN 'INSERT'
            WHEN 'w' THEN 'UPDATE'
            WHEN 'd' THEN 'DELETE'
            WHEN '*' THEN 'ALL'
            ELSE 'ALL'
        END;

        policy_sql := format(
            'CREATE POLICY %I ON "MuuqWear".wishlist_items AS %s FOR %s TO %s',
            wishlist_policy_name,
            CASE WHEN pol.polpermissive THEN 'PERMISSIVE' ELSE 'RESTRICTIVE' END,
            cmd_text,
            role_list
        );

        IF pol.qual_expr IS NOT NULL AND pol.qual_expr <> '' THEN
            policy_sql := policy_sql || format(' USING (%s)', pol.qual_expr);
        END IF;

        IF pol.with_check_expr IS NOT NULL AND pol.with_check_expr <> '' THEN
            policy_sql := policy_sql || format(' WITH CHECK (%s)', pol.with_check_expr);
        END IF;

        EXECUTE policy_sql;
    END LOOP;
END $$;

DO $$
DECLARE
    wishlist_grant_count integer;
BEGIN
    SELECT count(*)
    INTO wishlist_grant_count
    FROM information_schema.role_table_grants
    WHERE table_schema = 'MuuqWear'
      AND table_name = 'wishlist_items'
      AND grantee NOT IN ('postgres');

    IF wishlist_grant_count = 0 THEN
        GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "MuuqWear".wishlist_items TO authenticated;
        GRANT ALL ON TABLE "MuuqWear".wishlist_items TO service_role;
        GRANT SELECT ON TABLE "MuuqWear".wishlist_items TO anon;
    END IF;
END $$;

DO $$
DECLARE
    cart_rls boolean;
    wishlist_policy_count integer;
BEGIN
    SELECT c.relrowsecurity
    INTO cart_rls
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'MuuqWear'
      AND c.relname = 'cart_items';

    SELECT count(*)
    INTO wishlist_policy_count
    FROM pg_policies
    WHERE schemaname = 'MuuqWear'
      AND tablename = 'wishlist_items';

    IF wishlist_policy_count = 0 THEN
        CREATE POLICY wishlist_items_select_own
            ON "MuuqWear".wishlist_items
            FOR SELECT TO authenticated
            USING (user_id = auth.uid());

        CREATE POLICY wishlist_items_insert_own
            ON "MuuqWear".wishlist_items
            FOR INSERT TO authenticated
            WITH CHECK (user_id = auth.uid());

        CREATE POLICY wishlist_items_update_own
            ON "MuuqWear".wishlist_items
            FOR UPDATE TO authenticated
            USING (user_id = auth.uid())
            WITH CHECK (user_id = auth.uid());

        CREATE POLICY wishlist_items_delete_own
            ON "MuuqWear".wishlist_items
            FOR DELETE TO authenticated
            USING (user_id = auth.uid());
    END IF;
END $$;

-- Verification
SELECT
    table_name,
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'MuuqWear'
  AND table_name IN ('cart_items', 'wishlist_items')
ORDER BY table_name, grantee, privilege_type;
