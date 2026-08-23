# ShoeRetail — Proje Tanımı v2 (Pivot Sonrası)

> Bu belge, `archive/v1-pre-pivot-spec.txt` yerine geçer. İş modeli değiştiği için
> proje tanımı yeniden yazılmıştır. Arşivdeki belge sadece tarihsel referanstır.
>
> Değişikliğin özeti için `archive/README.md`.

---

## 1. İŞ MODELİ

Biz bir **ayakkabı toptancısı / distribütörüyüz**. Üreticilerden mal alır,
perakendeci mağazalara satarız.

```
   ÜRETİCİLER                     BİZ (TOPTANCI)                PERAKENDECİLER
   (birkaç firma)                                               (çok sayıda mağaza)

   ┌───────────┐                 ┌──────────────┐               ┌───────────┐
   │ Üretici A │ ◀── sipariş ─── │              │ ── sevkiyat ─▶│ Mağaza 1  │
   ├───────────┤                 │   STOK       │               ├───────────┤
   │ Üretici B │ ─── mal ──────▶ │   DEPO       │ ◀── ödeme ────│ Mağaza 2  │
   ├───────────┤                 │   CARİ HESAP │               ├───────────┤
   │ Üretici C │ ◀── ödeme ───── │              │               │ Mağaza N  │
   └───────────┘                 └──────────────┘               └───────────┘
        │                               │                             │
   UYGULAMAYI                      UYGULAMAYI                    UYGULAMAYI
   KULLANIR                        KULLANIR                      KULLANMAZ
   (web portal)                    (WPF masaüstü)                (telefonla sipariş
                                                                  verir, biz elle
                                                                  sisteme gireriz)
```

### Kritik nokta

Perakendeci mağazalar sisteme **hiç girmez**. Siparişleri telefonla / WhatsApp ile /
yüz yüze alırız ve **elle sisteme gireriz**. Bu yüzden:

> 🎯 **Hızlı ve kolay veri girişi, bu ürünün en önemli özelliğidir.**
> Uygulamayı pazarlarken en büyük kozumuz budur. Her tasarım kararında
> "bu, siparişi girmeyi kolaylaştırıyor mu?" sorusu sorulmalıdır.

### Uygulamanın rolü

Bu sadece bir sipariş yazılımı değil, işletmenin **amiral gemisi**dir. Tek ekrandan
görülebilmesi gerekenler:

- Elimde ne var? (stok)
- Kimden ne aldım, ne zaman gelecek? (satın alma / üretici siparişleri)
- Kime ne sattım, ne zaman sevk edeceğim? (satış siparişleri)
- Kimden ne kadar alacağım var, vadesi ne zaman? (perakendeci cari)
- Kime ne kadar borcum var, vadesi ne zaman? (üretici cari)
- Ne kazanıyorum? (kâr marjı, raporlar)

---

## 2. KULLANICI ROLLERİ

V1'de **tam olarak iki rol** vardır. Fazlasını erkenden yapmayacağız.

### 2.1 `Owner` — Mağaza Sahibi

Bizim tarafımız. Sistemin tamamına erişir:
ürünler, stok, perakendeci müşteriler, satış siparişleri, sevkiyat,
üreticiler, satın alma siparişleri, mal kabul, tahsilat, borç ödeme,
cari hesaplar, raporlar, ayarlar, kullanıcı yönetimi.

### 2.2 `Manufacturer` — Üretici

Dış firma. **Yalnızca kendisine ait satın alma siparişlerini** görür.

Görebildikleri:
- kendisine verdiğimiz siparişler (stok kodu, ürün adı, beden dağılımı, adet)
- mutabık kalınan **alış** fiyatı (zaten kendi satış fiyatı, biliyor)
- termin/teslim tarihleri
- kendi sipariş durumu (Üretimde / Hazır / Sevk Edildi)
- *(opsiyonel, ileride)* kendisine olan borcumuzun bakiyesi

### 2.3 🔴 EN KRİTİK GİZLİLİK KURALI

> **Üretici; satış fiyatlarımızı, perakendeci müşteri listemizi, satış
> siparişlerimizi, cirolarımızı ve kâr marjımızı ASLA göremez.**

**Neden bu hayati:** Üretici aynı zamanda potansiyel rakiptir. Kâr marjımızı görürse
pazarlıkta bize karşı kullanır; müşteri listemizi görürse **bizi atlayıp doğrudan
onlara satabilir** — iş modelimizi yok eder.

**Nasıl korunur (üç katman):**

1. **Yapısal izolasyon:** Perakendeci finansı ve üretici finansı **ayrı tablolarda**
   tutulur. Ortak bir "cari hesap" tablosu yoktur. Böylece tek bir filtreleme hatası
   tüm müşteri verisini sızdıramaz.
