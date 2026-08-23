# ============================================================
#  run-tests.ps1 — Tum test paketini sifirdan calistirir.
#
#  Kullanim:  powershell -ExecutionPolicy Bypass -File run-tests.ps1
#
#  Sira: 00-reset -> schema.sql -> 01-seed -> 02-constraint-tests
#
#  NOT: psql'in stderr'i cmd icinde birlestiriliyor. Windows PowerShell 5.1'de
#  native komutun stderr'ini dogrudan 2>&1 ile birlestirmek her satiri
#  ErrorRecord'a sarar ve NOTICE bile hata gibi gorunur.
# ============================================================

$ErrorActionPreference = 'Continue'

$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
$db   = 'shoeretail_test'
$user = 'shoeretail'
$port = '5433'

# tests dizininden repo kokune: docs\database\tests -> ..\..\..
$kok    = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$schema = Join-Path $kok 'docs\database\schema.sql'

function Calistir {
    param([string]$Ad, [string]$Dosya, [string]$Durdur = '1')

    Write-Host ""
    Write-Host "-- $Ad --" -ForegroundColor Cyan

    $cmd = '"{0}" -U {1} -h localhost -p {2} -d {3} -w -v ON_ERROR_STOP={4} -f "{5}" 2>&1' `
           -f $psql, $user, $port, $db, $Durdur, $Dosya
    $cikti = cmd /c $cmd

    $hatalar = $cikti | Select-String -Pattern 'ERROR:|FATAL:'
    if ($hatalar) {
        Write-Host "  HATA:" -ForegroundColor Red
        $hatalar | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        if ($Durdur -eq '1') { exit 1 }
    } else {
        Write-Host "  tamam" -ForegroundColor Green
    }
}

function Sorgu {
    param([string]$Sql, [string]$Bayrak = '-c')
    $cmd = '"{0}" -U {1} -h localhost -p {2} -d {3} -w {4} "{5}" 2>&1' `
           -f $psql, $user, $port, $db, $Bayrak, $Sql
    cmd /c $cmd
}

Calistir '1/5  Semayi sifirla'    (Join-Path $PSScriptRoot '00-reset.sql')
Calistir '2/5  schema.sql yukle'  $schema
Calistir '3/5  Seed verisi'       (Join-Path $PSScriptRoot '01-seed.sql')
Calistir '4/5  Kisit testleri'    (Join-Path $PSScriptRoot '02-constraint-tests.sql') '0'
Calistir '5/5  Altin testler'     (Join-Path $PSScriptRoot '03-golden-tests.sql') '0'

Write-Host ""
Write-Host "============ SONUC ============" -ForegroundColor Cyan
Sorgu "SELECT 'kisit' AS tur, sonuc, count(*) AS adet FROM _test_sonuc GROUP BY sonuc UNION ALL SELECT 'altin', sonuc, count(*) FROM _altin_sonuc GROUP BY sonuc ORDER BY 1, 2"

$kalanKisit = (Sorgu "SELECT count(*) FROM _test_sonuc  WHERE sonuc <> 'GECTI'" '-tAc') -join ''
$kalanAltin = (Sorgu "SELECT count(*) FROM _altin_sonuc WHERE sonuc <> 'GECTI'" '-tAc') -join ''
$kalan = [int]$kalanKisit + [int]$kalanAltin

if ($kalan -eq 0) {
    Write-Host ""
    Write-Host "TUM TESTLER GECTI" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "$kalan TEST KALDI:" -ForegroundColor Red
    if ([int]$kalanKisit -gt 0) {
        Sorgu "SELECT no, tablo, aciklama, beklenen, detay FROM _test_sonuc WHERE sonuc <> 'GECTI' ORDER BY sira" '-xc'
    }
    if ([int]$kalanAltin -gt 0) {
        Sorgu "SELECT no, aciklama, ihlal, sonuc FROM _altin_sonuc WHERE sonuc <> 'GECTI' ORDER BY sira" '-xc'
    }
    exit 1
}
