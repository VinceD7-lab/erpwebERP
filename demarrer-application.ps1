<#
Demarre erpWeb.Web en arriere-plan et enregistre son PID
pour permettre l'arret via arreter-application.ps1.
#>

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$cheminProjetWeb = Join-Path $racineDepot "src/erpWeb.Web"
$fichierPid = Join-Path $racineDepot ".erpweb-app.pid"
$fichierJournal = Join-Path $racineDepot "erpweb-app.log"
$fichierJournalErreurs = Join-Path $racineDepot "erpweb-app-erreurs.log"
$dossierBinaires = Join-Path $racineDepot "src/erpWeb.Web/bin"

$processusExistant = Get-CimInstance Win32_Process -Filter "Name = 'erpWeb.Web.exe'" |
    Where-Object { $_.ExecutablePath -like "$dossierBinaires*" } |
    Select-Object -First 1

if ($processusExistant) {
    Write-Host "L'application erpWeb est deja en cours d'execution (PID $($processusExistant.ProcessId))."
    exit 0
}

Write-Host "Demarrage de erpWeb.Web..."

$processus = Start-Process -FilePath "dotnet" `
    -ArgumentList @("run", "--project", $cheminProjetWeb) `
    -RedirectStandardOutput $fichierJournal `
    -RedirectStandardError $fichierJournalErreurs `
    -WindowStyle Hidden `
    -PassThru

Set-Content -Path $fichierPid -Value $processus.Id

Write-Host "erpWeb demarre (PID $($processus.Id))."
Write-Host "Journal : $fichierJournal"
Write-Host "URL : http://localhost:5271"