2. **DTO izolasyonu:** `ManufacturerPurchaseOrderResponse` DTO'sunda `sale_price` diye
   bir alan **hiç bulunmaz**. UI'da gizlenmez — veri hiç gönderilmez.
3. **Backend RBAC:** `Manufacturer` rolü `Owner`-only endpoint'e istek atarsa **403**
   alır. Ayrıca her sorgu kendi `supplier_id`'si ile filtrelenir (bir üretici başka
   üreticinin siparişini de göremez).

### 2.4 İleriye dönük not

`Staff` (çalışan) rolü şu an **yapılmıyor**. Ancak `users.role` alanı öyle
tasarlanıyor ki ileride eklemek tek satırlık bir değişiklik olsun. Yazılım başka bir
toptancıya satıldığında orada birden fazla çalışan olacaktır.

---

## 3. TEKNOLOJİ YIĞINI

| Katman | Teknoloji | Not |
|---|---|---|
| Dil / Runtime | C# / .NET 10 | |
| Masaüstü UI (Owner) | WPF + MVVM | Klavye dostu, hızlı veri girişi |
| Web UI (Üretici) | **Blazor Server** | Faz 18'de, VPS ile birlikte devreye girer |
| Backend | ASP.NET Core Web API | Güvenlik ve iş mantığı sınırı |
| ORM | Entity Framework Core | Repository Pattern **yok** |
| Veritabanı | PostgreSQL | |
| API stili | REST + JSON | |
| Kimlik doğrulama | **JWT (token tabanlı)** | Hem masaüstü hem web'de aynı token çalışır |
| Sürüm kontrol | Git | |
| Hedef OS | Windows 11 | |

### 3.1 Mimari — İki UI, Tek Beyin

```
     [WPF Masaüstü]              [Blazor Web]
      (Owner konsolu)          (Üretici portalı)
            │                         │
            └───────── JWT ───────────┘
                       │
                       ▼
              ┌─────────────────┐
              │   Web API       │  ← RBAC, validation, hata yönetimi
              ├─────────────────┤
              │   Application   │  ← TÜM iş kuralları burada
              ├─────────────────┤
              │   Domain        │  ← varlıklar, iş kavramları
              ├─────────────────┤
              │  Infrastructure │  ← EF Core
              └────────┬────────┘
                       ▼
                 PostgreSQL
```

### 3.2 🔒 Değiştirilemez Mimari Kurallar

Bunlar, ileride Blazor portalını ucuza eklemenin **ön koşuludur**. İhlal edilirse
web arayüzü eklemek "yeni ekran yazmak" olmaktan çıkıp "her şeyi ikinci kez yazmak"
haline gelir.

1. **İş kuralı asla UI katmanında yaşamaz.** Fiyat hesabı, stok kontrolü, bakiye
   hesabı, vade hesabı → hepsi `Application` katmanında. ViewModel sadece
   görüntüler ve API çağırır.
2. **WPF'in hiçbir ayrıcalığı yoktur.** WPF ne görüyorsa API'den görür. Veritabanına,
   dosya sistemine, hiçbir kaynağa doğrudan erişmez.
3. **Kimlik doğrulama JWT'dir.** Windows/session tabanlı auth kullanılmaz.
4. **Yapılandırma koda gömülmez.** API adresi, DB bağlantısı, stok kodu öneki —
   hepsi config veya veritabanından okunur. VPS'e geçiş bu sayede kod değişikliği
   gerektirmez.
5. **HTTPS baştan kurulur**, localhost'ta bile. Sonradan eklenmez.

### 3.3 Kaçınılacaklar

Repository Pattern (EF Core zaten yeterli), MediatR/CQRS, mikroservis, Kubernetes,
erken soyutlama, moda desen kullanımı.

> **İlke: Mümkün olan yerde basit, gereken yerde sağlam.**
> Sağlamlık öncelikli alanlar: **para, stok, kimlik doğrulama, denetim, yedekleme.**

---

## 4. DAĞITIM (DEPLOYMENT) — İKİ AŞAMALI

### Aşama 1 — Tek PC (şimdi)

```
┌──────────────────────────────┐
│  Mağaza Sahibinin PC'si      │
│                              │
│  WPF ──▶ API ──▶ PostgreSQL  │
│                              │
│  Üretici portalı: KAPALI     │
└──────────────────────────────┘
```

Bütçe hazır olana kadar her şey tek makinede. Üretici henüz bağlanamaz.

### Aşama 2 — VPS (bütçe olunca)

