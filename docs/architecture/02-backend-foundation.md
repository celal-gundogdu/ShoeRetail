# FAZ 4 — Backend Temeli (Solution, EF Core, Migration'lar)

> Bu belge `docs/database/02-physical-blueprint.md`'nin backend tarafındaki karşılığıdır:
> tablo tablo değil, karar karar — Faz 4'te alınan her mimari kararın *neden*i.
> `CLAUDE.md` bu belgenin sıkıştırılmış özetini tutar; ayrıntı ve gerekçe burada.

**Durum:** ✅ Faz 4 tamamlandı (2026-08-24). Sonraki adım: Faz 5 (JWT auth + RBAC).

---

## 1. Solution Yapısı ve Referans Grafiği

```
ShoeRetail.sln (repo kökü, .slnx formatı — .NET 10'un yeni varsayılanı)
├── src/ShoeRetail.Domain          # POCO entity'ler, iş kavramları — hiçbir şeye bağımlı değil
├── src/ShoeRetail.Application     # (henüz boş) iş kuralları — Faz 5+
├── src/ShoeRetail.Infrastructure  # EF Core, PostgreSQL, DI kaydı → Application + Domain'e bağımlı
├── src/ShoeRetail.Contracts       # (henüz boş) API DTO'ları, gizlilik sınırı
├── src/ShoeRetail.Api             # ASP.NET Core Web API → Application + Infrastructure + Contracts'a bağımlı
├── src/ShoeRetail.Desktop         # WPF → SADECE Contracts'a bağımlı
├── tests/ShoeRetail.Domain.Tests  # → Domain
└── tests/ShoeRetail.Api.Tests     # → Api (transitif olarak Infrastructure'a da erişir)
```

**Neden bu referans yönü:** `CLAUDE.md` §3'teki "immutable architecture rules"ın derleyici
seviyesinde zorlanmasıdır. Özellikle **Desktop'ın Domain/Infrastructure'a referansı yok** —
WPF projesine yanlışlıkla `DbContext` kullanan bir satır yazılırsa proje derlenmez. Bu,
"WPF sıfır yetkiye sahip, DB'ye asla direkt erişmez" kuralının kod incelemesine değil
derleyiciye bırakılmış hâlidir.

**Alternatif değerlendirildi, elendi:** Tek proje (`ShoeRetail.Api` içinde her şey).
Bu ölçekte cazip görünür ama Faz 18'deki Blazor portalı eklenirken (aynı Application/
Infrastructure'ı Api ile paylaşması gerekecek) yeniden bölmeyi zorunlu kılardı. Katmanlama
maliyeti şimdi düşük, sonradan eklemenin maliyeti yüksek.

---

## 2. Paket Seçimleri

| Paket | Sürüm | Nerede | Neden |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | 10.0.11 | Infrastructure | .NET 10 ile aynı major, stabil |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Infrastructure | Resmi PostgreSQL sağlayıcısı |
| `EFCore.NamingConventions` | 10.0.1 | Infrastructure | Aşağıya bakınız |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Api | `dotnet ef` migration araçları için (yalnızca derleme zamanı) |
| `dotnet-ef` | 10.0.11 | Yerel araç (`dotnet-tools.json`) | Global değil — `dotnet tool restore` ile her makinede aynı sürüm garanti |

**`EFCore.NamingConventions` neden var:** `schema.sql` `snake_case` (`store_name`), C# tarafı
`PascalCase` (`StoreName`). 22 tabloda ~130 sütun için elle `HasColumnName(...)` yazmak
yerine bu paket otomatik çeviriyor. `CLAUDE.md` §10'daki "EF Core naming convention
bridges" ifadesi zaten bunu işaret ediyordu.

**`dotnet-ef` neden global değil, yerel araç:** Global araç sürümü makineden makineye
sapabilir; `dotnet-tools.json` repo'ya commit'lenip her geliştiricinin/CI'ın aynı sürümü
kullanmasını garanti eder. Çalıştırma: `dotnet tool run dotnet-ef ...` (veya `dotnet
tool restore` sonrası `dotnet ef ...`).

---

## 3. Bağlantı Dizesi Stratejisi

