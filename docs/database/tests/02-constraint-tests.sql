-- ============================================================
--  02-constraint-tests.sql — 22 tablonun kısıt testleri
--
--  Blueprint'teki tablo tablo yazılmış "Doğrulama testleri"
--  bloklarının tek akışa çevrilmiş, globalce numaralandırılmış hâli.
--
--  YAPI: her test iki yardımcı fonksiyondan biriyle çalıştırılır.
--    t_ok(...)   → komutun BAŞARILI olması beklenir
--    t_fail(...) → komutun HATA vermesi ve hata metninin verilen
--                  kısıt adını içermesi beklenir
--
--  Her test kendi alt-transaction'ında çalışır (plpgsql BEGIN/EXCEPTION),
--  yani başarısız bir test sonrakileri bozmaz. Sonuçlar _test_sonuc
--  tablosunda toplanır; dosyanın sonunda özet basılır.
--
--  ID'ler açıkça verilir — testler birbirinin id varsayımına güvenir.
--  Ön koşul: 00-reset.sql + schema.sql + 01-seed.sql çalıştırılmış olmalı.
-- ============================================================

DO $$
BEGIN
    IF current_database() <> 'shoeretail_test' THEN
        RAISE EXCEPTION 'GUVENLIK DURDURMASI: yalnizca shoeretail_test. Bagli: %',
            current_database();
    END IF;
END
$$;

DROP TABLE IF EXISTS _test_sonuc;
CREATE TABLE _test_sonuc (
    sira      serial PRIMARY KEY,
    tablo     text,
    no        text,
    aciklama  text,
    beklenen  text,
    sonuc     text,
    detay     text
);

-- Tüm identity sequence'larını tablodaki en büyük id'nin bir üstüne sarar.
-- GEREKLİ: testler açık id ile INSERT yapıyor, bu sequence'ı ilerletmez.
-- Sarılmazsa, id vermeden yapılan sonraki INSERT'ler PK çakışmasına düşer ve
-- test edilmek istenen asıl kısıt yerine "duplicate key" hatası alınır.
CREATE OR REPLACE FUNCTION t_bump() RETURNS void LANGUAGE plpgsql AS $fn$
DECLARE r record;
BEGIN
    FOR r IN
        SELECT c.relname AS tbl, a.attname AS col
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE n.nspname = 'public' AND c.relkind = 'r' AND a.attidentity <> ''
    LOOP
        EXECUTE format(
            'SELECT setval(pg_get_serial_sequence(%L, %L),
                           (SELECT COALESCE(MAX(%I), 0) + 1 FROM %I), false)',
            r.tbl, r.col, r.col, r.tbl);
    END LOOP;
END
$fn$;

-- Başarılı olması beklenen komut
CREATE OR REPLACE FUNCTION t_ok(p_tablo text, p_no text, p_aciklama text, p_sql text)
RETURNS void LANGUAGE plpgsql AS $fn$
BEGIN
    EXECUTE p_sql;
    INSERT INTO _test_sonuc (tablo, no, aciklama, beklenen, sonuc, detay)
    VALUES (p_tablo, p_no, p_aciklama, 'BASARILI', 'GECTI', NULL);
EXCEPTION WHEN others THEN
    INSERT INTO _test_sonuc (tablo, no, aciklama, beklenen, sonuc, detay)
    VALUES (p_tablo, p_no, p_aciklama, 'BASARILI', 'KALDI', SQLERRM);
END
$fn$;

-- Hata vermesi beklenen komut. p_kisit, hata metninde geçmelidir.
CREATE OR REPLACE FUNCTION t_fail(p_tablo text, p_no text, p_aciklama text,
                                  p_kisit text, p_sql text)
RETURNS void LANGUAGE plpgsql AS $fn$
BEGIN
    EXECUTE p_sql;
    INSERT INTO _test_sonuc (tablo, no, aciklama, beklenen, sonuc, detay)
    VALUES (p_tablo, p_no, p_aciklama, 'HATA: ' || p_kisit, 'KALDI',
            'Hata bekleniyordu, komut basarili oldu');
EXCEPTION WHEN others THEN
    IF position(p_kisit in SQLERRM) > 0 THEN
        INSERT INTO _test_sonuc (tablo, no, aciklama, beklenen, sonuc, detay)
        VALUES (p_tablo, p_no, p_aciklama, 'HATA: ' || p_kisit, 'GECTI', NULL);
    ELSE
        INSERT INTO _test_sonuc (tablo, no, aciklama, beklenen, sonuc, detay)
        VALUES (p_tablo, p_no, p_aciklama, 'HATA: ' || p_kisit, 'KALDI',
                'Farkli hata: ' || SQLERRM);
    END IF;
END
$fn$;


-- ════════════════════════════════════════════════════════════
--  1) store_profile
-- ════════════════════════════════════════════════════════════
SELECT t_fail('store_profile','1.1','Ikinci satir eklenemez',
  'chk_store_profile_singleton',
  $$INSERT INTO store_profile (id, store_name) VALUES (2, 'Sahte')$$);
SELECT t_bump();

SELECT t_fail('store_profile','1.2','Kucuk harfli stok kodu oneki reddedilir',
  'chk_store_profile_stock_prefix_format',
  $$UPDATE store_profile SET stock_code_prefix = 'gnd' WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('store_profile','1.3','Bos magaza adi reddedilir',
  'chk_store_profile_name_not_blank',
  $$UPDATE store_profile SET store_name = '   ' WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('store_profile','1.4','Gecersiz para birimi reddedilir',
  'chk_store_profile_currency_format',
  $$UPDATE store_profile SET currency_code = 'try' WHERE id = 1$$);
SELECT t_bump();

-- Trigger testi: caller kasitli olarak eski bir tarih yazmaya calisir,
-- BEFORE UPDATE trigger'i bunu now() ile ezmelidir (bkz. blueprint,
-- "Tekrar Eden Desenler" #6 - Faz 4 karari).
SELECT t_ok('store_profile','1.5','set_updated_at trigger''i eski degeri ezer',
  $$
    DO $do$
    DECLARE v_after timestamptz;
    BEGIN
        UPDATE store_profile SET updated_at = '2000-01-01' WHERE id = 1;
        SELECT updated_at INTO v_after FROM store_profile WHERE id = 1;
        IF v_after < now() - interval '1 minute' THEN
            RAISE EXCEPTION 'trigger calismadi: updated_at = %', v_after;
        END IF;
    END
    $do$
  $$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  2) customers
-- ════════════════════════════════════════════════════════════
SELECT t_fail('customers','2.1','Kurumsal musteri unvansiz olamaz',
  'chk_customers_type_name_consistency',
  $$INSERT INTO customers (customer_type, full_name, phone)
    VALUES ('Corporate', 'Ahmet', '0555 000 00 00')$$);
SELECT t_bump();

SELECT t_fail('customers','2.2','Gecersiz musteri tipi reddedilir',
  'chk_customers_type',
  $$INSERT INTO customers (customer_type, full_name, phone)
    VALUES ('Bayi', 'Ahmet', '0555 000 00 00')$$);
SELECT t_bump();

SELECT t_fail('customers','2.3','Bos telefon reddedilir',
  'chk_customers_phone_not_blank',
  $$INSERT INTO customers (customer_type, full_name, phone)
    VALUES ('Individual', 'Ahmet', '   ')$$);
SELECT t_bump();

SELECT t_fail('customers','2.4','Negatif risk limiti reddedilir',
  'chk_customers_credit_limit_nonneg',
  $$INSERT INTO customers (customer_type, full_name, phone, credit_limit)
    VALUES ('Individual', 'Ahmet', '0555 000 00 00', -100)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  3) suppliers
-- ════════════════════════════════════════════════════════════
SELECT t_fail('suppliers','3.1','Unvansiz uretici reddedilir',
  'company_name',
  $$INSERT INTO suppliers (phone) VALUES ('0555 000 00 00')$$);
SELECT t_bump();

SELECT t_fail('suppliers','3.2','Bos unvan reddedilir',
  'chk_suppliers_company_name_not_blank',
  $$INSERT INTO suppliers (company_name, phone) VALUES ('   ', '0555 000 00 00')$$);
SELECT t_bump();

SELECT t_fail('suppliers','3.3','Negatif termin suresi reddedilir',
  'chk_suppliers_lead_time_nonneg',
  $$INSERT INTO suppliers (company_name, phone, default_lead_time_days)
    VALUES ('Test Uretim', '0555 000 00 00', -5)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  4) users
-- ════════════════════════════════════════════════════════════
SELECT t_fail('users','4.1','Owner rolune supplier_id verilemez',
  'chk_users_role_supplier_consistency',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role, supplier_id)
    VALUES ('test1', 'TEST1', 'h', 'Test', 'Owner', 1)$$);
SELECT t_bump();

SELECT t_fail('users','4.2','GUVENLIK: Manufacturer supplier_id olmadan olusturulamaz',
  'chk_users_role_supplier_consistency',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role)
    VALUES ('test2', 'TEST2', 'h', 'Test', 'Manufacturer')$$);
