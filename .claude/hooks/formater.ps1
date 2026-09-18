# PostToolUse (Edit|Write) : applique la mise en forme .editorconfig au fichier C# modifie.
# Utilise le mode --folder (sans chargement de la solution) pour rester rapide ; non bloquant.
$ErrorActionPreference = 'Continue'

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
$chemin = [string] $donnees.tool_input.file_path
if ($chemin -notmatch '\.cs$' -or -not (Test-Path -LiteralPath $chemin)) { exit 0 }

$racine = git -C (Split-Path -Parent $chemin) rev-parse --show-toplevel 2>$null
if (-not $racine) { exit 0 }

$complet = (Resolve-Path -LiteralPath $chemin).Path -replace '\\', '/'
$racine = $racine -replace '\\', '/'
if (-not $complet.StartsWith($racine, [StringComparison]::OrdinalIgnoreCase)) { exit 0 }
$relatif = $complet.Substring($racine.Length).TrimStart('/')

Push-Location $racine
try {
    dotnet format whitespace --folder --include $relatif --verbosity quiet 2>&1 | Out-Null
}
finally {
    Pop-Location
}

exit 0
