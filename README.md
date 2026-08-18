# ShoeRetail

Ayakkabı perakendecileri için white-label masaüstü mağaza yönetim sistemi.

**Ticari model:** Tek kod tabanı → her mağaza için bağımsız kurulum + bağımsız veritabanı
(klasik multi-tenant SaaS değil).

## Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Desktop UI | WPF (MVVM) |
| Backend | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Veritabanı | PostgreSQL |
| Runtime | .NET 10 |
| API Stili | REST + JSON |

## Mimari

```
WPF Desktop → HTTPS/JSON → ASP.NET Core API → EF Core → PostgreSQL
```

WPF, PostgreSQL'e asla doğrudan bağlanmaz. Backend, güvenlik ve iş kuralı sınırıdır.

## Proje Durumu

Şu an **veritabanı fiziksel şema tasarımı** aşamasındayız.
Detaylar: `docs/database/02-physical-blueprint.md`

Tüm ürün vizyonu, iş kuralları ve mimari kararlar için:
`docs/00-handoff/ShoeRetail_AI_Agent_Handoff.txt`

## Klasör Yapısı

Bkz. `docs/architecture/folder-structure.md`