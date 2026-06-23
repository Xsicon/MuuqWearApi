-- =============================================================================
-- MUUQWEAR — Journal sample data (Supabase SQL Editor)
-- Run in: Supabase Dashboard → SQL → New query
--
-- Schema: "MuuqWear" (matches the API Supabase client)
-- Safe to re-run: upserts by title (title is treated as unique in this script)
-- =============================================================================

DO $journal$
BEGIN
    -- Ensure pgcrypto is available for gen_random_uuid() if your project needs it
    -- (Supabase usually has it enabled already)
    CREATE EXTENSION IF NOT EXISTS pgcrypto;
END $journal$;

DO $journal$
BEGIN
    CREATE TEMP TABLE _seed_journal ON COMMIT DROP AS
    SELECT *
    FROM (VALUES
        (
            'Culture'::text,
            'The Art of Minimalist Performance: A New Era'::text,
            'Exploring the intersection of technical excellence and aesthetic purity in the modern wardrobe.'::text,
            'https://images.unsplash.com/photo-1508427953056-b00b8d78ebf5?w=600&h=400&fit=crop'::text,
            $$The marriage of performance engineering and minimalist design has long been the holy grail of modern fashion. At Muuqwear, we believe
that true luxury lies not in ornamentation, but in the mastery of form and function.

Our latest collection embodies this philosophy — every seam is intentional, every fabric chosen for both its tactile quality and its technical properties. The Sapphire Veil palette, our signature color system, was developed over 14 months of research, testing hundreds of dye formulations against our exacting standards for depth, longevity, and environmental impact.

The result is a collection that moves with you. Whether you're navigating the urban commute or exploring the coastline, these pieces adapt. Moisture-wicking interiors, wind-resistant exteriors, and a drape that feels like second skin — this is what happens when you refuse to compromise.

We interviewed three of our lead designers to understand the creative process behind the collection. "The constraint is the freedom," says Amina Osman, our Head of Textiles. "When you limit yourself to the Sapphire palette and natural silhouettes, you're forced to innovate at the material level. That's where the magic happens."

This philosophy extends beyond the garments themselves. Our packaging uses 100% recycled kraft paper, our shipping is carbon-neutral, and every piece comes with a care guide designed to extend its life by years, not months. Luxury, we believe, is sustainable by nature.$$::text
        ),
        (
            'Design',
            'Behind the Sapphire Veil Palette: Color Theory',
            'How we developed our signature shade through months of textile experimentation.',
            'https://images.unsplash.com/photo-1634225180401-ccdcee2c101c?w=500&h=400&fit=crop',
            $$Color is the silent architect of emotion. The Sapphire Veil palette was born from a simple question: what does trust look like? We explored over 200 shades of blue before arriving at #1E2A47 — a hue that carries the weight of a midnight ocean and the clarity of a winter sky.

Our textile team worked with artisan dyers in Portugal and Japan, blending traditional indigo techniques with modern spectrophotometry. The result is a color that deepens with age rather than fading — a living palette that tells the story of every wear.

The supporting tones — Veil Frost and Sapphire Dust — were calibrated to create maximum visual harmony with minimal contrast. This creates that distinctive Muuqwear calm that our community has come to expect.$$::text
        ),
        (
            'Innovation',
            'Sustainable Futures: Our Commitment to the Earth',
            'Our journey towards a 100% circular production model and ethical sourcing.',
            'https://images.unsplash.com/photo-1632263532338-45575eba6aba?w=500&h=400&fit=crop',
            $$Sustainability is not a marketing strategy — it's a design constraint. Every Muuqwear garment is engineered for longevity first. We use recycled ocean plastics for our synthetic fibers, organic cotton from regenerative farms, and merino wool from certified ethical sources.

Our zero-waste pattern cutting technique, developed in-house, reduces textile waste by 34% compared to industry standards. What remains is upcycled into accessories and packaging materials.

By 2028, we aim to achieve full circularity: every product we sell can be returned, deconstructed, and remade into something new.$$::text
        ),
        (
            'Lifestyle',
            'Form Follows Function: Inside Our Material Lab',
            'A deep dive into the technical fabrics that define our seasonal collection.',
            'https://images.unsplash.com/photo-1558303522-d7a2bdfdbd82?w=500&h=400&fit=crop',
            $$Welcome to Lab M, our 4,000-square-foot material research facility. Here, fabrics are stress-tested, weathered, and worn before they ever reach a pattern table.

Our proprietary AeroWeave technology combines hollow-core fibers with a DWR finish to create fabrics that breathe like cotton but repel water like gore-tex. The hand-feel is remarkably soft — a quality we never compromise on.

Every season, we test over 150 fabric samples. Only the top 8-10 make it into production. This rigorous selection process is what separates Muuqwear from conventional brands.$$::text
        ),
        (
            'Design',
            'The Evolution of Technical Outerwear',
            'From mountaineering to urban streets - how performance wear became everyday.',
            'https://images.unsplash.com/photo-1538516089546-40b08c9c9866?w=500&h=400&fit=crop',
            $$Technical outerwear has undergone a remarkable transformation. What began as specialized mountaineering gear has evolved into the foundation of the modern urban wardrobe.

At Muuqwear, we draw directly from this lineage. Our jackets incorporate features developed for extreme conditions — sealed seams, articulated elbows, storm flaps — but translate them into silhouettes that feel at home in a gallery opening or a coffee shop.

The key is restraint. We strip away anything that doesn't serve both form and function, leaving only the essential architecture of protection and style.$$::text
        ),
        (
            'Tech',
            'Smart Fabrics: The Future of Wearable Innovation',
            'Exploring responsive textiles and temperature-regulating materials.',
            'https://images.unsplash.com/photo-1620231151282-957594b30e94?w=500&h=400&fit=crop',
            $$The next frontier in fashion isn't about screens or sensors — it's about fabrics that think. Phase-change materials (PCMs) embedded in textile fibers can absorb, store, and release heat to maintain an optimal microclimate against your skin.

We're currently prototyping a liner fabric that adjusts its insulation value based on your body temperature and activity level. No batteries, no electronics — just material science at its most elegant.

This is the future we're building toward: clothing that's genuinely intelligent, not just connected.$$::text
        ),
        (
            'Innovation',
            'Zero Waste Pattern Cutting: A New Approach',
            'Our design team''s revolutionary approach to eliminating textile waste.',
            'https://images.unsplash.com/photo-1704716720991-cf3197cfb190?w=500&h=400&fit=crop',
            $$Traditional garment production wastes between 15-20% of fabric. Our zero-waste pattern cutting technique reduces this to under 3%.

The approach is deceptively simple: we design the pattern and the garment simultaneously, treating the fabric as a complete system rather than a raw material to be carved. Every offcut becomes a pocket, a facing, or a design detail.

It requires our designers to think in fundamentally different ways — but the results speak for themselves. Less waste, more creativity, and garments with a unique geometric integrity.$$::text
        )
    ) AS t(category, title, excerpt, image_url, content);

    -- 1) Update existing articles (matched by title)
    UPDATE "MuuqWear".journal_articles ja
    SET
        category = s.category,
        content = s.content,
        status = 'published',
        published_at = COALESCE(ja.published_at, now()),
        image_url = s.image_url
    FROM _seed_journal s
    WHERE ja.title = s.title;

    -- 2) Insert missing articles
    INSERT INTO "MuuqWear".journal_articles (
        id,
        category,
        title,
        content,
        status,
        views,
        created_at,
        published_at,
        image_url
    )
    SELECT
        gen_random_uuid(),
        s.category,
        s.title,
        s.content,
        'published',
        0,
        now(),
        now(),
        s.image_url
    FROM _seed_journal s
    WHERE NOT EXISTS (
        SELECT 1
        FROM "MuuqWear".journal_articles ja
        WHERE ja.title = s.title
    );
END $journal$;
-- Verify
SELECT
    category,
    title,
    status,
    created_at,
    published_at,
    image_url
FROM "MuuqWear".journal_articles
WHERE title IN (
    'The Art of Minimalist Performance: A New Era',
    'Behind the Sapphire Veil Palette: Color Theory',
    'Sustainable Futures: Our Commitment to the Earth',
    'Form Follows Function: Inside Our Material Lab',
    'The Evolution of Technical Outerwear',
    'Smart Fabrics: The Future of Wearable Innovation',
    'Zero Waste Pattern Cutting: A New Approach'
)
ORDER BY published_at DESC;

