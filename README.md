# ShoeRetail

Ayakkabı toptancısı/distribütörü için stok, sipariş (iki yönlü: üretici ve perakendeci)
ve finans (iki yönlü: alacak ve borç) yönetim sistemi.

```
ÜRETİCİLER ──▶ BİZ (toptancı) ──▶ PERAKENDE MAĞAZALAR
 uygulamayı kullanır   uygulamayı kullanır   uygulamayı KULLANMAZ
 (Blazor portal)       (WPF masaüstü)        (telefonla sipariş, biz elle gireriz)
```

**Beyaz etiket (white-label):** tek kod tabanı, her kurulum kendi bağımsız veritabanına
sahip (klasik multi-tenant SaaS değil).

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Desktop UI (toptancı konsolu) | WPF (MVVM) |
| Üretici portalı (Faz 18) | Blazor Server |
| Backend | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Veritabanı | PostgreSQL |
| Runtime | .NET 10 |
| API Stili | REST + JSON, JWT auth |

## Mimari

```
WPF Desktop → HTTPS/JSON → ASP.NET Core API → EF Core → PostgreSQL
```

WPF, PostgreSQL'e asla doğrudan bağlanmaz. Backend, güvenlik ve iş kuralı sınırıdır.

## Proje Durumu

- ✅ Faz 3 — Veritabanı fiziksel şema tasarımı (22 tablo, test edildi, onaylandı)
- ✅ Faz 4 — Backend temeli (solution, EF Core, migration'lar)
- ◀── Faz 5 — Kimlik doğrulama (JWT) + RBAC (şu an)

Güncel yol haritası ve tam durum: `CLAUDE.md`

## Belgeler

| Belge | İçerik |
|---|---|
| `CLAUDE.md` | Oturum başlangıç noktası — özet + tam güncel durum |
| `docs/00-handoff/02-project-spec-v2.md` | İş modeli / ürün spesifikasyonu (post-pivot, yetkili kaynak) |
| `docs/database/02-physical-blueprint.md` | Veritabanı tasarımı, tablo tablo gerekçeli |
| `docs/architecture/02-backend-foundation.md` | Backend temeli karar günlüğü (Faz 4) |
| `docs/architecture/folder-structure.md` | Klasör yapısı |
