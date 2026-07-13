-- Atomic affiliate payout processing (run after create-affiliate-payouts.sql).
-- IDEMPOTENT: CREATE OR REPLACE.
-- Exposes public.process_affiliate_payout for PostgREST RPC.

CREATE OR REPLACE FUNCTION public.process_affiliate_payout(
    p_affiliate_code text,
    p_processed_by uuid,
    p_payment_method text DEFAULT 'manual',
    p_admin_notes text DEFAULT NULL
)
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, "MuuqWear"
AS $$
DECLARE
    v_profile_id uuid;
    v_affiliate_name text;
    v_canonical_code text;
    v_total numeric(12,2);
    v_count integer;
    v_payout_id uuid;
    v_now timestamptz := timezone('utc', now());
    v_updated integer;
BEGIN
    IF p_affiliate_code IS NULL OR length(trim(p_affiliate_code)) = 0 THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', 'Affiliate code is required');
    END IF;

    IF p_processed_by IS NULL THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', 'Not authenticated');
    END IF;

    -- Serialize per affiliate profile
    SELECT p.id, p.full_name, p.affiliate_code
    INTO v_profile_id, v_affiliate_name, v_canonical_code
    FROM "MuuqWear".profiles p
    WHERE lower(p.affiliate_code) = lower(trim(p_affiliate_code))
      AND lower(coalesce(p.affiliate_application_status, '')) = 'approved'
    FOR UPDATE;

    IF v_profile_id IS NULL THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', 'Affiliate not found');
    END IF;

    -- Lock all pending referrals for this affiliate
    PERFORM 1
    FROM "MuuqWear".affiliate_referrals r
    WHERE lower(r.affiliate_code) = lower(v_canonical_code)
      AND r.status = 'pending'
    FOR UPDATE;

    SELECT coalesce(sum(r.commission_amount), 0), count(*)::integer
    INTO v_total, v_count
    FROM "MuuqWear".affiliate_referrals r
    WHERE lower(r.affiliate_code) = lower(v_canonical_code)
      AND r.status = 'pending';

    IF v_count = 0 THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', 'No pending commissions for this affiliate');
    END IF;

    IF v_total <= 0 THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', 'Payout total must be greater than zero');
    END IF;

    v_payout_id := gen_random_uuid();

    INSERT INTO "MuuqWear".affiliate_payouts (
        id,
        affiliate_code,
        profile_id,
        total_amount,
        referral_count,
        status,
        payment_method,
        admin_notes,
        processed_at,
        processed_by
    ) VALUES (
        v_payout_id,
        v_canonical_code,
        v_profile_id,
        v_total,
        v_count,
        'completed',
        coalesce(nullif(trim(p_payment_method), ''), 'manual'),
        nullif(trim(p_admin_notes), ''),
        v_now,
        p_processed_by
    );

    UPDATE "MuuqWear".affiliate_referrals r
    SET
        status = 'paid',
        paid_at = v_now,
        payout_id = v_payout_id,
        processed_by = p_processed_by
    WHERE lower(r.affiliate_code) = lower(v_canonical_code)
      AND r.status = 'pending';

    GET DIAGNOSTICS v_updated = ROW_COUNT;

    IF v_updated <> v_count THEN
        RAISE EXCEPTION 'Payout referral update mismatch (expected %, updated %)',
            v_count, v_updated;
    END IF;

    RETURN jsonb_build_object(
        'success', true,
        'payout_id', v_payout_id,
        'affiliate_code', v_canonical_code,
        'affiliate_name', coalesce(v_affiliate_name, v_canonical_code),
        'total_amount', v_total,
        'referral_count', v_count,
        'processed_at', v_now,
        'status', 'completed',
        'payment_method', coalesce(nullif(trim(p_payment_method), ''), 'manual')
    );
END;
$$;

GRANT EXECUTE ON FUNCTION public.process_affiliate_payout(text, uuid, text, text)
    TO anon, authenticated, service_role;

CREATE OR REPLACE FUNCTION public.count_affiliate_pending_payout_affiliates()
RETURNS integer
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public, "MuuqWear"
AS $$
    SELECT count(*)::integer
    FROM (
        SELECT r.affiliate_code
        FROM "MuuqWear".affiliate_referrals r
        INNER JOIN "MuuqWear".profiles p
            ON lower(p.affiliate_code) = lower(r.affiliate_code)
        WHERE r.status = 'pending'
          AND lower(coalesce(p.affiliate_application_status, '')) = 'approved'
          AND coalesce(r.commission_amount, 0) > 0
        GROUP BY r.affiliate_code
    ) x;
$$;

GRANT EXECUTE ON FUNCTION public.count_affiliate_pending_payout_affiliates()
    TO anon, authenticated, service_role;
