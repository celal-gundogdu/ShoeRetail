# Klasör Yapısı

Bu proje **OneDrive senkronizasyonu dışında** yerel bir konumda tutulmalıdır
(örn. `C:\Dev\ShoeRetail`). Git + GitHub zaten versiyon kontrolü ve yedekleme
görevini üstlenir; OneDrive ile aynı anda senkronize etmek dosya kilidi
çakışmalarına yol açabilir.

```
ShoeRetail/                              ← Git repo kökü
├── .git/
├── .gitignore
├── README.md
│
├── docs/                                ← tasarım dokümanları (kod DEĞİL)
│   ├── 00-handoff/
│   │   └── ShoeRetail_AI_Agent_Handoff.txt
│   ├── architecture/
│   │   └── folder-structure.md          ← bu dosya
│   └── database/
│       └── 02-physical-blueprint.md     ← aktif çalışılan belge
│
├── src/                                 ← Faz 2'de oluşturulacak (henüz YOK)
│   ├── ShoeRetail.Domain/
│   ├── ShoeRetail.Application/
│   ├── ShoeRetail.Infrastructure/
│   ├── ShoeRetail.Contracts/
│   ├── ShoeRetail.Api/
│   └── ShoeRetail.Desktop/
│
└── tests/                               ← Faz 2+ (henüz YOK)
    ├── ShoeRetail.Domain.Tests/
    └── ShoeRetail.Api.Tests/
```

## Neden `docs/` önce geliyor?

Kod yazmadan önce mimari ve veritabanı kararlarının belgeye dökülmesi,
ileride "neden böyle yaptık?" sorusuna dönüp bakabilmemizi sağlar. `src/`
klasörü, Faz 2 (Solution Architecture / Project Skeleton) adımında,
`dotnet new` ile gerçek proje dosyaları oluşturulduğunda ortaya çıkacak —
şimdiden boş proje klasörleri açmıyoruz.

## Kurulum Adımları (özet)

1. Bu klasörü OneDrive dışında bir yere aç: `C:\Dev\ShoeRetail`
2. `git init`
3. GitHub'da **private** bir repo oluştur, `git remote add origin ...`, `git push`
4. PostgreSQL kur (yerel veritabanı motoru)
5. pgAdmin veya DBeaver kur (veritabanı istemcisi)
6. VS Code kur (doküman/SQL düzenleme)
7. *(Faz 2'de)* Visual Studio 2022 + .NET 10 SDK