SELECT t_bump();

SELECT t_fail('users','4.3','Ayni kullanici adi farkli buyuk/kucuk harfle eklenemez',
  'ux_users_normalized_username',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role)
    VALUES ('AGunDogdu', 'AGUNDOGDU', 'h', 'Sahte', 'Owner')$$);
SELECT t_bump();

SELECT t_fail('users','4.4','Turkce karakterli kullanici adi reddedilir',
  'chk_users_username_format',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role)
    VALUES ('isik-ı', 'ISIK', 'h', 'Isik Bey', 'Owner')$$);
SELECT t_bump();

SELECT t_fail('users','4.5','Olmayan ureticiye bagli kullanici reddedilir',
  'fk_users_supplier',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role, supplier_id)
    VALUES ('test3', 'TEST3', 'h', 'Test', 'Manufacturer', 9999)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  5) products
-- ════════════════════════════════════════════════════════════
SELECT t_fail('products','5.1','Ayni stok kodu ikinci kez eklenemez',
  'ux_products_stock_code',
  $$INSERT INTO products (stock_code, name) VALUES ('GND000142', 'Baska Urun')$$);
SELECT t_bump();

SELECT t_fail('products','5.2','Kucuk harfli stok kodu reddedilir',
  'chk_products_stock_code_format',
  $$INSERT INTO products (stock_code, name) VALUES ('gnd000144', 'Test')$$);
SELECT t_bump();

SELECT t_fail('products','5.3','Tire iceren stok kodu reddedilir',
  'chk_products_stock_code_format',
  $$INSERT INTO products (stock_code, name) VALUES ('GND-000145', 'Test')$$);
SELECT t_bump();

SELECT t_fail('products','5.4','Cok kisa rakam kismi reddedilir',
  'chk_products_stock_code_format',
  $$INSERT INTO products (stock_code, name) VALUES ('GND12', 'Test')$$);
SELECT t_bump();

SELECT t_fail('products','5.5','Turkce cinsiyet degeri reddedilir',
  'chk_products_gender',
  $$INSERT INTO products (stock_code, name, gender) VALUES ('GND000148', 'Test', 'Erkek')$$);
SELECT t_bump();

SELECT t_fail('products','5.6','Bos urun adi reddedilir',
  'chk_products_name_not_blank',
  $$INSERT INTO products (stock_code, name) VALUES ('GND000146', '  ')$$);
SELECT t_bump();

SELECT t_fail('products','5.7','Olmayan uretici reddedilir',
  'fk_products_supplier',
  $$INSERT INTO products (stock_code, name, supplier_id) VALUES ('GND000147', 'Test', 9999)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  6) product_variants
-- ════════════════════════════════════════════════════════════
SELECT t_fail('product_variants','6.1','Ayni model+beden+renk ikinci kez eklenemez',
  'ux_product_variants_product_size_color',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (1, '41', 'SİYAH', 700.00, 1000.00)$$);
SELECT t_bump();

SELECT t_fail('product_variants','6.2','Sonunda bosluk olan beden reddedilir (gorunmez mukerrer)',
  'chk_product_variants_size_trimmed',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (1, '44 ', 'SİYAH', 620.00, 950.00)$$);
SELECT t_bump();

SELECT t_fail('product_variants','6.3','Negatif alis fiyati reddedilir',
  'chk_product_variants_purchase_price_nonneg',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (1, '45', 'SİYAH', -10, 950.00)$$);
SELECT t_bump();

SELECT t_ok('product_variants','6.4','Zararina satis KABUL EDILMELI (is karari, DB engellemez)',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (1, '46', 'SİYAH', 620.00, 500.00)$$);
SELECT t_bump();

SELECT t_ok('product_variants','6.5','Barkodlu varyant eklenir',
  $$INSERT INTO product_variants (product_id, size, color, barcode, purchase_price, sale_price)
    VALUES (1, '39', 'SİYAH', '8690000000001', 620, 950)$$);
SELECT t_bump();

SELECT t_fail('product_variants','6.6','Ayni barkod ikinci varyantta reddedilir',
  'ux_product_variants_barcode',
  $$INSERT INTO product_variants (product_id, size, color, barcode, purchase_price, sale_price)
    VALUES (1, '40', 'SİYAH', '8690000000001', 620, 950)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  7) inventory
-- ════════════════════════════════════════════════════════════
SELECT t_fail('inventory','7.1','Negatif stok reddedilir',
  'chk_inventory_on_hand_nonneg',
  $$UPDATE inventory SET quantity_on_hand = -1 WHERE id = 2$$);
SELECT t_bump();

SELECT t_fail('inventory','7.2','Rezerve, eldeki stoktan fazla olamaz',
  'chk_inventory_reserved_le_on_hand',
  $$UPDATE inventory SET quantity_reserved = 999 WHERE id = 1$$);
SELECT t_bump();

SELECT t_ok('inventory','7.3','quantity_available turetilmis kolon dogru hesaplanir',
  $$DO $x$ BEGIN
      IF (SELECT quantity_available FROM inventory WHERE id = 1) <> 17 THEN
        RAISE EXCEPTION 'quantity_available 17 olmaliydi';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  8) orders
-- ════════════════════════════════════════════════════════════
SELECT t_fail('orders','8.1','Ayni siparis numarasi ikinci kez eklenemez',
  'ux_orders_order_number',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id)
    VALUES ('SIP-2026-000142', 1, 1)$$);
SELECT t_bump();

SELECT t_fail('orders','8.2','Gecersiz durum reddedilir',
  'chk_orders_status',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id, status)
    VALUES ('SIP-2026-000900', 1, 1, 'Beklemede')$$);
SELECT t_bump();

