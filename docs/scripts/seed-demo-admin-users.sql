-- Demo admin/staff accounts for local and staging environments.
-- IDEMPOTENT: safe to re-run (updates password + profile role on conflict).
-- Run in Supabase SQL editor.
--
-- Accounts (passwords are demo-only — change in production):
--   admin@muuqwear.com      / admin123     → admin
--   ops@muuqwear.com        / ops123       → operations_manager
--   support@muuqwear.com    / support123   → support_team
--   merch@muuqwear.com      / merch123     → merchandising
--   creative@muuqwear.com   / creative123  → content_team
--   tech@muuqwear.com       / tech123      → technology_systems

CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
    acct record;
BEGIN
    FOR acct IN
        SELECT *
        FROM (VALUES
            ('b0000001-0000-4000-8000-000000000001'::uuid, 'admin@muuqwear.com',      'admin123',     'Admin',                   'admin'),
            ('b0000001-0000-4000-8000-000000000002'::uuid, 'ops@muuqwear.com',        'ops123',       'Operations Manager',      'operations_manager'),
            ('b0000001-0000-4000-8000-000000000003'::uuid, 'support@muuqwear.com',    'support123',   'Customer Support',        'support_team'),
            ('b0000001-0000-4000-8000-000000000004'::uuid, 'merch@muuqwear.com',      'merch123',     'Merchandising',           'merchandising'),
            ('b0000001-0000-4000-8000-000000000005'::uuid, 'creative@muuqwear.com',   'creative123',  'Creative & Content',      'content_team'),
            ('b0000001-0000-4000-8000-000000000006'::uuid, 'tech@muuqwear.com',       'tech123',      'Technology & Systems',    'technology_systems')
        ) AS t(user_id, email, password, full_name, role_slug)
    LOOP
        -- auth.users
        INSERT INTO auth.users (
            id,
            instance_id,
            aud,
            role,
            email,
            encrypted_password,
            email_confirmed_at,
            raw_app_meta_data,
            raw_user_meta_data,
            created_at,
            updated_at,
            confirmation_token,
            recovery_token,
            email_change_token_new,
            email_change
        ) VALUES (
            acct.user_id,
            '00000000-0000-0000-0000-000000000000',
            'authenticated',
            'authenticated',
            acct.email,
            crypt(acct.password, gen_salt('bf')),
            now(),
            '{"provider":"email","providers":["email"]}'::jsonb,
            jsonb_build_object('full_name', acct.full_name),
            now(),
            now(),
            '',
            '',
            '',
            ''
        )
        ON CONFLICT (id) DO UPDATE SET
            email = EXCLUDED.email,
            encrypted_password = EXCLUDED.encrypted_password,
            email_confirmed_at = COALESCE(auth.users.email_confirmed_at, now()),
            raw_user_meta_data = EXCLUDED.raw_user_meta_data,
            updated_at = now();

        -- auth.identities (required for email/password sign-in)
        INSERT INTO auth.identities (
            id,
            user_id,
            provider_id,
            identity_data,
            provider,
            last_sign_in_at,
            created_at,
            updated_at
        ) VALUES (
            gen_random_uuid(),
            acct.user_id,
            acct.user_id::text,
            jsonb_build_object('sub', acct.user_id::text, 'email', acct.email),
            'email',
            now(),
            now(),
            now()
        )
        ON CONFLICT (provider_id, provider) DO UPDATE SET
            identity_data = EXCLUDED.identity_data,
            updated_at = now();

        -- MuuqWear.profiles
        INSERT INTO "MuuqWear".profiles (
            id,
            full_name,
            email,
            role,
            is_deleted,
            created_at
        ) VALUES (
            acct.user_id,
            acct.full_name,
            acct.email,
            acct.role_slug,
            false,
            now()
        )
        ON CONFLICT (id) DO UPDATE SET
            full_name = EXCLUDED.full_name,
            email = EXCLUDED.email,
            role = EXCLUDED.role_slug,
            is_deleted = false;
    END LOOP;
END $$;
