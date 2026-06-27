-- =============================================================================
-- MUUQWEAR — Wishlist permissions (mirror cart_items)
--
-- HOW TO RUN
--   1. Supabase Dashboard → SQL → New query
--   2. Paste this entire script and click Run
--   3. Review the verification SELECTs at the bottom
--
-- WHEN TO RUN
--   After create-wishlist-items.sql, or any time wishlist_items returns
--   Postgres error 42501 ("permission denied for table wishlist_items").
--
-- IDEMPOTENT
--   Safe to re-run: copies grants/policies from cart_items and reapplies.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) DIAGNOSTICS — before (compare cart_items vs wishlist_items)
-- -----------------------------------------------------------------------------
SELECT
    table_name,
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'MuuqWear'
  AND table_name IN ('cart_items', 'wishlist_items')
ORDER BY table_name, grantee, privilege_type;

SELECT
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies
WHERE schemaname = 'MuuqWear'
  AND tablename IN ('cart_items', 'wishlist_items')
ORDER BY tablename, policyname;

SELECT
    c.relname AS table_name,
    c.relrowsecurity AS rls_enabled,
    c.relforcerowsecurity AS rls_forced
FROM pg_class c
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'MuuqWear'
  AND c.relname IN ('cart_items', 'wishlist_items')
ORDER BY c.relname;

-- -----------------------------------------------------------------------------
-- 2) SCHEMA USAGE (required for custom schemas)
-- -----------------------------------------------------------------------------
GRANT USAGE ON SCHEMA "MuuqWear" TO anon, authenticated, service_role;

-- wishlist_items stores per-user data. Keep RLS enabled before applying grants
-- so client keys cannot read or write rows without policies.
ALTER TABLE "MuuqWear".wishlist_items ENABLE ROW LEVEL SECURITY;

-- -----------------------------------------------------------------------------
-- 3) COPY TABLE GRANTS from cart_items → wishlist_items
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- 4) MIRROR RLS force setting from cart_items
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    cart_force boolean;
BEGIN
    SELECT c.relforcerowsecurity
    INTO cart_force
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'MuuqWear'
      AND c.relname = 'cart_items';

    ALTER TABLE "MuuqWear".wishlist_items ENABLE ROW LEVEL SECURITY;

    IF cart_force IS TRUE THEN
        ALTER TABLE "MuuqWear".wishlist_items FORCE ROW LEVEL SECURITY;
    ELSE
        ALTER TABLE "MuuqWear".wishlist_items NO FORCE ROW LEVEL SECURITY;
    END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 5) COPY RLS POLICIES from cart_items → wishlist_items
--    Expressions (user_id = auth.uid()) are identical for both tables.
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    pol RECORD;
    role_list text;
    policy_sql text;
    wishlist_policy_name text;
    cmd_text text;
BEGIN
    -- Drop existing wishlist policies so this script is re-runnable
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

-- -----------------------------------------------------------------------------
-- 6) FALLBACK — if cart_items grants were not found (edge case), apply
--    the standard MuuqWear user-scoped table grants used by addresses,
--    cart_items, and user_votes.
-- -----------------------------------------------------------------------------
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

-- Fallback RLS policies if no policies were copied
DO $$
DECLARE
    wishlist_policy_count integer;
BEGIN
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

-- -----------------------------------------------------------------------------
-- 7) DIAGNOSTICS — after (confirm parity)
-- -----------------------------------------------------------------------------
SELECT
    table_name,
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'MuuqWear'
  AND table_name IN ('cart_items', 'wishlist_items')
ORDER BY table_name, grantee, privilege_type;

SELECT
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies
WHERE schemaname = 'MuuqWear'
  AND tablename IN ('cart_items', 'wishlist_items')
ORDER BY tablename, policyname;
