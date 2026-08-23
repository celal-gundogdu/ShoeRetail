-- ============================================================
--  03-golden-tests.sql — Mutabakat (altın) testleri
--
--  Kısıt testleri "tek satır doğru mu?" diye sorar.
--  Bunlar "tablolar birbiriyle tutarlı mı?" diye sorar — çok satırlı,
--  çok tablolu kurallar. Faz 19'un provası.
--
--  Kural: aşağıdaki sorguların hepsi 0 satır döndürmelidir.
--  Satır dönerse Application katmanındaki transaction bütünlüğü bozuk demektir.
--
--  Ön koşul: run-tests.ps1 ile tüm zincir çalıştırılmış olmalı.
-- ============================================================

DO $$
BEGIN
    IF current_database() <> 'shoeretail_test' THEN
        RAISE EXCEPTION 'GUVENLIK DURDURMASI: yalnizca shoeretail_test. Bagli: %',
            current_database();
    END IF;
END
$$;

DROP TABLE IF EXISTS _altin_sonuc;
CREATE TABLE _altin_sonuc (
    sira     serial PRIMARY KEY,
    no       text,
    aciklama text,
    ihlal    bigint,
    sonuc    text
);

-- 0 satır dönmesi beklenen sorguyu çalıştırır.
CREATE OR REPLACE FUNCTION g_bos(p_no text, p_aciklama text, p_sql text)
RETURNS void LANGUAGE plpgsql AS $fn$
DECLARE n bigint;
BEGIN
    EXECUTE 'SELECT count(*) FROM (' || p_sql || ') s' INTO n;
    INSERT INTO _altin_sonuc (no, aciklama, ihlal, sonuc)
    VALUES (p_no, p_aciklama, n, CASE WHEN n = 0 THEN 'GECTI' ELSE 'KALDI' END);
EXCEPTION WHEN others THEN
    INSERT INTO _altin_sonuc (no, aciklama, ihlal, sonuc)
    VALUES (p_no, p_aciklama, -1, 'HATA: ' || SQLERRM);
END
$fn$;