```
      Mağaza PC              ┌─────────── VPS ───────────┐
      [WPF] ─────HTTPS──────▶│  API + Blazor Portal      │
                             │         │                 │
   Üretici (uzaktan)         │         ▼                 │
   [Tarayıcı] ───HTTPS──────▶│    PostgreSQL             │
                             │         │                 │
                             │    Otomatik yedek ──▶ dış │
                             └───────────────────────────┘
```

Geçişin kod değişikliği gerektirmemesi için §3.2'deki 4 ve 5 numaralı kurallar
zorunludur. Geçiş = "config'te adres değiştir + veritabanını taşı".

### Yedekleme kuralı

> Yedek **asla** veritabanıyla aynı fiziksel diskte tek başına durmaz.
> Disk ölürse hem veri hem yedek gider.

Hedef: günlük yedek + ikinci fiziksel/uzak hedef (harici disk / şifreli bulut).
Saklama: son 7 günlük, son 4 haftalık, son 6 aylık.

> **Bir yedekleme stratejisi, geri yükleme test edilene kadar tamamlanmış sayılmaz.**

---

## 5. STOK KODU (GND)

Depoya giren her ürün modelinin bir **stok kodu** vardır.

### Format

```
GND000142
│   └────┴─ 6 hane, başı sıfırla doldurulmuş
└─ önek (3 harf)
```

**Neden sabit 6 hane (değişken 5-6 değil):**
- Metin sıralaması doğru çalışır (`GND9999` vs `GND10000` sorunu olmaz)
- `GND1234` mü `GND01234` mü belirsizliği doğmaz
- 1.000.000 ürün kapasitesi

### Hangi seviyede?

Stok kodu bir **modeli** tanımlar → `products` tablosunda tutulur.

```
products.stock_code = 'GND000142'   →  "Klasik Erkek Bot / siyah deri"
   ├── product_variants:  GND000142 / 40 / Siyah
   ├── product_variants:  GND000142 / 41 / Siyah
   └── product_variants:  GND000142 / 41 / Kahve
```

### White-label uyumu (kritik)

`GND` öneki **veritabanına gömülmez.** Yazılım başka bir toptancıya satıldığında
onun öneki farklı olacaktır (`ABC`, `MRT`, …).

| Nerede | Ne |
|---|---|
| Veritabanı `CHECK` | Sadece genel format: `^[A-Z]{2,5}[0-9]{4,8}$` + boş olmasın + benzersiz |
| `store_profile` | `stock_code_prefix` (varsayılan `'GND'`), `stock_code_digits` (varsayılan `6`) |
| Application katmanı | Sıradaki kodu üretir, öneki doğrular |

### UI kolaylığı

- Kullanıcı `142` yazar → sistem `GND000142`'ye çevirir. 9 karakter yazma zorunluluğu yok.
- Yeni ürün eklerken sistem sıradaki boş kodu **otomatik önerir**, istenirse değiştirilir.
- Stok kodu her listede ilk kolon, her arama kutusunda birinci hedef.

### SKU saklanmaz

Varyant kodu (`GND000142-41-SİYAH`) `stock_code + size + color`'dan **türetilir** ve
veritabanında kolon olarak tutulmaz — tek doğruluk kaynağı ilkesi. Application katmanı
gösterim anında üretir. Varyantın gerçek kimliği `UNIQUE (product_id, size, color)`
kısıtıdır. Fiziksel etiket/okuyucu işi opsiyonel `barcode` alanıyla yürür.

### Renk normalizasyonu

`color`, UNIQUE kısıtının parçası olduğu için `'Siyah'`/`'siyah'` ikiliği **stoğu ikiye
böler**. Bu yüzden renk uygulama tarafında `ToUpperInvariant()` ile normalize edilerek
saklanır ve UI mevcut renkleri dropdown olarak sunar. Veritabanında `upper()`
kullanılmaz — Türkçe 'i' dönüşümü veritabanının dil ayarına bağlıdır.

### Toplu fiyat güncelleme (UI gereksinimi, Faz 7)

Fiyatlar varyant seviyesinde tutulduğu için bir modelin fiyatını değiştirmek çok satır
güncellemeyi gerektirir. Ürün ekranında **varyantları tıklayarak seçip toplu fiyat
güncelleme** imkânı olmalı — "hepsi" değil, "seçtiklerim".

---

## 6. SATIŞ TARAFI (Perakendeciye Satış)

### 6.1 Sipariş yaşam döngüsü

`ApprovalStatus` (onay adımı) **yoktur** — siparişi zaten kendi personelimiz giriyor,
kendi girdiğimizi kendimizin onaylaması gereksiz tıktır.