SELECT t_fail('orders','8.3','Shipped durumu sevk bilgisi olmadan olamaz',
  'chk_orders_shipped_fields',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id, status)
    VALUES ('SIP-2026-000901', 1, 1, 'Shipped')$$);
SELECT t_bump();

SELECT t_fail('orders','8.4','Cancelled durumu gerekce olmadan olamaz',
  'chk_orders_cancelled_fields',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id, status, cancelled_at, cancelled_by_user_id)
    VALUES ('SIP-2026-000902', 1, 1, 'Cancelled', now(), 1)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  9) order_items
-- ════════════════════════════════════════════════════════════
SELECT t_fail('order_items','9.1','Sifir adet reddedilir',
  'chk_order_items_quantity_positive',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (2, 3, 'GND000142', 'Klasik Erkek Bot', '43', 'SİYAH', 0, 950, 620)$$);
SELECT t_bump();

SELECT t_fail('order_items','9.2','Bos snapshot alani reddedilir',
  'chk_order_items_snapshots_not_blank',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (2, 3, 'GND000142', '  ', '43', 'SİYAH', 1, 950, 620)$$);
SELECT t_bump();

SELECT t_ok('order_items','9.3','line_total turetilmis kolon dogru hesaplanir',
  $$DO $x$ BEGIN
      IF (SELECT line_total FROM order_items WHERE id = 1) <> 95000.00 THEN
        RAISE EXCEPTION 'line_total 95000 olmaliydi';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  10) order_history
-- ════════════════════════════════════════════════════════════
SELECT t_fail('order_history','10.1','Gecersiz olay tipi reddedilir',
  'chk_order_history_event_type',
  $$INSERT INTO order_history (order_id, event_type, changed_by_user_id)
    VALUES (1, 'Silindi', 1)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  11-13) purchase_orders ve alt tablolari
-- ════════════════════════════════════════════════════════════
SELECT t_fail('purchase_orders','11.1','Ayni satin alma numarasi ikinci kez eklenemez',
  'ux_purchase_orders_number',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id)
    VALUES ('ALS-2026-000014', 1, 1)$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','11.2','Draft disi durum sent_at olmadan olamaz',
  'chk_purchase_orders_sent_fields',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id, status)
    VALUES ('ALS-2026-000900', 1, 1, 'InProduction')$$);
SELECT t_bump();

SELECT t_fail('purchase_order_items','12.1','Sifir siparis adedi reddedilir',
  'chk_purchase_order_items_ordered_positive',
  $$INSERT INTO purchase_order_items (purchase_order_id, product_variant_id,
      stock_code_snapshot, product_name_snapshot, size_snapshot, color_snapshot,
      ordered_quantity, unit_purchase_price)
    VALUES (1, 3, 'GND000142', 'Klasik Erkek Bot', '43', 'SİYAH', 0, 620)$$);
SELECT t_bump();

SELECT t_ok('purchase_order_items','12.2','received_total turetilmis kolon dogru (gelmeyen malin parasi yok)',
  $$DO $x$ BEGIN
      IF (SELECT received_total FROM purchase_order_items WHERE id = 1) <> 37200.00 THEN
        RAISE EXCEPTION 'received_total 37200 olmaliydi (60 x 620)';
      END IF;
      IF (SELECT received_total FROM purchase_order_items WHERE id = 2) <> 0 THEN
        RAISE EXCEPTION 'received_total 0 olmaliydi (hic mal gelmedi)';
      END IF;
    END $x$ $$);

SELECT t_fail('purchase_order_history','13.1','Gecersiz olay tipi reddedilir',
  'chk_po_history_event_type',
  $$INSERT INTO purchase_order_history (purchase_order_id, event_type, changed_by_user_id)
    VALUES (1, 'Silindi', 1)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  14) inventory_movements
-- ════════════════════════════════════════════════════════════
SELECT t_fail('inventory_movements','14.1','Ters isaretli rezervasyon reddedilir',
  'chk_inventory_movements_type_signature',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, reserved_delta, order_id, created_by_user_id)
    VALUES (1, 'OrderReservation', -3, 2, 1)$$);
SELECT t_bump();

SELECT t_fail('inventory_movements','14.2','Purchase ile stok azalamaz',
  'chk_inventory_movements_type_signature',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, created_by_user_id)
    VALUES (1, 'Purchase', -10, 1)$$);
SELECT t_bump();

SELECT t_fail('inventory_movements','14.3','Gerekcesiz hasar kaydi reddedilir',
  'chk_inventory_movements_manual_reason',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, created_by_user_id)
    VALUES (1, 'Damaged', -2, 1)$$);
SELECT t_bump();

SELECT t_ok('inventory_movements','14.4','Gerekceli hasar kaydi kabul edilir',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, reason, created_by_user_id)
    VALUES (2, 'Damaged', -2, 'Kutu ezilmis, satilamaz', 1)$$);
SELECT t_bump();

SELECT t_fail('inventory_movements','14.5','Siparise baglanmamis satis reddedilir',
  'chk_inventory_movements_order_link',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, reserved_delta, created_by_user_id)
    VALUES (1, 'Sale', -1, -1, 1)$$);
SELECT t_bump();

SELECT t_ok('inventory_movements','14.6','Siparissiz mal kabul KABUL (mesru senaryo)',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, reason, created_by_user_id)
    VALUES (1, 'Purchase', 2, 'Ureticiden elden alindi, siparis acilmadi', 1)$$);
SELECT t_bump();

SELECT t_fail('inventory_movements','14.7','Gecersiz hareket tipi reddedilir',
  'chk_inventory_movements_type_signature',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, created_by_user_id)
    VALUES (1, 'Sayim', 5, 1)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  15) payment_plans
-- ════════════════════════════════════════════════════════════
SELECT t_ok('payment_plans','15.1','Sevk edilmis siparise odeme plani olusturulur',
  $$INSERT INTO payment_plans (id, order_id, created_by_user_id, notes)
    VALUES (1, 1, 1, 'Pesinat + 4 taksit, musteriyle telefonda mutabik kalindi')$$);
SELECT t_bump();

SELECT t_fail('payment_plans','15.2','Ayni siparise ikinci plan reddedilir (1:1)',
  'ux_payment_plans_order_id',
  $$INSERT INTO payment_plans (order_id, created_by_user_id) VALUES (1, 1)$$);
SELECT t_bump();

SELECT t_fail('payment_plans','15.3','Olmayan siparis reddedilir',
  'fk_payment_plans_order',
  $$INSERT INTO payment_plans (order_id, created_by_user_id) VALUES (9999, 1)$$);
SELECT t_bump();

SELECT t_fail('payment_plans','15.4','Kullanicisiz plan reddedilir',
  'created_by_user_id',
  $$INSERT INTO payment_plans (order_id) VALUES (2)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  16) installments
-- ════════════════════════════════════════════════════════════
SELECT t_ok('installments','16.1','Pesinat + 4 taksitlik plan (toplam 100.000 = siparis tutari)',
  $$INSERT INTO installments (id, payment_plan_id, installment_number, installment_type, amount, due_date) VALUES
      (1, 1, 1, 'DownPayment', 20000.00, DATE '2026-08-18'),
      (2, 1, 2, 'Regular',     20000.00, DATE '2026-09-01'),
      (3, 1, 3, 'Regular',     20000.00, DATE '2026-10-01'),
      (4, 1, 4, 'Regular',     20000.00, DATE '2026-11-01'),
      (5, 1, 5, 'Regular',     20000.00, DATE '2026-12-01')$$);
