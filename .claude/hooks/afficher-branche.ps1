# SessionStart : rappelle la branche courante (GitHub Flow).
$ErrorActionPreference = 'Continue'

$branche = git rev-parse --abbrev-ref HEAD 2>$null
if (-not $branche) { exit 0 }

Write-Output "Branche Git courante : $branche"
if ($branche -eq 'main') {
    Write-Output "ATTENTION : vous etes sur main. GitHub Flow : creer une branche <type>/<description> avant toute modification."
}
exit 0