```
 SİPARİŞ ALINDI (Received)   →  stok REZERVE edilir (fiziksel stok düşmez)
        ↓
 HAZIRLANIYOR (Preparing)    →  depoda toplanıyor
        ↓
 SEVK EDİLDİ (Shipped)       →  ★ fiziksel stok DÜŞER
                                ★ perakendeciye BORÇ yazılır (cari hesap)
                                ★ ödeme planı + taksitler oluşur
        ↓
 TESLİM EDİLDİ (Delivered)

 (her aşamada) İPTAL (Cancelled) → rezervasyon serbest kalır
```

**Neden stok düşümü sevkiyatta:** Mal depodan fiziksel olarak çıkmadan ne stok
düşmeli ne de müşteriye borç yazılmalı. Gerçek hayata birebir uyar.

**Neden rezervasyon korunuyor:** Telefonla sipariş alındı, mal henüz depoda ama
başkasına satılmamalı. `quantity_available = on_hand - reserved`.

### 6.2 ★ Beden Dağılımı Izgarası — Kozumuz

Toptan sipariş şöyle gelir: *"GND142'den 40 numaradan 3, 41'den 5, 42'den 4 çift"*.

Her bedeni ayrı satır olarak eklemek işkencedir. Bunun yerine:

```
┌─────────────────────────────────────────────────────────┐
│  Stok Kodu: [ 142        ]  → GND000142 Klasik Erkek Bot│
│  Renk:      [ Siyah  ▾   ]                              │
│                                                          │
│    38   39   40   41   42   43   44   45                │
│  [  ] [  ] [ 3] [ 5] [ 4] [  ] [  ] [  ]     Toplam: 12 │
│                                                          │
│                                    [ Satıra Ekle ]       │
└─────────────────────────────────────────────────────────┘
```

Tek ürün seçilir, bedenler yan yana kutucuk olarak çıkar, adetler yazılır, Enter.

### 6.3 Veri girişi ilkeleri

1. **Telefonla konuşurken girilebilmeli.** Tek ekran, popup yok, fare gerekmiyor.
2. **Klavyeden el kalkmaz.** Kod yaz → Enter → adetler → Enter → sonraki satır.
3. **Stok kodu her yerde birinci sınıf.** Büyük arama kutusu, her listede ilk kolon.
4. **Geri alınabilirlik.** Yanlış giriş kaçınılmaz; "Düzelt" ve "İptal" iki tık uzakta.
   (Arkada ters kayıt çalışır, kullanıcı bunu bilmez.)
5. **Büyük yazı, yüksek kontrast, sığ menü.** ERP tarzı iç içe menü yok.
6. **Yazdırma.** Sipariş fişi / sevk listesi çıktısı — depoda kağıtla çalışılır.
7. **İnsan dilinde hata mesajı.** `NullReferenceException` değil,
   *"Sunucuya ulaşılamıyor. Ana bilgisayarın açık olduğunu kontrol edin."*

### 6.4 Sipariş kalemi snapshot'ları

`order_items`; ürün adı, stok kodu, SKU, beden, renk, birim satış fiyatı ve birim
alış fiyatını **o anki haliyle kopyalar**. Ürün fiyatı sonradan değişse bile geçmiş
siparişler değişmez. `unit_purchase_price` sadece geçmişe dönük kâr analizi içindir
ve üreticiye asla gönderilmez.

---

## 7. SATIN ALMA TARAFI (Üreticiden Alış) — YENİ

### 7.1 Akış

```
 BİZ: Üreticiye sipariş oluştur
      (stok kodu, beden dağılımı, adet, alış fiyatı, termin tarihi, vade tarihi)
        ↓
 ÜRETİCİ: Portala girer → siparişi görür
          → "Üretimde" / "Hazır" / "Sevk Edildi" olarak işaretler
        ↓
 BİZ: Mal gelir → MAL KABUL
      (gelen adet girilir — KISMİ OLABİLİR)
        ↓
 Stok ARTAR  (inventory_movements: Purchase)
        ↓
 Üreticiye BORCUMUZ oluşur (supplier_transactions)
```

### 7.2 Kısmi teslimat zorunlu

Ayakkabı üretiminde *"500 çift sipariş verildi, 320 çift geldi, kalanı 2 hafta sonra"*
son derece normaldir. Bu yüzden sipariş kalemi bazında:

- `ordered_quantity` — sipariş edilen
- `received_quantity` — bugüne kadar teslim alınan (birikimli)

ayrı tutulur. Sipariş durumu bu ikisinden türetilir (Bekliyor / Kısmi / Tamamlandı).

### 7.3 Varsayılan üretici — dolu gelir, kilitli gelmez

`products.supplier_id` bir **varsayılandır, kısıt değildir.** Satın alma siparişi
ekranında üretici alanı bu değerle otomatik dolar, ama kullanıcı serbestçe
değiştirebilir. Aynı modeli bu sefer başka bir imalathaneye yaptırmak meşru bir
senaryodur ve yazılım bunu engellememelidir.