**Karar:** `appsettings.json`'da sadece boş bir `ConnectionStrings:Default` anahtarı durur
(şeklin belgesi). Gerçek şifre yalnızca **.NET User Secrets**'ta (`%APPDATA%` altında,
repo dışında, asla git'e giremez).

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Password=..." --project src/ShoeRetail.Api
```

**Neden:** `CLAUDE.md` §3 kural #4 — hiçbir şey hardcode edilmeyecek. `pgpass.conf`'un
`psql` için yaptığını User Secrets .NET tarafı için yapıyor. `.gitignore` zaten
`appsettings.Development.json` / `appsettings.Local.json`'ı dışlıyordu; User Secrets bunun
resmi .NET karşılığı ve dosya proje klasörünün bile dışında yaşıyor (yanlışlıkla `git add`
ile commit'lenme riski sıfır).

DI kaydı `src/ShoeRetail.Infrastructure/DependencyInjection.cs`'deki `AddInfrastructure()`
extension'ında yaşıyor — Api sadece çağırıyor, Npgsql'in var olduğunu bile bilmiyor.

---

## 4. İki Veritabanı: `shoeretail_test` vs `shoeretail_dev`

Faz 3'te tek veritabanı vardı (`shoeretail_test`), `run-tests.ps1` tarafından her
çalıştırıldığında silinip `schema.sql`'den sıfırdan kuruluyordu.

**Sorun:** EF Core migration'ları kendi geçmişini (`__EFMigrationsHistory` tablosu) aynı
veritabanında tutar. `shoeretail_test`'i migration hedefi de yaparsak, her `run-tests.ps1`
çalıştığında bu geçmiş silinir ama tablolar `schema.sql`'den geri gelir — iki sistem
(ham SQL test harness'i ve EF migration geçmişi) sessizce senkronsuz kalır.

**Karar (kullanıcıyla birlikte alındı):** Ayrı, kalıcı bir **`shoeretail_dev`** veritabanı
açıldı (2026-08-24, `shoeretail` rolüne sahip). `shoeretail_test` **sadece**
`run-tests.ps1`'in ham SQL test harness'i olarak kalır — EF migration'ları veya uygulama
asla ona dokunmaz. `shoeretail_dev` EF Core migration'larının sahibidir; Api'nin User
Secrets'ındaki bağlantı dizesi buraya işaret eder.

`CLAUDE.md` §1'deki eski not ("There is no shoeretail_dev") artık geçersiz — o not sadece
belgedeki yanlış bir iddiayı düzeltiyordu, "asla ikinci bir DB açılmayacak" diye bir karar
değildi.

---

## 5. Entity Eşleme Yaklaşımı

Her tablo için: `src/ShoeRetail.Domain/<Tablo>.cs` (sade POCO) +
`src/ShoeRetail.Infrastructure/Persistence/Configurations/<Tablo>Configuration.cs`
(`IEntityTypeConfiguration<T>`, Fluent API).

**Bilinçli kapsam kısıtlaması: navigation property YOK.** Sadece FK skaler alanları
(`Order.CustomerId` gibi), `Order.Customer` gibi navigation property'ler yok. Tüm FK
ilişkileri `HasOne<Target>().WithMany().HasForeignKey(...)` ile "gölge" (shadow)
ilişki olarak tanımlandı — hem kaynak hem hedefte navigation property gerektirmeyen,
EF Core'un tam desteklediği bir kalıp.

**Neden:** Hız — 21 tabloyu tek oturumda bitirmek için gerçek sorgu ihtiyaçları henüz
bilinmiyorken (Application katmanı henüz yok) baştan tam bir navigation grafiği kurmak
erken soyutlama olurdu. `Order.Items` gibi koleksiyon navigation'ları Faz 7+'de gerçek
ekran ihtiyaçları netleşince eklenecek.

**CHECK kısıtları birebir taşındı:** `schema.sql`'deki her `CONSTRAINT chk_...` aynı isim
ve aynı SQL ifadesiyle `ToTable(tb => tb.HasCheckConstraint(...))` içinde tekrar
tanımlandı — 73/73 isim `diff` ile doğrulandı (bkz. §7).

**Bilinen küçük sapma:** EF Core'un Npgsql sağlayıcısı her FK kolonuna otomatik index
ekliyor (konvansiyon). Bu yüzden `shoeretail_dev`'de blueprint'in açıkça listelemediği
birkaç fazladan index var (ör. `ix_supplier_payments_reversed_by_user_id`). Zararsız
(eksik değil, fazladan) ama `schema.sql` ile birebir aynı değil.

---

## 6. `updated_at` Otomatik Tazeleme

Blueprint'te "Faz 4'te karar verilecek" diye açık bırakılmıştı (bkz.
`02-physical-blueprint.md` "Tekrar Eden Desenler" #6). **Karar: PostgreSQL `BEFORE
UPDATE` trigger'ı**, EF Core `SaveChanges` override'ı değil.

**Gerekçe:** Blueprint'in kendi "DB Kısıtı mı, Uygulama Kuralı mı?" ölçütüne göre
`updated_at = now()` tek satırlı/tek tablolu bir kuraldır → DB'nin işi. Trigger ayrıca
kaynağı ne olursa olsun (EF Core, psql, pgAdmin) her UPDATE'i kapsar; `SaveChanges`
override'ı yalnızca C# tarafından geçeni yakalardı.

**EF Core entegrasyonu:** `updated_at` kolonu olan 14 entity'nin `UpdatedAt` property'si
`ValueGeneratedOnAddOrUpdate()` ile işaretlendi. Bu sayede EF Core bu kolona hiç yazmıyor;
UPDATE sonrası trigger'ın hesapladığı gerçek değeri Npgsql'in `RETURNING` desteğiyle geri
okuyor. Migration: `AddUpdatedAtTriggers` (trigger fonksiyonu + 14 trigger, ham SQL —
EF'in Fluent API'sinde trigger karşılığı yok, `migrationBuilder.Sql(...)` kullanıldı).

Tam gerekçe ve DDL: `docs/database/02-physical-blueprint.md` "Tekrar Eden Desenler" #6,
`docs/database/schema.sql` ("updated_at OTOMATİK TAZELEME" bölümü).

---

## 7. Test Stratejisi

İki bağımsız test katmanı, ikisi de gerçek PostgreSQL'e karşı çalışır (mock yok):

1. **SQL test paketi** (`docs/database/tests/`, `shoeretail_test`'e karşı) — şemanın
   kendisini doğrular: 169 kısıt testi + 12 altın (mutabakat) testi. `updated_at`
   trigger'ı için hem fonksiyonel test (1.5: trigger eski değeri eziyor mu) hem meta-test
   (M.2: `updated_at` kolonu olan her tablonun trigger'ı var mı) eklendi.
2. **EF Core entegrasyon testleri** (`tests/ShoeRetail.Api.Tests/AppDbContextTests.cs`,
   `shoeretail_dev`'e karşı) — gerçek `DbContext.SaveChangesAsync()` döngüsünün DB
   varsayılanlarını ve trigger'ı doğru okuduğunu kanıtlar. Api'nin User Secrets kimliğini
   (`dc5140c4-d28d-4a5b-8f03-102f408513f4`) kullanır, testler kendi yazdığı satırı
   `finally` bloğunda siler — `shoeretail_dev` her zaman temiz kalır.

**Neden iki ayrı katman:** SQL paketi "şema doğru mu" sorusuna cevap verir (DB
kısıtlarının ta kendisi test edilir). EF testleri "uygulama şemayı doğru mu kullanıyor"
sorusuna cevap verir (ORM'in üretim conventions'ı — value generation, computed column
geri okuma — gerçekten çalışıyor mu). Sadece SQL paketini geçirmek, EF tarafında bir
yapılandırma hatası olmadığını KANITLAMAZ; StoreProfile'ın ilk halinde bu ayrım
düşünülmemişti, `updated_at` kararıyla birlikte netleşti.

---

## 8. Bilinen Ortam Notu: .NET 10 PATH Çakışması

Geliştirme makinesinde iki ayrı `dotnet.exe` kurulumu var: eski bir .NET 9 SDK
(`C:\Program Files (x86)\dotnet-sdk-9.0.302-win-x64\`, standart olmayan bir konumda) PATH'te
yeni .NET 10'dan (`C:\Program Files\dotnet\`) önce geliyor. Düzeltmek admin yetkisi
gerektiriyor (bu oturumda yoktu). Ayrıntı ve kalıcı çözüm: `CLAUDE.md` §1.

**Bu belgeyi/projeyi devralan biri için:** `dotnet --version` yanlış sürüm gösteriyorsa
şaşırmayın — tam yolu kullanın veya PATH'i admin ile düzeltin.

---

## 9. Bilinçli Olarak Ertelenenler

| Ne | Neden şimdi değil | Ne zaman |
|---|---|---|
| Navigation property'ler | Gerçek sorgu ihtiyaçları henüz bilinmiyor | Faz 7+ |
| JWT auth / RBAC | Roadmap'te ayrı faz | Faz 5 |
| Serilog / global exception handling | Roadmap'te ayrı faz, erken eklemek "birden fazla fazı otomatik atlamak" olurdu | Faz 17 |
| `AuditService` (audit_logs'a yazan kod) | Aynı sebep | Faz 17 |
| Stok rezervasyonu için `SELECT ... FOR UPDATE` kilitleme | Henüz kilitlenecek bir iş akışı (sipariş girişi) yok — kod yazmak spekülatif olurdu | Faz 10 |
