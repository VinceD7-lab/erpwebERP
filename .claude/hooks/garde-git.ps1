# PreToolUse (Bash|PowerShell) : protege main et bloque les commandes destructrices.
$ErrorActionPreference = 'Continue'

function Bloquer([string] $message) {
    [Console]::Error.WriteLine($message)
    exit 2
}

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
$commande = [string] $donnees.tool_input.command
if (-not $commande) { exit 0 }

if ($commande -match 'git\s+push\b[^\r\n;|&]*\s(--force\b|--force-with-lease\b|-f\b)') {
    Bloquer 'GitHub Flow : git push --force est interdit.'
}

if ($commande -match 'dotnet\s+ef\s+database\s+drop\b') {
    Bloquer 'dotnet ef database drop est interdit. Supprimer la base manuellement si necessaire.'
}

if ($commande -match 'git\s+push\b[^\r\n;|&]*\s(\S+:)?main\b') {
    Bloquer 'GitHub Flow : aucun push vers main. Pousser la branche de travail et ouvrir une Pull Request.'
}

if ($commande -match 'git\s+(commit|push)\b') {
    $repertoire = if ($donnees.cwd) { [string] $donnees.cwd } else { (Get-Location).Path }
    $branche = git -C $repertoire rev-parse --abbrev-ref HEAD 2>$null
    if ($branche -eq 'main') {
        Bloquer 'GitHub Flow : aucun commit ni push sur main. Creer une branche <type>/<description> (git switch -c fonctionnalite/...).'
    }
}

exit 0
