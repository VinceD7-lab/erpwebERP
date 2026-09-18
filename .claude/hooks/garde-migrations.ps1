# PreToolUse (Edit|Write) : interdit la modification d'une migration deja fusionnee dans main.
$ErrorActionPreference = 'Continue'

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
$chemin = [string] $donnees.tool_input.file_path
if ($chemin -notmatch '[\\/]Migrations[\\/][^\\/]+\.cs$') { exit 0 }
if ($chemin -match 'ModelSnapshot\.cs$') { exit 0 }
if (-not (Test-Path -LiteralPath $chemin)) { exit 0 }

$racine = git -C (Split-Path -Parent $chemin) rev-parse --show-toplevel 2>$null
if (-not $racine) { exit 0 }

$complet = (Resolve-Path -LiteralPath $chemin).Path -replace '\\', '/'
$racine = $racine -replace '\\', '/'
if (-not $complet.StartsWith($racine, [StringComparison]::OrdinalIgnoreCase)) { exit 0 }
$relatif = $complet.Substring($racine.Length).TrimStart('/')

git -C $racine cat-file -e "main:$relatif" 2>$null
if ($LASTEXITCODE -eq 0) {
    [Console]::Error.WriteLine("Migration deja fusionnee dans main : $relatif")
    [Console]::Error.WriteLine('Ne jamais modifier une migration fusionnee : creer une nouvelle migration (dotnet ef migrations add <NomEnFrancais>).')
    exit 2
}

exit 0
