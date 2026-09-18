# Stop : avant de rendre la main, verifie que la solution compile et que les regles d'architecture passent.
# Ignore si aucun fichier source n'est modifie ou si Claude poursuit deja suite a ce hook.
$ErrorActionPreference = 'Continue'

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
if ($donnees.stop_hook_active) { exit 0 }

$repertoire = if ($donnees.cwd) { [string] $donnees.cwd } else { (Get-Location).Path }
$racine = git -C $repertoire rev-parse --show-toplevel 2>$null
if (-not $racine) { exit 0 }
Set-Location $racine

$modifies = git status --porcelain -- '*.cs' '*.cshtml' '*.csproj' '*.props' 2>$null
if (-not $modifies) { exit 0 }

$sortieBuild = dotnet build erpWeb.sln -nologo -v q 2>&1
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine('Build en echec : corriger avant de terminer.')
    $sortieBuild | Where-Object { $_ -match ': error ' } | Select-Object -Unique -First 15 | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 2
}

$sortieTests = dotnet test tests/erpWeb.UnitTests --no-build --filter 'FullyQualifiedName~Architecture' -nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine('Tests d''architecture en echec : une regle de dependance entre couches est enfreinte.')
    $sortieTests | Where-Object { $_ -match 'Failed|infraction|Assert' } | Select-Object -First 15 | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 2
}

exit 0
