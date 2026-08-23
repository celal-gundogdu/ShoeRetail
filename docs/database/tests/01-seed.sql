-- ============================================================
--  01-seed.sql — Asgari test verisi
--
--  KAPSAM: Tablo 1-14 (temel iş verisi). Finans tabloları (15-22)
--  bilinçli olarak BOŞ bırakılır — onların satırlarını
--  02-constraint-tests.sql kendi testleri içinde oluşturur.
--
--  ID'ler AÇIKÇA verilir; testler sabit id'lere güvenebilsin diye.
--  Sonda tüm identity sequence'ları elle ileri sarılır — yoksa
--  sonraki otomatik INSERT'ler çakışır.
-- ============================================================

DO $$
BEGIN
    IF current_database() <> 'shoeretail_test' THEN
        RAISE EXCEPTION 'GUVENLIK DURDURMASI: yalnizca shoeretail_test. Bagli: %',
            current_database();
    END IF;
END
$$;

-- ── 1) store_profile (satır schema.sql tarafından oluşturuldu) ──
UPDATE store_profile
SET store_name = 'Gündoğdu Ayakkabı',
    phone      = '0212 555 00 00',
    updated_at = now()
WHERE id = 1;

-- ── 2) customers ──
INSERT INTO customers (id, customer_type, company_name, phone, city, default_payment_term_days, credit_limit)
VALUES (1, 'Corporate', 'ABC Ayakkabı Ltd.', '0212 555 11 22', 'İstanbul', 60, 250000.00);

INSERT INTO customers (id, customer_type, full_name, phone, city)
VALUES (2, 'Individual', 'Mehmet Yılmaz', '0532 111 22 33', 'Konya');

-- ── 3) suppliers ──
INSERT INTO suppliers (id, company_name, phone, city, default_payment_term_days, default_lead_time_days)
VALUES (1, 'Anadolu Ayakkabı San. Ltd.', '0332 444 55 66', 'Konya', 90, 30);

INSERT INTO suppliers (id, company_name, phone, contact_person)
VALUES (2, 'Ege Deri A.Ş.', '0232 333 22 11', 'Hasan Bey');

-- ── 4) users ──
INSERT INTO users (id, username, normalized_username, password_hash, full_name, role)
VALUES (1, 'agundogdu', 'AGUNDOGDU', 'dummy_hash_owner', 'Ahmet Gündoğdu', 'Owner');

INSERT INTO users (id, username, normalized_username, password_hash, full_name, role, supplier_id)
VALUES (2, 'anadolu', 'ANADOLU', 'dummy_hash_mfr', 'Anadolu Ayakkabı', 'Manufacturer', 1);

-- ── 5) products ──
INSERT INTO products (id, stock_code, name, brand, category, gender, season, supplier_id, supplier_product_code)
VALUES (1, 'GND000142', 'Klasik Erkek Bot', 'Gündoğdu', 'Bot', 'Men', 'Winter', 1, 'MDL-7734-B');

INSERT INTO products (id, stock_code, name)
VALUES (2, 'GND000143', 'Kadın Babet');

-- ── 6) product_variants ──
INSERT INTO product_variants (id, product_id, size, color, purchase_price, sale_price) VALUES
  (1, 1, '41', 'SİYAH', 620.00, 950.00),
  (2, 1, '42', 'SİYAH', 620.00, 950.00),
  (3, 1, '43', 'SİYAH', 620.00, 950.00),
  (4, 2, '37', 'TABA',  310.00, 480.00);

-- ── 7) inventory ──
INSERT INTO inventory (id, product_variant_id, quantity_on_hand, quantity_reserved, low_stock_threshold) VALUES
  (1, 1, 20, 3, 5),
  (2, 2, 15, 0, 5),
  (3, 3,  8, 0, 5),
  (4, 4, 40, 0, 5);

-- ── 8) orders ──
-- Sevk edilmiş sipariş: ödeme planı ve cari hareket testleri buna bağlanacak.
INSERT INTO orders (id, order_number, customer_id, created_by_user_id, order_date,
                    status, total_amount, shipped_at, shipped_by_user_id)
