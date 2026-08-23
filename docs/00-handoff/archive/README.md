# Arşiv — Pivot Öncesi (v1) Tasarım Belgeleri

Bu klasördeki belgeler **geçersizdir**. Sadece tarihsel referans ve karar
gerekçelerini geriye dönük izleyebilmek için saklanıyor.

## Ne oldu?

Proje başlangıcında iş modeli şu varsayıma dayanıyordu:

```
ÜRETİCİ ──(uygulama YOK)──▶ BİZ ──(uygulama VAR: self-servis)──▶ PERAKENDECİ
```

Yani: perakendeci mağazalar uygulamaya kendileri girip sipariş talebi
oluşturacak, üretici ise sistemin dışında kalacaktı.

**Bu varsayım tersine döndü.** Gerçek durum:

```
ÜRETİCİ ──(uygulama VAR: portal)──▶ BİZ ──(uygulama YOK)──▶ PERAKENDECİ
                                                            (telefonla sipariş,
                                                             elle veri girişi)
```

## Bunun sonucu olarak geçersiz hale gelen v1 kararları

| v1 kararı | Yeni durum |
|---|---|
| `Customer` rolü (perakendeci self-servis girişi) | **Kaldırıldı.** Perakendeci sisteme hiç girmiyor. |
| `Seller` rolü | `Owner` olarak yeniden adlandırıldı |
| Sepet (Cart) modülü | **Kaldırıldı** — self-servis sipariş yok |
| Müşteri Kataloğu modülü | **Kaldırıldı** |
| Sipariş onay akışı (`ApprovalStatus`: Pending/Approved/Rejected) | **Kaldırıldı** — siparişi zaten kendi personelimiz giriyor |
| Stok düşümü "onay" anında | **Değişti** — stok düşümü "sevk" anında |
| "Customer asla PurchasePrice görmez" (1 numaralı gizlilik kuralı) | **Tersine döndü** — yeni kural: "Üretici asla satış fiyatı / müşteri verisi görmez" |
| Tedarikçi & satın alma yönetimi V1 dışı | **V1 çekirdeğine alındı** — üretici uygulamayı kullanıyor |
| 16 tablo | 22 tablo |
| Yalnızca LAN (internet gerekmez) | VPS'e geçiş planlandı (üretici uzaktan bağlanacak) |

## Değişmeyen kararlar

Aşağıdakiler v1'den aynen taşındı ve hâlâ geçerli:

- C# / .NET 10 / WPF (MVVM) / ASP.NET Core Web API / EF Core / PostgreSQL
- Katmanlı mimari (Domain / Application / Infrastructure / Contracts / Api / Desktop)
- WPF asla PostgreSQL'e doğrudan bağlanmaz — her şey API üzerinden
- Product ≠ ProductVariant ayrımı
- Stok rezervasyon mantığı (`quantity_available = on_hand - reserved`)
- Hard-delete yasağı — `is_active` / iptal / ters kayıt
- Ledger tabanlı cari hesap (tek `balance` alanı değil)
- Ters kayıt (reversal) ile finansal düzeltme
- Snapshot'lı sipariş kalemleri
- Tek doğruluk kaynağı (single source of truth) ilkesi
- Fiziksel şema konvansiyonları (bigint identity PK, timestamptz, varchar+CHECK enum,
  numeric(18,2), ON DELETE RESTRICT, snake_case)
- Aşırı mühendislikten kaçınma (Repository Pattern yok, MediatR/CQRS yok, mikroservis yok)
- Basit ve anlaşılır UI — teknolojiden anlamayan kullanıcı hedefi

## Güncel belgeler

- `CLAUDE.md` — bağlayıcı çalışma kuralları + güncel durum (oturum başlangıç noktası)
- `docs/00-handoff/02-project-spec-v2.md` — yeni tam iş/domain tanımı
- `docs/database/02-physical-blueprint.md` — fiziksel veritabanı tasarım günlüğü
- `docs/database/schema.sql` — çalıştırılabilir DDL
