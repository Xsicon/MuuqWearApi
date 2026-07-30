-- Optional seed for help_articles (18 articles from HelpArticleSeed).
-- Run AFTER create-help-articles.sql. Skips insert when any row already exists.

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "MuuqWear".help_articles LIMIT 1) THEN
        RAISE NOTICE 'help_articles already has data — seed skipped.';
        RETURN;
    END IF;

    INSERT INTO "MuuqWear".help_articles
        (title, category, content, status, view_count, helpful_count, published_at)
    VALUES
        ('How do I track my order?', 'Orders', 'Once shipped, you''ll receive an email with your tracking number. You can also check order status in your account dashboard under Order History.', 'published', 4821, 423, now()),
        ('Can I cancel my order?', 'Orders', 'Orders can be cancelled within 1 hour of placement. After that, we begin processing immediately and cancellations are no longer possible.', 'published', 3204, 287, now()),
        ('How long does processing take?', 'Orders', 'Most orders are processed and shipped within 24 hours during business days (Monday–Friday).', 'published', 2891, 260, now()),
        ('Do you ship internationally?', 'Shipping', 'Yes, we ship to 40+ countries. International shipping rates and delivery times vary by destination. You can see estimated costs at checkout.', 'published', 5102, 480, now()),
        ('Is shipping free?', 'Shipping', 'Standard shipping is free on orders over $150 USD within the continental US. Express and international shipping fees apply.', 'published', 3988, 361, now()),
        ('Can I change my shipping address?', 'Shipping', 'Address changes can be made within 1 hour of placing your order. Contact us immediately via live chat if your order needs to be redirected.', 'published', 1890, 172, now()),
        ('What is your return policy?', 'Returns', 'We accept returns within 30 days of delivery. Items must be unworn with all original tags still attached. Final sale items are not eligible.', 'published', 6240, 589, now()),
        ('How do I start a return?', 'Returns', 'Log into your account, go to Order History, select the item you wish to return, and click ''Start Return''. You''ll receive a prepaid return label via email.', 'published', 4102, 390, now()),
        ('When will I receive my refund?', 'Returns', 'Refunds are processed within 5–7 business days after we receive your return. You''ll receive an email confirmation when your refund is issued.', 'published', 3521, 312, now()),
        ('What payment methods do you accept?', 'Payments', 'We accept all major credit/debit cards (Visa, Mastercard, Amex), PayPal, Apple Pay, Google Pay, and Muuqwear Gift Cards.', 'published', 2980, 270, now()),
        ('Is my payment information secure?', 'Payments', 'Absolutely. All payments are processed through Stripe, which holds PCI DSS Level 1 certification — the highest level of payment security.', 'published', 1740, 161, now()),
        ('Do you offer gift cards?', 'Payments', 'Yes! Digital gift cards are available in $25, $50, $100, and $200 denominations. They can be purchased and redeemed at checkout.', 'published', 2210, 198, now()),
        ('How do I reset my password?', 'Account', 'Click ''Forgot Password'' on the login page. Enter your email address and we''ll send a reset link. The link is valid for 24 hours.', 'published', 3100, 290, now()),
        ('Can I update my email address?', 'Account', 'Yes, you can change your email address in Account Settings under your profile. A confirmation email will be sent to your new address.', 'published', 1560, 144, now()),
        ('How do I delete my account?', 'Account', 'Contact privacy@muuqwear.com with your account email and we''ll process your deletion request within 30 days per GDPR requirements.', 'draft', 0, 0, NULL),
        ('How do I find my size?', 'Product Info', 'Check our Size Guide page for detailed measurements and fit recommendations. Our garments are cut to a European standard — when in doubt, size up.', 'published', 4500, 415, now()),
        ('What materials do you use?', 'Product Info', 'We use organic cotton, recycled polyester, merino wool, and our proprietary AeroWeave technical fabric. Each product page lists the exact fabric composition.', 'published', 3800, 352, now()),
        ('How should I care for my garments?', 'Product Info', 'Each item includes care label instructions. Generally: machine wash cold (30°C), hang to dry. Do not tumble dry or iron on prints.', 'published', 2900, 265, now());
END $$;