SELECT t_bump();

SELECT t_ok('installments','16.2','Esit olmayan taksit KABUL (ozel tutar desteklenir)',
  $$INSERT INTO installments (id, payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (6, 1, 6, 'Regular', 7500.50, DATE '2027-01-15')$$);
SELECT t_bump();

SELECT t_fail('installments','16.3','Ayni planda tekrar eden sira numarasi reddedilir',
  'ux_installments_plan_number',
  $$INSERT INTO installments (payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (1, 3, 'Regular', 5000.00, DATE '2027-02-01')$$);
SELECT t_bump();

SELECT t_fail('installments','16.4','Sifir tutarli taksit reddedilir',
  'chk_installments_amount_positive',
  $$INSERT INTO installments (payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (1, 7, 'Regular', 0, DATE '2027-02-01')$$);
SELECT t_bump();

SELECT t_fail('installments','16.5','Gecersiz taksit tipi reddedilir',
  'chk_installments_type',
  $$INSERT INTO installments (payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (1, 8, 'Kapora', 5000.00, DATE '2027-02-01')$$);
SELECT t_bump();

SELECT t_fail('installments','16.6','Sifir sira numarasi reddedilir',
  'chk_installments_number_positive',
  $$INSERT INTO installments (payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (1, 0, 'Regular', 5000.00, DATE '2027-02-01')$$);
SELECT t_bump();

SELECT t_fail('installments','16.7','Olmayan plan reddedilir',
  'fk_installments_payment_plan',
  $$INSERT INTO installments (payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (9999, 1, 'Regular', 5000.00, DATE '2027-02-01')$$);
SELECT t_bump();

SELECT t_ok('installments','16.8','Ayni vadeye iki taksit KABUL (pesinat + ilk taksit ayni gun)',
  $$INSERT INTO installments (id, payment_plan_id, installment_number, installment_type, amount, due_date)
    VALUES (9, 1, 9, 'Regular', 1000.00, DATE '2026-08-18')$$);
SELECT t_bump();

SELECT t_ok('installments','16.9','Dagitimsiz taksit silinebilir (plan duzeltme senaryosu)',
  $$DELETE FROM installments WHERE id = 9$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  17) payments
-- ════════════════════════════════════════════════════════════
SELECT t_ok('payments','17.1','Havale ile tahsilat',
  $$INSERT INTO payments (id, customer_id, amount, payment_method, payment_date, reference_no, created_by_user_id)
    VALUES (1, 1, 25000.00, 'BankTransfer', DATE '2026-08-18', 'DEKONT-9912', 1)$$);
SELECT t_bump();

SELECT t_ok('payments','17.2','Nakit tahsilat, referanssiz',
  $$INSERT INTO payments (id, customer_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (2, 1, 5000.00, 'Cash', DATE '2026-08-19', 1)$$);
SELECT t_bump();

SELECT t_fail('payments','17.3','Negatif tutar reddedilir (ters kayit boyle yapilmaz)',
  'chk_payments_amount_positive',
  $$INSERT INTO payments (customer_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, -5000.00, 'Cash', DATE '2026-08-19', 1)$$);
SELECT t_bump();

SELECT t_fail('payments','17.4','Gecersiz odeme yontemi reddedilir',
  'chk_payments_method',
  $$INSERT INTO payments (customer_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, 1000.00, 'Havale', DATE '2026-08-19', 1)$$);
SELECT t_bump();

SELECT t_fail('payments','17.5','Gerekcesiz iptal reddedilir',
  'chk_payments_reversal_consistency',
  $$UPDATE payments SET status = 'Reversed' WHERE id = 1$$);
SELECT t_bump();

SELECT t_ok('payments','17.6','Tam iptal (kim/ne zaman/neden dolu) kabul edilir',
  $$UPDATE payments
    SET status = 'Reversed', reversed_at = now(),
        reversed_by_user_id = 1, reversal_reason = 'Yanlis musteriye islendi'
    WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('payments','17.7','Aktif kayitta iptal alani dolu olamaz',
  'chk_payments_reversal_consistency',
  $$UPDATE payments SET reversal_reason = 'deneme' WHERE id = 2$$);
SELECT t_bump();

SELECT t_fail('payments','17.8','Olmayan musteri reddedilir',
  'fk_payments_customer',
  $$INSERT INTO payments (customer_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (9999, 1000.00, 'Cash', DATE '2026-08-19', 1)$$);
SELECT t_bump();

SELECT t_ok('payments','17.9','Ayni referans numarasi tekrar KABUL (bilincli, UNIQUE degil)',
  $$INSERT INTO payments (id, customer_id, amount, payment_method, payment_date, reference_no, created_by_user_id)
    VALUES (3, 1, 3000.00, 'BankTransfer', DATE '2026-08-20', 'DEKONT-9912', 1)$$);
SELECT t_bump();

SELECT t_ok('payments','17.10','Gecerli tahsilat toplami 8000 (iptal edilen sayilmaz)',
  $$DO $x$ BEGIN
      IF (SELECT COALESCE(SUM(amount),0) FROM payments WHERE customer_id=1 AND status='Active') <> 8000.00 THEN
        RAISE EXCEPTION 'Aktif tahsilat toplami 8000 olmaliydi';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  18) payment_allocations
-- ════════════════════════════════════════════════════════════
SELECT t_ok('payment_allocations','18.1','Bir odemenin iki taksite bolunmesi',
  $$INSERT INTO payment_allocations (id, payment_id, installment_id, amount) VALUES
      (1, 2, 1, 4000.00),
      (2, 2, 2, 1000.00)$$);
SELECT t_bump();

SELECT t_ok('payment_allocations','18.2','Bir taksidin ikinci odemeyle kismen kapanmasi',
  $$INSERT INTO payment_allocations (id, payment_id, installment_id, amount)
    VALUES (3, 3, 1, 2500.00)$$);
SELECT t_bump();

SELECT t_fail('payment_allocations','18.3','Sifir tutarli dagitim reddedilir',
  'chk_payment_allocations_amount_positive',
  $$INSERT INTO payment_allocations (payment_id, installment_id, amount) VALUES (2, 1, 0)$$);
SELECT t_bump();

SELECT t_fail('payment_allocations','18.4','Negatif dagitim reddedilir',
  'chk_payment_allocations_amount_positive',
  $$INSERT INTO payment_allocations (payment_id, installment_id, amount) VALUES (2, 1, -500)$$);
SELECT t_bump();

SELECT t_fail('payment_allocations','18.5','Olmayan odeme reddedilir',
  'fk_payment_allocations_payment',
  $$INSERT INTO payment_allocations (payment_id, installment_id, amount) VALUES (9999, 1, 100)$$);
SELECT t_bump();

SELECT t_fail('payment_allocations','18.6','KRITIK: dagitimi olan taksit SILINEMEZ',
  'fk_payment_allocations_installment',
  $$DELETE FROM installments WHERE id = 1$$);
SELECT t_bump();

SELECT t_ok('payment_allocations','18.7','Dagitimsiz taksit hala silinebilir',
  $$DELETE FROM installments WHERE id = 6$$);
SELECT t_bump();

SELECT t_ok('payment_allocations','18.8','Ayni odeme+taksit cifti ikinci kez KABUL (UNIQUE yok; toplam odemeyi asmaz)',
  $$INSERT INTO payment_allocations (id, payment_id, installment_id, amount)
    VALUES (4, 3, 1, 500.00)$$);
SELECT t_bump();

SELECT t_ok('payment_allocations','18.9','Taksit 1 odenen tutari 7000 (aktif odemeler)',
  $$DO $x$ BEGIN
      IF (SELECT COALESCE(SUM(pa.amount),0) FROM payment_allocations pa
          JOIN payments p ON p.id = pa.payment_id AND p.status = 'Active'
          WHERE pa.installment_id = 1) <> 7000.00 THEN
        RAISE EXCEPTION 'Taksit 1 odenen tutari 7000 olmaliydi';
      END IF;
    END $x$ $$);

SELECT t_ok('payment_allocations','18.10','Odeme iptal edilince dagitim DURUR ama SAYILMAZ',
  $$DO $x$
    BEGIN
      UPDATE payments SET status='Reversed', reversed_at=now(),
             reversed_by_user_id=1, reversal_reason='Test iptali' WHERE id = 3;

      IF (SELECT COALESCE(SUM(pa.amount),0) FROM payment_allocations pa
          JOIN payments p ON p.id = pa.payment_id AND p.status = 'Active'
          WHERE pa.installment_id = 1) <> 4000.00 THEN
        RAISE EXCEPTION 'Iptal sonrasi odenen 4000 olmaliydi';
      END IF;

      IF (SELECT count(*) FROM payment_allocations WHERE payment_id = 3) <> 2 THEN
        RAISE EXCEPTION 'Dagitim satirlari SILINMEMELIYDI';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  19) account_transactions
-- ════════════════════════════════════════════════════════════
SELECT t_ok('account_transactions','19.1','Siparis sevkiyati borc dogurur',
  $$INSERT INTO account_transactions (id, customer_id, transaction_type, amount, order_id, description, created_by_user_id)
    VALUES (1, 1, 'Sale', 100000.00, 1, 'Siparis SIP-2026-000142 sevkiyati', 1)$$);
SELECT t_bump();

SELECT t_ok('account_transactions','19.2','Tahsilat borcu azaltir',
  $$INSERT INTO account_transactions (id, customer_id, transaction_type, amount, payment_id, description, created_by_user_id)
    VALUES (2, 1, 'Payment', -5000.00, 2, 'Tahsilat #2 (Nakit)', 1)$$);
SELECT t_bump();

SELECT t_ok('account_transactions','19.3','Odeme iptalinin ters kaydi borcu geri yukler',
  $$INSERT INTO account_transactions (id, customer_id, transaction_type, amount, payment_id, description, created_by_user_id)
    VALUES (3, 1, 'Reversal', 5000.00, 2, 'Tahsilat #2 iptali', 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.4','Sale EKSI tutarla reddedilir (isaret hatasi)',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, order_id, created_by_user_id)
    VALUES (1, 'Sale', -100000.00, 1, 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.5','Siparise baglanmamis Sale reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, created_by_user_id)
    VALUES (1, 'Sale', 100000.00, 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.6','Payment ARTI tutarla reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, payment_id, created_by_user_id)
    VALUES (1, 'Payment', 5000.00, 2, 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.7','Gerekcesiz manuel duzeltme reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, created_by_user_id)
    VALUES (1, 'Adjustment', -250.00, 1)$$);
SELECT t_bump();

SELECT t_ok('account_transactions','19.8','Gerekceli manuel duzeltme kabul edilir',
  $$INSERT INTO account_transactions (id, customer_id, transaction_type, amount, description, created_by_user_id)
    VALUES (4, 1, 'Adjustment', -250.00, 'Kur farki duzeltmesi, patron onayi', 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.9','Sifir tutarli hareket reddedilir',
  'chk_account_transactions_amount_nonzero',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Adjustment', 0, 'Test', 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','19.10','Gecersiz hareket tipi reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Iade', -100.00, 'Test', 1)$$);
SELECT t_bump();

SELECT t_ok('account_transactions','19.11','Musteri bakiyesi 99750 (100000 - 5000 + 5000 - 250)',
  $$DO $x$ BEGIN
      IF (SELECT COALESCE(SUM(amount),0) FROM account_transactions WHERE customer_id=1) <> 99750.00 THEN
        RAISE EXCEPTION 'Bakiye 99750 olmaliydi, bulunan: %',
          (SELECT COALESCE(SUM(amount),0) FROM account_transactions WHERE customer_id=1);
SELECT t_bump();
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  20) supplier_payments
-- ════════════════════════════════════════════════════════════
SELECT t_ok('supplier_payments','20.1','Faturaya bagli odeme',
  $$INSERT INTO supplier_payments (id, supplier_id, purchase_order_id, amount, payment_method, payment_date, reference_no, created_by_user_id)
    VALUES (1, 1, 1, 80000.00, 'BankTransfer', DATE '2026-08-20', 'EFT-4417', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_payments','20.2','Toplu odeme (faturasiz) KABUL',
  $$INSERT INTO supplier_payments (id, supplier_id, amount, payment_method, payment_date, notes, created_by_user_id)
    VALUES (2, 1, 200000.00, 'BankTransfer', DATE '2026-08-25', 'Agustos toplu odemesi', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_payments','20.3','Avans (siparis oncesi) KABUL',
  $$INSERT INTO supplier_payments (id, supplier_id, amount, payment_method, payment_date, notes, created_by_user_id)
    VALUES (3, 1, 50000.00, 'Cash', DATE '2026-08-26', 'Siparis oncesi kapora', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','20.4','Negatif tutar reddedilir',
  'chk_supplier_payments_amount_positive',
  $$INSERT INTO supplier_payments (supplier_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, -1000.00, 'Cash', DATE '2026-08-26', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','20.5','Gecersiz odeme yontemi reddedilir',
  'chk_supplier_payments_method',
  $$INSERT INTO supplier_payments (supplier_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, 1000.00, 'EFT', DATE '2026-08-26', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','20.6','Gerekcesiz iptal reddedilir',
  'chk_supplier_payments_reversal_consistency',
  $$UPDATE supplier_payments SET status = 'Reversed' WHERE id = 1$$);
SELECT t_bump();

SELECT t_ok('supplier_payments','20.7','Tam iptal kabul edilir',
  $$UPDATE supplier_payments
    SET status = 'Reversed', reversed_at = now(),
        reversed_by_user_id = 1, reversal_reason = 'Cift odeme yapilmis'
    WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','20.8','Olmayan uretici reddedilir',
  'fk_supplier_payments_supplier',
  $$INSERT INTO supplier_payments (supplier_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (9999, 1000.00, 'Cash', DATE '2026-08-26', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','20.9','Olmayan satin alma siparisi reddedilir',
  'fk_supplier_payments_purchase_order',
  $$INSERT INTO supplier_payments (supplier_id, purchase_order_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, 9999, 1000.00, 'Cash', DATE '2026-08-26', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_payments','20.10','Aktif odeme toplami 250000',
  $$DO $x$ BEGIN
      IF (SELECT COALESCE(SUM(amount),0) FROM supplier_payments WHERE supplier_id=1 AND status='Active') <> 250000.00 THEN
        RAISE EXCEPTION 'Aktif odeme toplami 250000 olmaliydi';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  21) supplier_transactions
-- ════════════════════════════════════════════════════════════
SELECT t_ok('supplier_transactions','21.1','Kismi mal kabul borc dogurur (ilk parti)',
  $$INSERT INTO supplier_transactions (id, supplier_id, transaction_type, amount, purchase_order_id, description, created_by_user_id)
    VALUES (1, 1, 'Purchase', 60000.00, 1, 'PO ilk parti mal kabulu', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_transactions','21.2','Kalan mal kabul borcu artirir',
  $$INSERT INTO supplier_transactions (id, supplier_id, transaction_type, amount, purchase_order_id, description, created_by_user_id)
    VALUES (2, 1, 'Purchase', 40000.00, 1, 'PO kalan mal kabulu', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_transactions','21.3','Odeme borcu azaltir',
  $$INSERT INTO supplier_transactions (id, supplier_id, transaction_type, amount, supplier_payment_id, description, created_by_user_id)
    VALUES (3, 1, 'Payment', -200000.00, 2, 'Agustos toplu odemesi', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_transactions','21.4','Faturaya bagli odeme (her iki kaynak dolu) KABUL',
  $$INSERT INTO supplier_transactions (id, supplier_id, transaction_type, amount, purchase_order_id, supplier_payment_id, description, created_by_user_id)
    VALUES (4, 1, 'Payment', -50000.00, 1, 2, 'PO icin odeme', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','21.5','Purchase EKSI tutarla reddedilir',
  'chk_supplier_transactions_type_signature',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, purchase_order_id, created_by_user_id)
    VALUES (1, 'Purchase', -60000.00, 1, 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','21.6','Siparise baglanmamis mal kabul reddedilir',
  'chk_supplier_transactions_type_signature',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, created_by_user_id)
    VALUES (1, 'Purchase', 60000.00, 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','21.7','Odeme kaydina baglanmamis odeme reddedilir',
  'chk_supplier_transactions_type_signature',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, purchase_order_id, created_by_user_id)
    VALUES (1, 'Payment', -5000.00, 1, 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','21.8','Gerekcesiz manuel duzeltme reddedilir',
  'chk_supplier_transactions_type_signature',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, created_by_user_id)
    VALUES (1, 'Adjustment', 1500.00, 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_transactions','21.9','Siparissiz mal alimi Adjustment ile KABUL',
  $$INSERT INTO supplier_transactions (id, supplier_id, transaction_type, amount, description, created_by_user_id)
    VALUES (5, 1, 'Adjustment', 1500.00, 'Ureticiden elden alinan mal, siparis acilmadi', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','21.10','Sifir tutar reddedilir',
  'chk_supplier_transactions_amount_nonzero',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Adjustment', 0, 'Test', 1)$$);
SELECT t_bump();

SELECT t_ok('supplier_transactions','21.11','Uretici bakiyesi -148500 (fazla odeme / avans durumu)',
  $$DO $x$ BEGIN
      IF (SELECT COALESCE(SUM(amount),0) FROM supplier_transactions WHERE supplier_id=1) <> -148500.00 THEN
        RAISE EXCEPTION 'Bakiye -148500 olmaliydi, bulunan: %',
          (SELECT COALESCE(SUM(amount),0) FROM supplier_transactions WHERE supplier_id=1);
SELECT t_bump();
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  22) audit_logs
-- ════════════════════════════════════════════════════════════
SELECT t_ok('audit_logs','22.1','Fiyat degisikligi (JSONB)',
  $$INSERT INTO audit_logs (id, user_id, action, entity_type, entity_id, old_values, new_values, description, ip_address)
    VALUES (1, 1, 'Update', 'ProductVariant', 1,
            '{"sale_price": 850.00}'::jsonb, '{"sale_price": 950.00}'::jsonb,
            'GND000142-41-SIYAH satis fiyati guncellendi', '192.168.1.42')$$);
SELECT t_bump();

SELECT t_ok('audit_logs','22.2','Siparis olusturma (old_values yok)',
  $$INSERT INTO audit_logs (id, user_id, action, entity_type, entity_id, new_values, description)
    VALUES (2, 1, 'Create', 'Order', 1,
            '{"order_number": "SIP-2026-000142", "total_amount": 100000.00}'::jsonb,
            'Yeni siparis olusturuldu')$$);
SELECT t_bump();

SELECT t_ok('audit_logs','22.3','Basarili giris (entity_id yok)',
  $$INSERT INTO audit_logs (id, user_id, action, entity_type, description, ip_address)
    VALUES (3, 1, 'Login', 'User', 'Basarili giris', '192.168.1.42')$$);
SELECT t_bump();

SELECT t_ok('audit_logs','22.4','KRITIK: basarisiz giris user_id OLMADAN kaydedilir',
  $$INSERT INTO audit_logs (id, action, entity_type, description, ip_address)
    VALUES (4, 'LoginFailed', 'User', 'Basarisiz giris denemesi: kullanici adi "admin"', '203.0.113.7')$$);
SELECT t_bump();

SELECT t_fail('audit_logs','22.5','Bos action reddedilir',
  'chk_audit_logs_action_not_blank',
  $$INSERT INTO audit_logs (user_id, action, entity_type, description)
    VALUES (1, '   ', 'Order', 'Test')$$);
SELECT t_bump();

SELECT t_fail('audit_logs','22.6','Olmayan kullanici reddedilir',
  'fk_audit_logs_user',
  $$INSERT INTO audit_logs (user_id, action, entity_type, entity_id)
    VALUES (9999, 'Update', 'Order', 1)$$);
SELECT t_bump();

SELECT t_ok('audit_logs','22.7','Olmayan entity_id KABUL (polimorfik tasarimin bilincli bedeli)',
  $$INSERT INTO audit_logs (id, user_id, action, entity_type, entity_id, description)
    VALUES (5, 1, 'Delete', 'Installment', 99999, 'Silinmis kaydin izi')$$);
SELECT t_bump();

SELECT t_ok('audit_logs','22.8','GIZLILIK: password_hash hicbir kayitta loglanmamis',
  $$DO $x$ BEGIN
      IF (SELECT count(*) FROM audit_logs
          WHERE old_values ? 'password_hash' OR new_values ? 'password_hash') <> 0 THEN
        RAISE EXCEPTION 'password_hash loglanmis! AuditService kara listesi bozuk.';
      END IF;
    END $x$ $$);


-- ════════════════════════════════════════════════════════════
--  24) KAPSAM TAMAMLAMA
--
--  İlk turdan sonra kapsam ölçüldü: 129 kısıttan yalnızca 65'i bir testle
--  yoklanıyordu. Aşağıdakiler kalan boşluğu kapatır. Özellikle üç unique
--  index gerçek bir iş kuralıdır ve hiç test edilmiyordu.
-- ════════════════════════════════════════════════════════════

-- ── Benzersizlik kuralları (teknik değil, iş kuralı) ──
SELECT t_fail('inventory','24.1','Bir varyantin ikinci envanter satiri olamaz',
  'ux_inventory_product_variant_id',
  $$INSERT INTO inventory (product_variant_id, quantity_on_hand) VALUES (1, 5)$$);
SELECT t_bump();

SELECT t_fail('order_items','24.2','Ayni siparise ayni varyant iki kez eklenemez',
  'ux_order_items_order_variant',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (1, 1, 'GND000142', 'Klasik Erkek Bot', '41', 'SİYAH', 5, 950, 620)$$);
SELECT t_bump();

SELECT t_fail('purchase_order_items','24.3','Ayni satin alma siparisine ayni varyant iki kez eklenemez',
  'ux_purchase_order_items_order_variant',
  $$INSERT INTO purchase_order_items (purchase_order_id, product_variant_id,
      stock_code_snapshot, product_name_snapshot, size_snapshot, color_snapshot,
      ordered_quantity, unit_purchase_price)
    VALUES (1, 1, 'GND000142', 'Klasik Erkek Bot', '41', 'SİYAH', 10, 620)$$);
SELECT t_bump();

-- ── Durum ↔ zorunlu alan tutarlılığı ──
SELECT t_fail('orders','24.4','Delivered durumu delivered_at olmadan olamaz',
  'chk_orders_delivered_fields',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id, status, shipped_at, shipped_by_user_id)
    VALUES ('SIP-2026-000910', 1, 1, 'Delivered', now(), 1)$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','24.5','Completed durumu tamamlama bilgisi olmadan olamaz',
  'chk_purchase_orders_completed_fields',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id, status, sent_at)
    VALUES ('ALS-2026-000910', 1, 1, 'Completed', now())$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','24.6','Cancelled durumu gerekce olmadan olamaz',
  'chk_purchase_orders_cancelled_fields',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id, status, cancelled_at, cancelled_by_user_id)
    VALUES ('ALS-2026-000911', 1, 1, 'Cancelled', now(), 1)$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','24.7','Gecersiz satin alma durumu reddedilir',
  'chk_purchase_orders_status',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id, status, sent_at)
    VALUES ('ALS-2026-000912', 1, 1, 'Yolda', now())$$);
SELECT t_bump();

-- NOT: chk_payments_status tek basina IHLAL EDILEMEZ. 'Active'/'Reversed'
-- disindaki her deger, reversal_consistency kisitinin iki dalina da uymadigi
-- icin once ona takilir. Yani status kisiti savunma derinligi/belgelemedir.
-- Test, satirin reddedildigini dogrular; hangi kisitin yakaladigi degil.
SELECT t_fail('payments','24.8','Gecersiz odeme durumu reddedilir (herhangi bir kisit yakalar)',
  'chk_payments_',
  $$INSERT INTO payments (customer_id, amount, payment_method, payment_date, status, created_by_user_id)
    VALUES (1, 100, 'Cash', CURRENT_DATE, 'Iptal', 1)$$);
SELECT t_bump();

-- Ayni gerekce (bkz. 24.8).
SELECT t_fail('supplier_payments','24.9','Gecersiz odeme durumu reddedilir (herhangi bir kisit yakalar)',
  'chk_supplier_payments_',
  $$INSERT INTO supplier_payments (supplier_id, amount, payment_method, payment_date, status, created_by_user_id)
    VALUES (1, 100, 'Cash', CURRENT_DATE, 'Iptal', 1)$$);
SELECT t_bump();

-- ── Metin normalizasyonu ve format ──
SELECT t_fail('product_variants','24.10','Basinda bosluk olan renk reddedilir (gorunmez mukerrer)',
  'chk_product_variants_color_trimmed',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (2, '38', ' TABA', 310, 480)$$);
SELECT t_bump();

SELECT t_fail('users','24.11','Kucuk harfli normalized_username reddedilir',
  'chk_users_normalized_username_format',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role)
    VALUES ('kucuk', 'kucuk', 'h', 'Test', 'Owner')$$);
SELECT t_bump();

SELECT t_fail('users','24.12','Bos ad-soyad reddedilir',
  'chk_users_full_name_not_blank',
  $$INSERT INTO users (username, normalized_username, password_hash, full_name, role)
    VALUES ('bosad', 'BOSAD', 'h', '   ', 'Owner')$$);
SELECT t_bump();

SELECT t_fail('suppliers','24.13','Bos telefon reddedilir',
  'chk_suppliers_phone_not_blank',
  $$INSERT INTO suppliers (company_name, phone) VALUES ('Test Firma', '   ')$$);
SELECT t_bump();

SELECT t_fail('products','24.14','Gecersiz sezon reddedilir',
  'chk_products_season',
  $$INSERT INTO products (stock_code, name, season) VALUES ('GND000950', 'Test', 'Ilkbahar')$$);
SELECT t_bump();

SELECT t_fail('audit_logs','24.15','Bos entity_type reddedilir',
  'chk_audit_logs_entity_type_not_blank',
  $$INSERT INTO audit_logs (user_id, action, entity_type) VALUES (1, 'Update', '  ')$$);
SELECT t_bump();

-- ── Sayısal alt sınırlar ──
SELECT t_fail('store_profile','24.16','Hane sayisi araligi disinda reddedilir',
  'chk_store_profile_stock_digits_range',
  $$UPDATE store_profile SET stock_code_digits = 12 WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('store_profile','24.17','Negatif kritik stok esigi reddedilir',
  'chk_store_profile_low_stock_nonneg',
  $$UPDATE store_profile SET default_low_stock_threshold = -1 WHERE id = 1$$);
SELECT t_bump();

SELECT t_fail('customers','24.18','Negatif vade reddedilir',
  'chk_customers_payment_term_nonneg',
  $$INSERT INTO customers (customer_type, full_name, phone, default_payment_term_days)
    VALUES ('Individual', 'Test', '0555 000 00 00', -1)$$);
SELECT t_bump();

SELECT t_fail('suppliers','24.19','Negatif vade reddedilir',
  'chk_suppliers_payment_term_nonneg',
  $$INSERT INTO suppliers (company_name, phone, default_payment_term_days)
    VALUES ('Test Firma', '0555 000 00 00', -1)$$);
SELECT t_bump();

SELECT t_fail('inventory','24.20','Negatif rezerve reddedilir',
  'chk_inventory_reserved_nonneg',
  $$UPDATE inventory SET quantity_reserved = -1 WHERE id = 2$$);
SELECT t_bump();

SELECT t_fail('inventory','24.21','Negatif kritik stok esigi reddedilir',
  'chk_inventory_low_stock_nonneg',
  $$UPDATE inventory SET low_stock_threshold = -1 WHERE id = 2$$);
SELECT t_bump();

SELECT t_fail('orders','24.22','Negatif siparis tutari reddedilir',
  'chk_orders_total_nonneg',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id, total_amount)
    VALUES ('SIP-2026-000913', 1, 1, -1)$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','24.23','Negatif satin alma tutari reddedilir',
  'chk_purchase_orders_total_nonneg',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id, total_amount)
    VALUES ('ALS-2026-000913', 1, 1, -1)$$);
SELECT t_bump();

SELECT t_fail('order_items','24.24','Negatif satis fiyati reddedilir',
  'chk_order_items_sale_price_nonneg',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (2, 4, 'GND000143', 'Kadin Babet', '37', 'TABA', 1, -1, 310)$$);
SELECT t_bump();

SELECT t_fail('order_items','24.25','Negatif alis fiyati reddedilir',
  'chk_order_items_purchase_price_nonneg',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (2, 4, 'GND000143', 'Kadin Babet', '37', 'TABA', 1, 480, -1)$$);
SELECT t_bump();

SELECT t_fail('product_variants','24.26','Negatif satis fiyati reddedilir',
  'chk_product_variants_sale_price_nonneg',
  $$INSERT INTO product_variants (product_id, size, color, purchase_price, sale_price)
    VALUES (2, '39', 'TABA', 310, -1)$$);
SELECT t_bump();

SELECT t_fail('purchase_order_items','24.27','Negatif birim fiyat reddedilir',
  'chk_purchase_order_items_price_nonneg',
  $$INSERT INTO purchase_order_items (purchase_order_id, product_variant_id,
      stock_code_snapshot, product_name_snapshot, size_snapshot, color_snapshot,
      ordered_quantity, unit_purchase_price)
    VALUES (1, 4, 'GND000143', 'Kadin Babet', '37', 'TABA', 5, -1)$$);
SELECT t_bump();

SELECT t_fail('purchase_order_items','24.28','Negatif kabul adedi reddedilir',
  'chk_purchase_order_items_received_nonneg',
  $$UPDATE purchase_order_items SET received_quantity = -1 WHERE id = 2$$);
SELECT t_bump();

SELECT t_fail('purchase_order_items','24.29','Bos snapshot alani reddedilir',
  'chk_purchase_order_items_snapshots_not_blank',
  $$INSERT INTO purchase_order_items (purchase_order_id, product_variant_id,
      stock_code_snapshot, product_name_snapshot, size_snapshot, color_snapshot,
      ordered_quantity, unit_purchase_price)
    VALUES (1, 4, 'GND000143', '  ', '37', 'TABA', 5, 310)$$);
SELECT t_bump();

-- ── Foreign key ailesinden örneklem ──
-- 36 FK'nin mekaniği aynı (ON DELETE RESTRICT + olmayan parent reddi).
-- Her ailenin bir temsilcisi test ediliyor; hepsini yazmak kapsamı
-- rakamsal olarak şişirir ama gerçek bilgi katmaz.
SELECT t_fail('orders','24.30','Olmayan musteriye siparis reddedilir',
  'fk_orders_customer',
  $$INSERT INTO orders (order_number, customer_id, created_by_user_id)
    VALUES ('SIP-2026-000914', 9999, 1)$$);
SELECT t_bump();

SELECT t_fail('order_items','24.31','Olmayan varyanta siparis kalemi reddedilir',
  'fk_order_items_product_variant',
  $$INSERT INTO order_items (order_id, product_variant_id, stock_code_snapshot,
      product_name_snapshot, size_snapshot, color_snapshot, quantity, unit_sale_price, unit_purchase_price)
    VALUES (2, 9999, 'GND000142', 'Test', '41', 'SİYAH', 1, 950, 620)$$);
SELECT t_bump();

SELECT t_fail('inventory','24.32','Olmayan varyanta envanter reddedilir',
  'fk_inventory_product_variant',
  $$INSERT INTO inventory (product_variant_id, quantity_on_hand) VALUES (9999, 5)$$);
SELECT t_bump();

SELECT t_fail('purchase_orders','24.33','Olmayan ureticiye satin alma reddedilir',
  'fk_purchase_orders_supplier',
  $$INSERT INTO purchase_orders (purchase_order_number, supplier_id, created_by_user_id)
    VALUES ('ALS-2026-000914', 9999, 1)$$);
SELECT t_bump();

SELECT t_fail('account_transactions','24.34','Olmayan musteriye cari hareket reddedilir',
  'fk_account_transactions_customer',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, order_id, description, created_by_user_id)
    VALUES (9999, 'Sale', 100, 1, 'Test', 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','24.35','Olmayan ureticiye cari hareket reddedilir',
  'fk_supplier_transactions_supplier',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, purchase_order_id, description, created_by_user_id)
    VALUES (9999, 'Purchase', 100, 1, 'Test', 1)$$);
SELECT t_bump();

SELECT t_fail('inventory_movements','24.36','Olmayan varyanta stok hareketi reddedilir',
  'fk_inventory_movements_variant',
  $$INSERT INTO inventory_movements (product_variant_id, movement_type, on_hand_delta, reason, created_by_user_id)
    VALUES (9999, 'ManualIncrease', 1, 'Test', 1)$$);
SELECT t_bump();

SELECT t_fail('order_history','24.37','Olmayan siparise gecmis kaydi reddedilir',
  'fk_order_history_order',
  $$INSERT INTO order_history (order_id, event_type, changed_by_user_id)
    VALUES (9999, 'Created', 1)$$);
SELECT t_bump();

SELECT t_fail('payments','24.38','Olmayan kullaniciyla odeme reddedilir',
  'fk_payments_created_by',
  $$INSERT INTO payments (customer_id, amount, payment_method, payment_date, created_by_user_id)
    VALUES (1, 100, 'Cash', CURRENT_DATE, 9999)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  ÖZET
-- ════════════════════════════════════════════════════════════
\echo ''
\echo '════════════ SONUC ════════════'
SELECT sonuc, count(*) AS adet FROM _test_sonuc GROUP BY sonuc ORDER BY sonuc;

\echo ''
\echo '════════════ KALAN TESTLER (bos ise hepsi gecti) ════════════'
SELECT no, tablo, aciklama, beklenen, detay
FROM _test_sonuc WHERE sonuc = 'KALDI' ORDER BY sira;


-- ════════════════════════════════════════════════════════════
--  23) NULL-CHECK REGRESYON TESTLERİ
--
--  İlk test turunda bulunan hata: btrim(NULL) <> '' sonucu NULL'dır ve
--  PostgreSQL, CHECK sonucu NULL olan satırı GEÇERLİ sayar. Guard'sız
--  yazılmış dört kısıt, gerekçesiz kayıtları sessizce kabul ediyordu.
--  Bu testler o deliğin geri gelmesini engeller.
-- ════════════════════════════════════════════════════════════
SELECT t_fail('account_transactions','23.1','REGRESYON: Adjustment description NULL ile reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Adjustment', -250.00, NULL, 1)$$);
SELECT t_bump();

SELECT t_fail('supplier_transactions','23.2','REGRESYON: Adjustment description NULL ile reddedilir',
  'chk_supplier_transactions_type_signature',
  $$INSERT INTO supplier_transactions (supplier_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Adjustment', 1500.00, NULL, 1)$$);
SELECT t_bump();

SELECT t_fail('payments','23.3','REGRESYON: Reversed, at/by dolu ama reason NULL reddedilir',
  'chk_payments_reversal_consistency',
  $$UPDATE payments SET status='Reversed', reversed_at=now(),
      reversed_by_user_id=1, reversal_reason=NULL WHERE id=2$$);
SELECT t_bump();

SELECT t_fail('supplier_payments','23.4','REGRESYON: Reversed, at/by dolu ama reason NULL reddedilir',
  'chk_supplier_payments_reversal_consistency',
  $$UPDATE supplier_payments SET status='Reversed', reversed_at=now(),
      reversed_by_user_id=1, reversal_reason=NULL WHERE id=2$$);
SELECT t_bump();

SELECT t_fail('account_transactions','23.5','REGRESYON: Adjustment bos string ile de reddedilir',
  'chk_account_transactions_type_signature',
  $$INSERT INTO account_transactions (customer_id, transaction_type, amount, description, created_by_user_id)
    VALUES (1, 'Adjustment', -250.00, '   ', 1)$$);
SELECT t_bump();


-- ════════════════════════════════════════════════════════════
--  ÖZET
-- ════════════════════════════════════════════════════════════
\echo ''
\echo '════════════ SONUC ════════════'
SELECT sonuc, count(*) AS adet FROM _test_sonuc GROUP BY sonuc ORDER BY sonuc;

\echo ''
\echo '════════════ KALAN TESTLER (bos ise hepsi gecti) ════════════'
SELECT no, tablo, aciklama, beklenen, detay
FROM _test_sonuc WHERE sonuc = 'KALDI' ORDER BY sira;
