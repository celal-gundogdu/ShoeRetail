# Klasör Yapısı

Bu proje **OneDrive senkronizasyonu dışında** yerel bir konumda tutulmalıdır
(örn. `C:\Dev\ShoeRetail`). Git + GitHub zaten versiyon kontrolü ve yedekleme
görevini üstlenir; OneDrive ile aynı anda senkronize etmek dosya kilidi
çakışmalarına yol açabilir.

> ✅ Faz 4'te gerçekleşti (2026-08-24): `src/` ve `tests/` artık gerçek proje dosyalarını
> içeriyor. Ayrıntı ve gerekçe: `docs/architecture/02-backend-foundation.md`.

```
ShoeRetail/                              ← Git repo kökü
├── .git/
├── .gitignore
├── ShoeRetail.sln / .slnx
├── dotnet-tools.json                    ← yerel dotnet-ef sürüm kilidi
├── README.md
│
├── docs/                                ← tasarım dokümanları (kod DEĞİL)
│   ├── 00-handoff/
│   │   └── 02-project-spec-v2.md        ← iş modeli spesifikasyonu
│   ├── architecture/
│   │   ├── folder-structure.md          ← bu dosya
│   │   └── 02-backend-foundation.md     ← Faz 4 karar günlüğü
│   └── database/
│       ├── 02-physical-blueprint.md     ← 22 tablo, karar karar
│       ├── schema.sql                   ← çalıştırılabilir DDL
│       └── tests/                       ← SQL kısıt/mutabakat test paketi
│
├── src/
│   ├── ShoeRetail.Domain/               ← POCO entity'ler (22 tablo eşlendi)
│   ├── ShoeRetail.Application/          ← boş, iş kuralları Faz 5+'te gelecek
│   ├── ShoeRetail.Infrastructure/       ← EF Core, Npgsql, DbContext, migration'lar
│   ├── ShoeRetail.Contracts/            ← boş, API DTO'ları Faz 5+'te gelecek
│   ├── ShoeRetail.Api/                  ← ASP.NET Core Web API iskeleti
│   └── ShoeRetail.Desktop/              ← WPF iskeleti (henüz boş şablon)
│
└── tests/
    ├── ShoeRetail.Domain.Tests/         ← henüz test yok (Domain'de iş kuralı yok)
    └── ShoeRetail.Api.Tests/            ← EF Core entegrasyon testleri (shoeretail_dev)
```

## Neden `docs/` önce geliyor?

Kod yazmadan önce mimari ve veritabanı kararlarının belgeye dökülmesi,
ileride "neden böyle yaptık?" sorusuna dönüp bakabilmemizi sağlar. `src/`
klasörü Faz 4'te (Backend Temeli / Solution İskeleti), `dotnet new` ile gerçek
proje dosyaları oluşturulduğunda ortaya çıktı — Faz 3 bitene kadar bilerek boş
proje klasörleri açılmadı.

> Not: bu paragraf eskiden "Faz 2" diyordu — o, yol haritası 20 faza
> genişletilmeden önce yazılmıştı. Güncel faz numaraları için `CLAUDE.md` §9
> tektir.

## Kurulum Adımları (özet)

1. Bu klasörü OneDrive dışında bir yere aç: `C:\Dev\ShoeRetail`
2. `git init`
3. GitHub'da **private** bir repo oluştur, `git remote add origin ...`, `git push`
4. PostgreSQL kur (yerel veritabanı motoru)
5. pgAdmin veya DBeaver kur (veritabanı istemcisi)
6. VS Code kur (doküman/SQL düzenleme)
7. *(Faz 4'te)* .NET 10 SDK — kuruldu, ama PATH çakışması var, bkz. `CLAUDE.md` §1