-- ════════════════════════════════════════════════════════════
--  META: şemanın kendisini denetleyen test
-- ════════════════════════════════════════════════════════════
-- Guard'sız btrim(): nullable kolonda CHECK sonucu NULL olur ve satır
-- SESSİZCE kabul edilir. Bu hata gerçekten yaşandı (bkz. blueprint,
-- "Tekrar Eden Desenler" #5). Yeni bir kısıt aynı hatayla eklenirse
-- bu test yakalar.
SELECT g_bos('M.1',
  'Guardsiz btrim() iceren CHECK kisiti (NULL sessizce gecer)',
  $q$
    SELECT c.conname
    FROM pg_constraint c
    JOIN pg_class t ON t.oid = c.conrelid
    JOIN pg_namespace n ON n.oid = t.relnamespace
    WHERE n.nspname = 'public'
      AND c.contype = 'c'
      AND pg_get_constraintdef(c.oid) LIKE '%btrim%'
      AND EXISTS (
          -- btrim() uygulanan kolonlardan en az biri nullable ve
          -- kisit metninde onun icin IS NOT NULL guard'i yok.
          --
          -- IKI YAZIM BICIMI VAR (pg_get_constraintdef ciktisi):
          --   text    kolon -> btrim(description)
          --   varchar kolon -> btrim((company_name)::text)
          -- Ikisini de yakalamak sart; sadece birini aramak testi sessizce
          -- ise yaramaz hale getirir (bu hata bir kez yapildi ve yakalandi).
          SELECT 1
          FROM pg_attribute a
          WHERE a.attrelid = t.oid
            AND a.attnum > 0
            AND NOT a.attisdropped
            AND NOT a.attnotnull
            AND (   pg_get_constraintdef(c.oid) LIKE '%btrim('  || a.attname || '%'
                 OR pg_get_constraintdef(c.oid) LIKE '%btrim((' || a.attname || '%')
            AND pg_get_constraintdef(c.oid) NOT LIKE '%' || a.attname || ' IS NOT NULL%'
      )
      -- SEZGISEL TEST, ISPAT DEGIL: guard baska bir OR dalinda duruyorsa
      -- bu sorgu yanlis olarak "temiz" der. Yeni kisit yazarken gozle de bak.
  $q$);


-- ════════════════════════════════════════════════════════════
--  FİNANS: perakendeci tarafı
-- ════════════════════════════════════════════════════════════
SELECT g_bos('F.1',
  'Asiri dagitilmis odeme (dagitim > odeme tutari)',
  $q$
    SELECT p.id
    FROM payments p
    JOIN payment_allocations pa ON pa.payment_id = p.id
    GROUP BY p.id, p.amount
    HAVING SUM(pa.amount) > p.amount
  $q$);

SELECT g_bos('F.2',
  'Fazla odenmis taksit (aktif dagitim > taksit tutari)',
  $q$
    SELECT i.id
    FROM installments i
    JOIN payment_allocations pa ON pa.installment_id = i.id
    JOIN payments p ON p.id = pa.payment_id AND p.status = 'Active'
    GROUP BY i.id, i.amount
    HAVING SUM(pa.amount) > i.amount
  $q$);

SELECT g_bos('F.3',
  'Odeme plani toplami siparis tutarindan farkli',
  $q$
    SELECT pp.id
    FROM payment_plans pp
    JOIN orders o ON o.id = pp.order_id
    LEFT JOIN installments i ON i.payment_plan_id = pp.id
    GROUP BY pp.id, o.total_amount
    HAVING COALESCE(SUM(i.amount), 0) <> o.total_amount
  $q$);

SELECT g_bos('F.4',
  'Aktif tahsilat toplami ile defterdeki Payment toplami uyusmuyor',
  $q$
    SELECT c.id
    FROM customers c
    WHERE (SELECT COALESCE(SUM(p.amount), 0) FROM payments p
           WHERE p.customer_id = c.id AND p.status = 'Active')
        <> (SELECT COALESCE(-SUM(at.amount), 0) FROM account_transactions at
            WHERE at.customer_id = c.id AND at.transaction_type = 'Payment')
  $q$);

SELECT g_bos('F.5',
  'Kaynak belgesi olmayan Sale veya Payment defter satiri',
  $q$
    SELECT id FROM account_transactions
    WHERE (transaction_type = 'Sale'    AND order_id   IS NULL)
       OR (transaction_type = 'Payment' AND payment_id IS NULL)
  $q$);


-- ════════════════════════════════════════════════════════════
--  FİNANS: üretici tarafı
-- ════════════════════════════════════════════════════════════
SELECT g_bos('F.6',
  'Aktif uretici odemesi toplami ile defterdeki Payment toplami uyusmuyor',
  $q$
    SELECT s.id
    FROM suppliers s
    WHERE (SELECT COALESCE(SUM(sp.amount), 0) FROM supplier_payments sp
           WHERE sp.supplier_id = s.id AND sp.status = 'Active')
        <> (SELECT COALESCE(-SUM(st.amount), 0) FROM supplier_transactions st
            WHERE st.supplier_id = s.id AND st.transaction_type = 'Payment')
  $q$);

SELECT g_bos('F.7',
  'Siparise baglanmamis Purchase defter satiri',
  $q$
    SELECT id FROM supplier_transactions
    WHERE transaction_type = 'Purchase' AND purchase_order_id IS NULL
  $q$);


-- ════════════════════════════════════════════════════════════
--  BÜTÜNLÜK: sipariş / stok
-- ════════════════════════════════════════════════════════════
SELECT g_bos('B.1',
  'Sevk edilmis siparis, sevk bilgisi eksik',
  $q$
    SELECT id FROM orders
    WHERE status IN ('Shipped','Delivered')
      AND (shipped_at IS NULL OR shipped_by_user_id IS NULL)
  $q$);

SELECT g_bos('B.2',
  'Mal kabul edilen adet, siparis edilenden fazla',
  $q$
    SELECT id FROM purchase_order_items
    WHERE received_quantity > ordered_quantity
  $q$);

SELECT g_bos('B.3',
  'Rezerve stok eldeki stoktan fazla',
  $q$
    SELECT id FROM inventory WHERE quantity_reserved > quantity_on_hand
  $q$);


-- ════════════════════════════════════════════════════════════
--  BİLGİ (test değil): inventory ↔ inventory_movements
--
--  Bu mutabakat şu an KASITLI olarak tutmuyor. Sebep: kısıt testleri
--  doğrudan inventory_movements'a satır yazıyor ama inventory'yi
--  güncellemiyor — bu senkronizasyon Application katmanının işi
--  (Faz 8) ve henüz yazılmadı. Faz 19'da bu bir TEST olacak.
-- ════════════════════════════════════════════════════════════
\echo ''
\echo '---- BILGI: inventory <-> inventory_movements farki (Faz 8 sonrasi 0 olmali) ----'
SELECT i.product_variant_id,
       i.quantity_on_hand                       AS envanter,
       COALESCE(SUM(m.on_hand_delta), 0)        AS hareket_toplami,
       i.quantity_on_hand - COALESCE(SUM(m.on_hand_delta), 0) AS fark
FROM inventory i
LEFT JOIN inventory_movements m ON m.product_variant_id = i.product_variant_id
GROUP BY i.product_variant_id, i.quantity_on_hand
ORDER BY i.product_variant_id;


-- ════════════════════════════════════════════════════════════
--  ÖZET
-- ════════════════════════════════════════════════════════════
\echo ''
\echo '============ ALTIN TEST SONUCU ============'
SELECT no, aciklama, ihlal, sonuc FROM _altin_sonuc ORDER BY sira;
