<#
Arrete l'application erpWeb.Web demarree via demarrer-application.ps1.
#>

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$fichierPid = Join-Path $racineDepot ".erpweb-app.pid"

if (-not (Test-Path $fichierPid)) {
    Write-Host "Aucune application erpWeb en cours (fichier PID introuvable)."
    exit 0
}

$identifiantProcessus = Get-Content $fichierPid -ErrorAction SilentlyContinue

if (-not $identifiantProcessus -or -not (Get-Process -Id $identifiantProcessus -ErrorAction SilentlyContinue)) {
    Write-Host "L'application erpWeb n'est plus en cours d'execution."
    Remove-Item $fichierPid -Force
    exit 0
}

Write-Host "Arret de erpWeb (PID $identifiantProcessus)..."

# dotnet run demarre un processus enfant (dotnet exec) : on arrete tout l'arbre.
taskkill /PID $identifiantProcessus /T /F | Out-Null

Remove-Item $fichierPid -Force

Write-Host "erpWeb arrete."