VALUES (1, 'SIP-2026-000142', 1, 1, DATE '2026-08-15',
        'Shipped', 100000.00, timestamptz '2026-08-18 09:00+03', 1);

-- Henüz sevk edilmemiş sipariş
INSERT INTO orders (id, order_number, customer_id, created_by_user_id, order_date, status, total_amount)
VALUES (2, 'SIP-2026-000143', 2, 1, DATE '2026-08-20', 'Received', 2850.00);

-- ── 9) order_items ──
INSERT INTO order_items (id, order_id, product_variant_id, stock_code_snapshot,
                         product_name_snapshot, size_snapshot, color_snapshot,
                         quantity, unit_sale_price, unit_purchase_price)
VALUES (1, 1, 1, 'GND000142', 'Klasik Erkek Bot', '41', 'SİYAH', 100, 950.00, 620.00),
       (2, 2, 2, 'GND000142', 'Klasik Erkek Bot', '42', 'SİYAH',   3, 950.00, 620.00);

-- ── 10) order_history ──
INSERT INTO order_history (id, order_id, event_type, new_value, changed_by_user_id) VALUES
  (1, 1, 'Created', 'Received', 1),
  (2, 1, 'StatusChanged', 'Shipped', 1);

-- ── 11) purchase_orders ──
INSERT INTO purchase_orders (id, purchase_order_number, supplier_id, created_by_user_id,
                             order_date, expected_delivery_date, payment_due_date,
                             status, total_amount, sent_at)
VALUES (1, 'ALS-2026-000014', 1, 1, DATE '2026-07-20', DATE '2026-08-19', DATE '2026-11-17',
        'Sent', 100000.00, timestamptz '2026-07-20 11:00+03');

-- ── 12) purchase_order_items ──
INSERT INTO purchase_order_items (id, purchase_order_id, product_variant_id,
                                  stock_code_snapshot, product_name_snapshot,
                                  size_snapshot, color_snapshot, supplier_product_code,
                                  ordered_quantity, received_quantity, unit_purchase_price)
VALUES (1, 1, 1, 'GND000142', 'Klasik Erkek Bot', '41', 'SİYAH', 'MDL-7734-B', 100, 60, 620.00),
       (2, 1, 2, 'GND000142', 'Klasik Erkek Bot', '42', 'SİYAH', 'MDL-7734-B',  62, 0,  620.00);

-- ── 13) purchase_order_history ──
INSERT INTO purchase_order_history (id, purchase_order_id, event_type, new_value, changed_by_user_id) VALUES
  (1, 1, 'Created', 'Draft', 1),
  (2, 1, 'StatusChanged', 'Sent', 1);

-- ── 14) inventory_movements ──
INSERT INTO inventory_movements (id, product_variant_id, movement_type, on_hand_delta,
                                 reserved_delta, order_id, purchase_order_id,
                                 reason, created_by_user_id) VALUES
  (1, 1, 'InitialStock',     20, 0, NULL, NULL, 'Açılış sayımı', 1),
  (2, 1, 'OrderReservation',  0, 3, 2,    NULL, NULL, 1);

-- ============================================================
--  Identity sequence'larını ileri sar.
--  Açık id ile INSERT yapıldığında sequence ilerlemez; bu adım
--  atlanırsa sonraki otomatik INSERT'ler "duplicate key" verir.
-- ============================================================
DO $$
DECLARE r record;
BEGIN
    FOR r IN
        SELECT c.relname AS tbl, a.attname AS col
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'public'
          AND c.relkind = 'r'
          AND a.attidentity <> ''
    LOOP
        EXECUTE format(
            'SELECT setval(pg_get_serial_sequence(%L, %L),
                           (SELECT COALESCE(MAX(%I), 0) + 1 FROM %I),
                           false)',
            r.tbl, r.col, r.col, r.tbl);
    END LOOP;
END
$$;

SELECT 'seed tamam' AS durum,
       (SELECT count(*) FROM customers)        AS musteri,
       (SELECT count(*) FROM suppliers)        AS uretici,
       (SELECT count(*) FROM product_variants) AS varyant,
       (SELECT count(*) FROM orders)           AS siparis;