Bir alışın **gerçek** üreticisi her zaman `purchase_orders.supplier_id`'dir.

### 7.4 Üreticinin kendi ürün kodu

Biz `GND000142` deriz, üretici `MDL-7734-B` der. Üretici ne üreteceğini anlasın diye
sipariş kaleminde **her iki kod da** bulunur (`supplier_product_code`).

---

## 8. FİNANS — ÇİFT TARAFLI

```
        ÜRETİCİ                      BİZ                    PERAKENDECİ
           │                                                     │
           │ ◀─── BORCUMUZ ────┐              ┌─── ALACAĞIMIZ ──▶│
           │                   │              │                  │
   supplier_transactions ──────┤              ├────── account_transactions
   supplier_payments     ──────┘              └────── payments
                                                      installments
                                                      payment_allocations
```

### 8.1 Neden ayrı tablolar (birleşik "cari hesap" değil)

**Güvenlik gerekçesi:** Üretici bu sisteme **giriş yapıyor**. Müşteri carisi ile
üretici carisi aynı tabloda yaşarsa, tek bir filtreleme hatası üreticinin tüm
perakendeci müşteri listesini ve cirolarını görmesine yol açar. **Ayrı tablo =
yapısal izolasyon.**

**İş gerekçesi:** İki akış gerçek hayatta farklıdır — "alacağımı tahsil etme"
(kovalama, vade takibi, taksit planı) ile "borcumu ödeme" (vadesi gelen faturayı
ödeme) farklı ekranlar, farklı reflekslerdir.

### 8.2 Perakendeci tarafı (zengin)

Toptan ayakkabı ticaretinde peşin ödeme istisnadır. Desteklenen modeller:

1. Peşin
2. Tamamı taksitli
3. Peşinat + kalan taksitli

**Peşinat ayrı bir alan değil, özel bir taksittir.** Ödeme planının kendisi tek
doğruluk kaynağıdır:

```
Sipariş toplamı: 100.000 TL
#1  Peşinat   20.000 TL   Vade: 18 Ağu
#2  Normal    20.000 TL   Vade: 01 Eyl
#3  Normal    20.000 TL   Vade: 01 Eki
#4  Normal    20.000 TL   Vade: 01 Kas
#5  Normal    20.000 TL   Vade: 01 Ara
```

Eşit taksit **zorunlu değildir** — özel tutarlar ve özel vadeler desteklenir.
UI otomatik plan üretebilir, backend esnek kalır.

