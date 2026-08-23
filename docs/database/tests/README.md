# Veritabanı Test Paketi

`schema.sql`'in gerçekten çalıştığını ve kısıtların gerçekten koruduğunu doğrular.

## Çalıştırma

```
powershell -ExecutionPolicy Bypass -File docs\database\tests\run-tests.ps1
```

Hedef veritabanı **`shoeretail_test`** (port 5433, `shoeretail` rolü). Her koşu
şemayı sıfırdan kurar — mevcut veri silinir. Üç script de yanlış veritabanına
bağlıyken çalışmayı reddeden bir güvenlik kontrolüyle başlar.

## Dosyalar

| Dosya | İş |
|---|---|
| `00-reset.sql` | `public` şemasını düşürüp yeniden oluşturur (idempotent) |
| `01-seed.sql` | Tablo 1-14 için asgari veri, açık `id`'lerle |
| `02-constraint-tests.sql` | 168 kısıt testi — tek satır doğruluğu |
| `03-golden-tests.sql` | 11 mutabakat testi — tablolar arası tutarlılık |
| `run-tests.ps1` | Hepsini sırayla çalıştırır, PASS/FAIL özeti basar |

## Test motoru

`02` içinde üç yardımcı fonksiyon var:

- **`t_ok(tablo, no, açıklama, sql)`** — komutun başarılı olması beklenir
- **`t_fail(tablo, no, açıklama, kısıt, sql)`** — komutun hata vermesi **ve** hata
  metninin verilen kısıt adını içermesi beklenir
- **`t_bump()`** — tüm identity sequence'larını ileri sarar

Her test kendi alt-transaction'ında (`BEGIN ... EXCEPTION`) çalışır; başarısız bir
test sonrakileri bozmaz. Sonuçlar `_test_sonuc` / `_altin_sonuc` tablolarında toplanır.

### Neden `t_bump()` gerekiyor

Testler açık `id` ile INSERT yapıyor, bu identity sequence'ını **ilerletmez**.
Sarılmazsa, `id` vermeden yapılan sonraki INSERT'ler PK çakışmasına düşer ve test
edilmek istenen asıl kısıt yerine `duplicate key` hatası alınır — test yeşil
görünmez ama **yanlış sebeple** kırmızı olur. İlk koşuda tam olarak bu oldu.

## Bilinen tuzaklar (hepsi bir kez yaşandı)

| Tuzak | Belirti | Çözüm |
|---|---|---|
| `END $x$$$)` | psql bunu `$x` + `$$` diye ayrıştırır, ortada `$` kalır | Araya boşluk: `END $x$ $$)` |
| Açık `id` + sequence | Alakasız `duplicate key` hataları | Her testten sonra `t_bump()` |
| Idempotent olmayan reset | `DROP` ile `CREATE` arasında kesilirse script kendini kilitler | `DROP SCHEMA IF EXISTS` / `CREATE SCHEMA IF NOT EXISTS` |
| PowerShell 5.1 + native `2>&1` | `NOTICE` bile hata sayılır, script durur | Yönlendirmeyi `cmd /c` içinde yap |

## `03` içindeki meta-test (M.1)

Şemanın kendisini denetler: nullable bir kolonda guard'sız `btrim()` içeren `CHECK`
kısıtı arar. Böyle bir kısıt satırı **sessizce kabul eder**, çünkü `btrim(NULL) <> ''`
sonucu `NULL`'dır ve PostgreSQL `NULL` sonuçlu `CHECK`'i geçerli sayar.

Bu hata dört kısıtta gerçekten vardı ve ilk test koşusunda yakalandı
(`account_transactions`, `supplier_transactions`, `payments`, `supplier_payments`).

> ⚠️ M.1 **sezgisel** bir testtir, ispat değil: guard başka bir `OR` dalında duruyorsa
> yanlış olarak "temiz" der. Yeni kısıt yazarken gözle de kontrol et.

## Kapsam — ve neyin test EDİLMEDİĞİ

```
Toplam kısıt + unique index : 129
Bir testle yoklanan         : 102  (%79)
Açık                        :  27
```

Açık olanların **26'sı foreign key**. Hepsinin mekaniği aynı (`ON DELETE RESTRICT`,
olmayan parent reddi); her aileden bir temsilci test ediliyor. Kalan 26'yı yazmak
kapsam rakamını şişirir ama yeni bilgi vermez. Kalan 1 tanesi
`chk_supplier_payments_status` — tek başına **ihlal edilemez**, çünkü
`Active`/`Reversed` dışındaki her değer önce `reversal_consistency` kısıtına takılır.

### 🔴 Bu paket şunları test ETMEZ

Veritabanı yeşil olması, sistemin doğru çalışacağı anlamına gelmez:

| Alan | Neden burada değil | Nerede |
|---|---|---|
| Çok satırlı iş kuralları | `SUM(installments) = order.total_amount` gibi kurallar Application katmanında zorlanır; DB engellemez | Faz 14 |
| Eşzamanlılık | "Son 1 adet için iki eşzamanlı sipariş" senaryosu transaction/kilit gerektirir | Faz 4+ |
| `inventory` ↔ `inventory_movements` senkronu | Bu senkronizasyon uygulama kodunun işi, henüz yazılmadı | Faz 8 |
| Yetkilendirme / gizlilik | Üreticinin başkasının verisini görememesi API katmanında | Faz 5 / 18 |
| ~~`updated_at` tazeleme~~ | ~~Trigger mı EF `SaveChanges` mi~~ | **Çözüldü (Faz 4): trigger. Test 1.5 + M.2** |
| Performans / index etkinliği | Gerçek veri hacmi yok, ölçüm anlamsız | Faz 19 |

Yani bu paketin verdiği güvence şudur: **şema kurulur ve tek satırlık kurallar
gerçekten korur.** Fazlası değil.

## `inventory` ↔ `inventory_movements`

`03` bunu **bilgi olarak** raporlar, test olarak değil. Kısıt testleri doğrudan
`inventory_movements`'a satır yazıyor ama `inventory`'yi güncellemiyor — bu
senkronizasyon Application katmanının işi (Faz 8) ve henüz yazılmadı.
**Faz 19'da bu bir test olacak.**
