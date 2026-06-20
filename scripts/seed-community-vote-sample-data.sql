-- =============================================================================
-- MUUQWEAR — Community Vote sample data (Supabase SQL Editor)
--
-- HOW TO RUN
--   1. Supabase Dashboard → SQL → New query
--   2. Paste this entire script and click Run
--   3. Check the verification SELECT at the bottom
--
-- SCHEMA
--   Tables live in the "MuuqWear" schema (same as the API Supabase client).
--
-- IDEMPOTENT
--   Safe to re-run: skips rows matched by style_name.
-- =============================================================================

DO $$
BEGIN
    INSERT INTO "MuuqWear".vote_items (
        id,
        style_name,
        subtitle,
        description,
        image_url,
        tag,
        vote_count,
        color_options,
        status,
        season,
        created_at
    )
    SELECT
        gen_random_uuid(),
        s.style_name,
        s.subtitle,
        s.description,
        s.image_url,
        s.tag,
        s.vote_count,
        s.color_options,
        s.status,
        s.season,
        s.created_at
    FROM (
        VALUES
            (
                'Asymmetric Hoodie'::text,
                'Apparel · SS26'::text,
                'Community concept — asymmetric cut hoodie'::text,
                'https://images.unsplash.com/photo-1722620197153-eb2549909098?w=400&h=450&fit=crop'::text,
                'TRENDING'::text,
                342::integer,
                '["#1E2A47","#4A5C7A","#3d5a3d"]'::jsonb,
                'active'::text,
                'SS26'::text,
                now() - interval '3 days'
            ),
            (
                'Tech Jacket v2',
                'Outerwear · Upcoming',
                'Technical shell jacket — vote for colorways',
                'https://images.unsplash.com/photo-1538516089546-40b08c9c9866?w=400&h=450&fit=crop',
                'VOTE NOW',
                156,
                '["#1E2A47","#4A5C7A"]'::jsonb,
                'active',
                'Upcoming',
                now() - interval '1 day'
            ),
            (
                'Utility Cargo Jogger',
                'Apparel · SS26',
                'Relaxed cargo jogger with utility pockets',
                'https://images.unsplash.com/photo-1666164938911-550e91bc34cb?w=400&h=450&fit=crop',
                'TRENDING',
                218,
                '["#4A5C7A","#3d5a3d","#1E2A47"]'::jsonb,
                'active',
                'SS26',
                now() - interval '2 days'
            ),
            (
                'Minimalist Parka',
                'Apparel · Pre-Order Ready',
                'Winner — minimalist waterproof parka',
                'https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=400&h=350&fit=crop',
                'WINNER',
                512,
                '["#1E2A47","#4A5C7A"]'::jsonb,
                'finished',
                'Pre-Order Ready',
                now() - interval '45 days'
            ),
            (
                'Structured Tote',
                'Accessories · Fall Collection',
                'Winner moved into production',
                'https://images.unsplash.com/photo-1693592401248-c9544518318a?w=400&h=350&fit=crop',
                'IN PRODUCTION',
                389,
                '["#1E2A47","#4A5C7A"]'::jsonb,
                'production',
                'Fall Collection',
                now() - interval '60 days'
            ),
            (
                'Technical Shell',
                'Apparel · Limited Edition',
                'Limited edition technical shell — community winner',
                'https://images.unsplash.com/photo-1620231151282-957594b30e94?w=400&h=350&fit=crop',
                'WINNER',
                445,
                '["#1E2A47","#3d5a3d"]'::jsonb,
                'finished',
                'Limited Edition',
                now() - interval '30 days'
            )
    ) AS s(
        style_name,
        subtitle,
        description,
        image_url,
        tag,
        vote_count,
        color_options,
        status,
        season,
        created_at
    )
    WHERE NOT EXISTS (
        SELECT 1
        FROM "MuuqWear".vote_items vi
        WHERE lower(trim(vi.style_name)) = lower(trim(s.style_name))
    );
END $$;

-- Verification
SELECT
    style_name,
    subtitle,
    tag,
    vote_count,
    status,
    season,
    created_at
FROM "MuuqWear".vote_items
WHERE style_name IN (
    'Asymmetric Hoodie',
    'Tech Jacket v2',
    'Utility Cargo Jogger',
    'Minimalist Parka',
    'Structured Tote',
    'Technical Shell'
)
ORDER BY
    CASE status
        WHEN 'active' THEN 0
        WHEN 'finished' THEN 1
        WHEN 'production' THEN 2
        ELSE 3
    END,
    vote_count DESC;