**Kural:** `SUM(installments.amount) = order.total_amount` — Application katmanında,
transaction içinde zorlanır (DB'de değil; çok satırlı kural).

#### Ödeme ≠ Taksit

| Taksit (`installments`) | Ödeme (`payments`) |
|---|---|
| Ödenmesi **gereken** para | Fiilen **alınan** para |

Bir ödeme birden fazla taksidi kapatabilir; bir taksit birden fazla ödemeyle kısmen
kapanabilir → `payment_allocations` (köprü tablo).

```
Taksit #1 kalan: 10.000
Taksit #2 kalan: 20.000
Müşteri öder:    25.000
                  ├── 10.000 → Taksit #1  (Ödendi)
                  └── 15.000 → Taksit #2  (Kısmi, kalan 5.000)
```

#### Tahsilat UX'i basit kalmalı

Backend karmaşık olabilir, kullanıcı arayüzü olmamalı. Varsayılan akış:

```
Müşteri: ABC Ayakkabı
Tutar:   [ 25.000 ]   Yöntem: [ Havale ▾ ]   Tarih: [ 18.08.2026 ]
                                                       [ KAYDET ]
```

Sistem parayı **en eski vadesi geçmiş taksitten başlayarak otomatik dağıtır.**
"Dağıtımı Değiştir" ileri seviye ve opsiyonel bir aksiyondur — normal kullanıcı
hiçbir zaman elle dağıtım yapmak zorunda kalmaz.

#### Taksit durumu **saklanmaz, türetilir**

| Durum | Koşul |
|---|---|
| Ödendi | dağıtılan ≥ tutar |
| Kısmi | 0 < dağıtılan < tutar |
| Bekliyor | dağıtılan = 0 **ve** vade ≥ bugün |
| Gecikmiş | kalan > 0 **ve** vade < bugün |

Böylece bayat durum verisi oluşmaz.

### 8.3 Üretici tarafı (bilinçli olarak basit)

Taksit planı / taksit / dağıtım makinesi **kurulmuyor**. Bunun yerine:

- `purchase_orders.payment_due_date` — vade tarihi
- `supplier_payments` — yaptığımız ödemeler
- `supplier_transactions` — cari defter, bakiye `SUM()` ile

İleride üreticiye vadeli taksitli ödeme gerekirse eklenir. Şimdi 3 tablo kurup
kullanmamak israftır.

### 8.4 Ters kayıt (reversal) politikası

> **Finansal hata silinerek düzeltilmez, ters kayıtla düzeltilir.**

Yanlışlıkla 50.000 girildi, doğrusu 5.000 olacaktı:

```
Ödeme #184     50.000  ─ İPTAL EDİLDİ (ters kayıt)
Ters kayıt    -50.000
Ödeme #185      5.000  ─ Doğru kayıt
```

Kayıt veritabanında kalır; kim/ne zaman/neden bilgisi tutulur. Kullanıcı hatasını
görebilir ve ne olduğunu anlayabilir.

### 8.5 Tek doğruluk kaynağı — türetilen değerler

Bunlar **bağımsız alan olarak saklanmaz**:

| Değer | Kaynak |
|---|---|
| `quantity_available` | `quantity_on_hand - quantity_reserved` (DB generated column) |
| Taksidin ödenen tutarı | `SUM(aktif payment_allocations)` |
| Perakendeci bakiyesi | `SUM(account_transactions.amount)` |
| Üretici bakiyesi | `SUM(supplier_transactions.amount)` |
| Siparişin ödeme durumu | ödenen vs toplam karşılaştırması |
| Satın alma sipariş durumu | `ordered_quantity` vs `received_quantity` |

---

## 9. DEĞİŞMEZ KURALLAR (INVARIANTS)

Bunlar asla ihlal edilmez.

1. 🔴 **Üretici API cevaplarında satış fiyatı, müşteri verisi veya kâr marjı bulunmaz.**
2. 🔴 Bir üretici, başka bir üreticinin siparişini göremez.
3. Stok asla negatif olamaz.
4. Sipariş toplamları yalnızca sunucuda hesaplanır.
5. İstemciden gelen fiyata asla güvenilmez.
6. `order_items` fiyat/ad/kod/beden/renk **snapshot'ı** saklar; geçmiş siparişler
   sonradan değişmez.
7. Tüm finansal hareketler izlenebilir kalır (ledger, üzerine yazılan alan değil).
8. Pasif ürün/varyant yeni siparişe konu edilemez.
9. Geçersiz sipariş durum geçişleri reddedilir.
10. Parola asla düz metin saklanmaz; `password_hash` asla API'de veya logda görünmez.
11. Her kurulum kendi bağımsız veritabanına sahiptir.
12. **Sipariş alındığında stok rezerve edilir** (düşmez).
13. **Fiziksel stok yalnızca SEVKİYAT anında düşer.**
14. Finansal düzeltme ters kayıtla yapılır, sessiz DELETE ile değil.
15. Stok kodu öneki koda gömülmez, yapılandırmadan gelir.

### Hard-delete yasağı

`users`, `customers`, `suppliers`, `products`, `product_variants`, `orders`,
`order_items`, `purchase_orders`, `purchase_order_items`, `payments`,
`supplier_payments`, `account_transactions`, `supplier_transactions`, `audit_logs`
→ **asla fiziksel olarak silinmez.**

Yerine: `is_active` / pasifleştir / iptal et / ters kayıt / düzeltme kaydı.

---

## 10. V1 KAPSAMI

### İçeride

**Kimlik:** giriş/çıkış, JWT, iki rol (Owner / Manufacturer), parola hash'leme, RBAC.

**Ürünler:** stok kodu (GND), oluştur/güncelle/pasifleştir, arama, varyantlar
(beden/renk/SKU/barkod), alış ve satış fiyatı.

**Stok:** anlık miktar, rezerve, müsait hesabı, manuel düzeltme, hareket geçmişi,
negatif stok yasağı, kritik stok uyarısı.

**Perakendeci müşteriler:** bireysel/kurumsal, zorunlu telefon, arama, işlem geçmişi.

**Satış siparişleri:** hızlı elle giriş, beden ızgarası, rezervasyon, snapshot,
durum akışı, sevkiyat, iptal, sipariş geçmişi, yazdırma.

**Üreticiler:** üretici kayıtları, üretici kullanıcı hesapları.

**Satın alma siparişleri:** sipariş oluşturma, üretici portalı (Faz 18), durum takibi,
kısmi mal kabul, stok girişi.

**Perakendeci finansı:** peşin/taksitli/peşinatlı planlar, özel vade ve tutarlar,
kısmi ödeme, otomatik dağıtım, gecikme hesabı, ters kayıt.

**Üretici finansı:** borç takibi, ödeme kaydı, cari defter, vade takibi.

**Cari hesaplar:** iki taraflı ledger, bakiye, hareket geçmişi.

**Dashboard:** bugünün siparişleri, sevk bekleyenler, toplam alacak, gecikmiş alacak,
bugün/bu hafta/bu ay vadesi gelenler, üreticiye borç ve vadeler, kritik stok,
bekleyen üretici siparişleri.

**Tahsilat ekranı:** Gecikmiş / Bugün / Bu Hafta / Bu Ay filtreleri, tek tıkla ödeme girişi.

**White-label:** mağaza adı, logo, tema, stok kodu öneki, bağımsız veritabanı.

**Altyapı:** loglama, denetim (audit), hata yönetimi, doğrulama, yedekleme,
geri yükleme dokümantasyonu, EF migration, kurulum.

### Dışarıda (V1'de yapılmayacak)

Mobil uygulama, perakendeci self-servis portalı, e-ticaret/pazaryeri entegrasyonu,
e-fatura / resmî muhasebe, banka / online ödeme entegrasyonu, çoklu şube/depo ve
transferler, gelişmiş iade/geri ödeme akışı, indirim/kampanya motoru, yapay zekâ,
SMS/WhatsApp/e-posta pazarlama, çevrimdışı senkronizasyon, üretici için taksitli
ödeme planı.

---

## 11. YOL HARİTASI

> ⚠️ Bu liste durum takibi için değil, kapsam referansı içindir — **güncel "şu an
> neredeyiz" bilgisi için her zaman `CLAUDE.md` §9'a bakın**, burası değil. Bu dosya
> son güncellendiğinde (Faz 3) burada da bir "ŞU AN" işareti vardı; canlı tutmak yerine
> tek kaynağı (`CLAUDE.md`) yetkili kılmak daha güvenli — iki yerde aynı bilgiyi
> güncellemeyi unutmak tam olarak bu şekilde oldu.

```
FAZ 3   Veritabanı Tasarımı              ✅ tamamlandı
FAZ 4   Backend Temeli + EF Core + Migration ✅ tamamlandı
FAZ 5   Kimlik Doğrulama (JWT) + RBAC (2 rol)
FAZ 6   WPF Temeli / MVVM / Navigasyon / Tema
FAZ 7   Ürünler + Stok Kodu + Varyantlar
FAZ 8   Stok + Stok Hareketleri
FAZ 9   Perakendeci Müşteriler
FAZ 10  ★ Satış Siparişi (beden ızgarası — koz ekran)
FAZ 11  Sevkiyat + Stok Düşümü
FAZ 12  Üreticiler + Satın Alma Siparişleri
FAZ 13  Mal Kabul + Stok Girişi
FAZ 14  Perakendeci Finansı (plan / taksit / tahsilat / cari)
FAZ 15  Üretici Finansı (borç / ödeme / cari)
FAZ 16  Dashboard + Raporlar
FAZ 17  Loglama / Audit / Hata Yönetimi
FAZ 18  ★ VPS'e Taşıma + Üretici Portalı (Blazor)
FAZ 19  Test / Güvenlik / Performans
FAZ 20  Yedekleme / Kurulum / Dokümantasyon / Devir
```

### Sürüm aşamaları

| Sürüm | İçerik |
|---|---|
| **V0.1** Teknik prototip | DB + API + WPF + giriş + temel ürünler |
| **V0.5** İşlevsel MVP | Kimlik + ürünler + stok + müşteriler + satış siparişi + sevkiyat |
| **V0.8** İş MVP'si | Satın alma + mal kabul + iki taraflı finans + dashboard + audit |
| **V1.0** Üretim | Üretici portalı + VPS + test + kurulum + yedekleme + dokümantasyon |

### Her modül için geliştirme döngüsü

Gereksinim → Domain modeli → Veritabanı → API sözleşmesi → İş mantığı →
Endpoint → Backend testi → WPF servisi → ViewModel → View → Entegrasyon testi →
Kod gözden geçirme → Git commit.

> UI, altındaki veri modeli ve kullanım senaryosu anlaşılmadan yazılmaz.

---

## 12. KRİTİK İŞ İŞLEMLERİ (atomik olmalı)

Application katmanında, tek transaction içinde. Herhangi biri başarısız olursa
**tamamı geri alınır (ROLLBACK)**.

**Satış siparişi girişi:** doğrula (ürün/varyant aktif mi, müsait stok yeterli mi) →
güncel satış fiyatını sunucudan oku → `orders` (Received) + `order_items` (snapshot'lı)
oluştur → `quantity_reserved += adet` → `inventory_movements` (rezervasyon) →
`order_history` → COMMIT.

**Sevkiyat:** durum doğrula → `Shipped` yap → `quantity_on_hand -= adet` →
`quantity_reserved -= adet` → `inventory_movements` (Sale) → `payment_plans` +
`installments` oluştur → `account_transactions` `+sipariş tutarı` → `order_history` →
`audit_logs` → COMMIT.

> Asla oluşmaması gereken durum: *"Sipariş sevk edildi ama cari hesaba borç yazılmadı"*
> veya *"Stok düştü ama sipariş güncellenmedi"*.

**Sipariş iptali:** durum doğrula → `Cancelled` → `quantity_reserved -= adet`
(fiziksel stok değişmez) → `inventory_movements` (ReservationReleased) →
sevk edilmişse ters cari kaydı → `order_history` → `audit_logs` → COMMIT.

**Mal kabul:** satın alma siparişi doğrula → `received_quantity += gelen adet`
(sipariş edilenden fazla olamaz) → `quantity_on_hand += adet` →
`inventory_movements` (Purchase) → `supplier_transactions` `+borç` →
`purchase_order_history` → `audit_logs` → COMMIT.

**Tahsilat (perakendeciden):** `payments` oluştur → en eski gecikmiş/açık taksitten
başlayarak otomatik dağıt → `payment_allocations` → `account_transactions` `-tutar` →
`audit_logs` → COMMIT.

**Ödeme (üreticiye):** `supplier_payments` oluştur → `supplier_transactions` `-tutar` →
`audit_logs` → COMMIT.

**Ödeme iptali (ters kayıt):** kayıt aktif mi doğrula → `Reversed` işaretle +
neden/kim/ne zaman → dağıtımlar ödenen tutara sayılmaz olur → ters yönlü cari kaydı →
`audit_logs` → COMMIT. **Orijinal kayıt veritabanında kalır.**

### Eşzamanlılık

Son 1 adet için iki sipariş aynı anda girilirse stok bozulmamalıdır. Çözüm
**istemci tarafı kontrolle değil**, uygun DB transaction/kilitleme ile yapılır
(Faz 4+'ta somutlaşacak: `SELECT ... FOR UPDATE` veya PostgreSQL'in `xmin` sistem
kolonu ile optimistic concurrency).

---

## 13. GÜVENLİK KURALLARI

| Kural | |
|---|---|
| Parola | Asla düz metin |
| `password_hash` | Asla API'de veya logda |
| Satış fiyatı / müşteri verisi | Asla üreticiye |
| Yetkilendirme | Backend'de zorlanır, UI'da gizlemek yetmez |
| Fiyat & stok | Backend otoritedir |
| Finansal değişiklik | Denetlenebilir olmalı |
| DB parolası | Asla Git'e commit edilmez |
| Girdi | Daima doğrulanır |
| İstemci | PostgreSQL'e doğrudan erişemez |
| Log/audit | Hassas veri sızdırmaz |

### Denetlenecek (audit) işlemler

Ürün oluşturma, fiyat değişikliği, ürün pasifleştirme, manuel stok değişikliği,
sipariş oluşturma/sevk/iptal, satın alma siparişi oluşturma, mal kabul, ödeme
oluşturma/iptali, ödeme planı değişikliği, müşteri/üretici değişikliği, kullanıcı
oluşturma/pasifleştirme.

**Asla loglanmaz:** parolalar, hash'ler, token'lar, DB kimlik bilgileri.

---

## 14. TEST GEREKSİNİMLERİ

| Tür | Örnek |
|---|---|
| Birim | İş kuralları, hesaplamalar |
| Entegrasyon | API + veritabanı |
| Yetkilendirme | Üretici → Owner endpoint'i **403 almalı** |
| Gizlilik | Üretici cevabında `sale_price` **hiç bulunmamalı** |
| Eşzamanlılık | Müsait stok 1, iki eşzamanlı sipariş → stok bozulmamalı |
| Finansal | 100.000 sipariş + 20.000 ödeme = 80.000 bakiye; kısmi dağıtım doğru olmalı; ters kayıt doğru durumu geri getirmeli |

---

## 15. "BİTTİ" TANIMI

Bir özellik, ekranı çalıştığı için bitmiş sayılmaz. Tamamlanma kriterleri:

Domain modeli var · DB eşlemesi/migration var · Doğrulama var · İş kuralları var ·
API var · Yetkilendirme var · Backend testi var · WPF servisi var · ViewModel var ·
UI var · Hata durumu var · Yükleniyor durumu var · Boş durum var · Arama çalışıyor ·
Benzersizlik kısıtları var · Yetkiler doğru · Loglama/audit uygun · Kod gözden
geçirildi · Git'e commit edildi.
