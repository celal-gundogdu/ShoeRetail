-- ============================================================
--  00-reset.sql — Temiz sayfa
--
--  public şemasını tamamen düşürüp yeniden oluşturur. Ardından
--  schema.sql çalıştırılır (runner bunu ayrı adımda yapar).
--
--  ⚠️ SADECE shoeretail_test veritabanında çalıştırılmalıdır.
--     Aşağıdaki koruma, yanlış veritabanına bağlıyken çalışmayı engeller.
-- ============================================================

DO $$
BEGIN
    IF current_database() <> 'shoeretail_test' THEN
        RAISE EXCEPTION
            'GUVENLIK DURDURMASI: bu script yalnizca shoeretail_test icin. Bagli olunan: %',
            current_database();
    END IF;
END
$$;

-- IF EXISTS / IF NOT EXISTS bilinçli: bir önceki koşu DROP ile CREATE arasında
-- yarıda kesilirse şema yok kalır ve script bir daha asla çalışmaz.
-- (Bu tam olarak başımıza geldi — idempotent olmayan reset, kendini kilitler.)
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA IF NOT EXISTS public;

SELECT 'public semasi sifirlandi' AS durum;
