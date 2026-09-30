<#
Arrete l'application erpWeb.Web demarree via demarrer-application.ps1.
#>

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$fichierPid = Join-Path $racineDepot ".erpweb-app.pid"
$dossierBinaires = Join-Path $racineDepot "src\erpWeb.Web\bin"

$identifiantsAArreter = @()

if (Test-Path $fichierPid) {
    $identifiantProcessus = Get-Content $fichierPid -ErrorAction SilentlyContinue
    if ($identifiantProcessus -and (Get-Process -Id $identifiantProcessus -ErrorAction SilentlyContinue)) {
        $identifiantsAArreter += $identifiantProcessus
    }
    Remove-Item $fichierPid -Force
}

# "dotnet run" demarre erpWeb.Web.exe comme processus descendant ; selon la profondeur de
# l'arbre, "taskkill /T" ne le retrouve pas toujours (reparentage Windows). On le recherche
# donc aussi directement par son chemin d'execution.
$identifiantsAArreter += Get-CimInstance Win32_Process -Filter "Name = 'erpWeb.Web.exe'" |
    Where-Object { $_.ExecutablePath -like "$dossierBinaires*" } |
    Select-Object -ExpandProperty ProcessId

$identifiantsAArreter = $identifiantsAArreter | Select-Object -Unique

if (-not $identifiantsAArreter) {
    Write-Host "Aucune application erpWeb en cours."
    exit 0
}

foreach ($identifiant in $identifiantsAArreter) {
    # Un identifiant precedent a pu deja entrainer l'arret de celui-ci (arbre de processus commun).
    if (-not (Get-Process -Id $identifiant -ErrorAction SilentlyContinue)) {
        continue
    }

    Write-Host "Arret de erpWeb (PID $identifiant)..."
    taskkill /PID $identifiant /T /F | Out-Null
}

Write-Host "erpWeb arrete."
