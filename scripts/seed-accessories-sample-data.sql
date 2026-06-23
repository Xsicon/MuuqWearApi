-- =============================================================================
-- MUUQWEAR — Accessories sample data (Supabase SQL Editor)
-- Run in: Supabase Dashboard → SQL → New query
--
-- Schema: "MuuqWear" (matches the API Supabase client)
-- Safe to re-run: inserts missing SKUs; updates existing MQ-ACC-* rows.
-- =============================================================================

DO $$
DECLARE
    accessories_category_id uuid;
BEGIN
    SELECT id
    INTO accessories_category_id
    FROM "MuuqWear".categories
    WHERE lower(name) = 'accessories'
    LIMIT 1;

    IF accessories_category_id IS NULL THEN
        RAISE EXCEPTION 'Accessories category not found in "MuuqWear".categories';
    END IF;

    CREATE TEMP TABLE _acc_seed ON COMMIT DROP AS
    SELECT *
    FROM (VALUES
        (
            'MQ-ACC-001'::text,
            'Veil Crossbody Bag'::text,
            'Bags'::text,
            60.00::numeric,
            NULL::text,
            'https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=600&h=800&fit=crop'::text,
            'Minimalist crossbody with adjustable strap'::text,
            '["Black", "Navy", "Olive"]'::jsonb,
            true,   -- is_featured
            false,  -- is_best_seller
            true    -- is_new_arrival
        ),
        (
            'MQ-ACC-002',
            'City Commuter 20L',
            'Bags',
            95.00,
            'Best Seller',
            'https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=600&h=800&fit=crop',
            'Water-resistant everyday backpack',
            '["Black", "Charcoal", "Navy"]'::jsonb,
            true,
            true,
            false
        ),
        (
            'MQ-ACC-003',
            'M-01 Court Sneaker',
            'Footwear',
            110.00,
            NULL,
            'https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600&h=800&fit=crop',
            'Minimalist leather sneaker',
            '["White", "Black", "Grey"]'::jsonb,
            true,
            true,
            false
        ),
        (
            'MQ-ACC-004',
            'Midnight Wrap Scarf',
            'Accessories',
            75.00,
            NULL,
            'https://images.unsplash.com/photo-1520903920243-00d872a2d1c9?w=600&h=800&fit=crop',
            'Premium merino wool scarf',
            '["Navy", "Charcoal", "Camel"]'::jsonb,
            true,
            false,
            true
        ),
        (
            'MQ-ACC-005',
            'Minimalist Belt',
            'Accessories',
            45.00,
            NULL,
            'https://images.unsplash.com/photo-1624222247344-70f5d2d49eb4?w=600&h=800&fit=crop',
            'Italian leather with matte buckle',
            '["Black", "Brown", "Tan"]'::jsonb,
            false,
            true,
            false
        ),
        (
            'MQ-ACC-006',
            'Urban Tech Wallet',
            'Accessories',
            55.00,
            'New',
            'https://images.unsplash.com/photo-1627123424574-724758594e93?w=600&h=800&fit=crop',
            'RFID-protected card holder',
            '["Black", "Navy", "Olive"]'::jsonb,
            false,
            false,
            true
        ),
        (
            'MQ-ACC-007',
            'Performance Socks 3-Pack',
            'Accessories',
            28.00,
            NULL,
            'https://images.unsplash.com/photo-1586350977771-b3b0abd50c82?w=600&h=800&fit=crop',
            'Merino blend athletic socks',
            '["Black", "Grey", "Navy"]'::jsonb,
            false,
            true,
            false
        ),
        (
            'MQ-ACC-008',
            'Leather Cardholder',
            'Accessories',
            38.00,
            'Best Seller',
            'https://images.unsplash.com/photo-1627123424574-724758594e93?w=600&h=800&fit=crop',
            'Slim profile card wallet',
            '["Black", "Cognac", "Navy"]'::jsonb,
            false,
            true,
            false
        ),
        (
            'MQ-ACC-009',
            'Sapphire Beanie',
            'Accessories',
            32.00,
            NULL,
            'https://images.unsplash.com/photo-1576871337632-b9aef4c17ab9?w=600&h=800&fit=crop',
            'Merino wool knit beanie',
            '["Navy", "Black", "Grey"]'::jsonb,
            false,
            true,
            false
        ),
        (
            'MQ-ACC-010',
            'Tech Travel Pouch',
            'Bags',
            42.00,
            'New',
            'https://images.unsplash.com/photo-1585916420730-d7f95e942d43?w=600&h=800&fit=crop',
            'Organized cable & accessory case',
            '["Black", "Charcoal"]'::jsonb,
            false,
            false,
            true
        ),
        (
            'MQ-ACC-011',
            'Leather Keychain',
            'Accessories',
            22.00,
            'New',
            'https://images.unsplash.com/photo-1614676471928-2ed0ad1061a4?w=600&h=800&fit=crop',
            'Premium leather key fob',
            '["Black", "Cognac", "Navy"]'::jsonb,
            false,
            false,
            true
        ),
        (
            'MQ-ACC-012',
            'Minimalist Watch',
            'Accessories',
            195.00,
            'Featured',
            'https://images.unsplash.com/photo-1524805444758-089113d48a6d?w=600&h=800&fit=crop',
            'Sapphire crystal, Japanese movement',
            '["Silver", "Black", "Rose Gold"]'::jsonb,
            true,
            false,
            false
        )
    ) AS t(
        sku,
        name,
        category,
        price,
        badge,
        image_url,
        description,
        color_options,
        is_featured,
        is_best_seller,
        is_new_arrival
    );

    -- Insert new products
    INSERT INTO "MuuqWear".products (
        id,
        name,
        price,
        badge,
        image_url,
        category,
        is_active,
        created_at,
        is_new_arrival,
        is_featured,
        is_best_seller,
        description,
        gender,
        category_id,
        sku,
        is_deleted,
        color_options,
        is_ticket
    )
    SELECT
        gen_random_uuid(),
        s.name,
        s.price,
        COALESCE(s.badge, ''),
        s.image_url,
        s.category,
        true,
        now(),
        s.is_new_arrival,
        s.is_featured,
        s.is_best_seller,
        s.description,
        NULL,
        accessories_category_id,
        s.sku,
        false,
        s.color_options,
        false
    FROM _acc_seed s
    WHERE NOT EXISTS (
        SELECT 1
        FROM "MuuqWear".products p
        WHERE p.sku = s.sku
    );

    -- Refresh existing sample rows (carousel flags, badges, copy, images)
    UPDATE "MuuqWear".products p
    SET
        name = s.name,
        price = s.price,
        badge = COALESCE(s.badge, ''),
        image_url = s.image_url,
        category = s.category,
        description = s.description,
        color_options = s.color_options,
        is_featured = s.is_featured,
        is_best_seller = s.is_best_seller,
        is_new_arrival = s.is_new_arrival,
        is_active = true,
        is_deleted = false,
        category_id = accessories_category_id
    FROM _acc_seed s
    WHERE p.sku = s.sku;

    -- One Size stock for each sample SKU
    INSERT INTO "MuuqWear".product_size_stock (
        id,
        product_id,
        size,
        quantity,
        created_at
    )
    SELECT
        gen_random_uuid(),
        p.id,
        'One Size',
        25,
        now()
    FROM "MuuqWear".products p
    WHERE p.sku LIKE 'MQ-ACC-%'
      AND NOT EXISTS (
          SELECT 1
          FROM "MuuqWear".product_size_stock ps
          WHERE ps.product_id = p.id
      );
END $$;

-- Verify
SELECT
    p.sku,
    p.name,
    p.category,
    p.badge,
    p.price,
    p.is_featured,
    p.is_best_seller,
    p.is_new_arrival,
    ps.size,
    ps.quantity
FROM "MuuqWear".products p
LEFT JOIN "MuuqWear".product_size_stock ps ON ps.product_id = p.id
WHERE p.sku LIKE 'MQ-ACC-%'
ORDER BY p.sku;
