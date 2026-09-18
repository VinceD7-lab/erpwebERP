# PreToolUse (Bash|PowerShell) : impose le format Conventional Commits.
$ErrorActionPreference = 'Continue'

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
$commande = [string] $donnees.tool_input.command
if ($commande -notmatch 'git\s+commit\b') { exit 0 }

$message = $null
if ($commande -match "<<-?\s*['""]?(\w+)['""]?[^\r\n]*\r?\n(?<corps>[\s\S]*)") {
    # Heredoc bash : git commit -F - <<'EOF'
    $message = $Matches['corps']
}
elseif ($commande -match "@'\s*\r?\n(?<corps>[\s\S]*)") {
    # Here-string PowerShell : git commit -m @'
    $message = $Matches['corps']
}
elseif ($commande -match "-m\s+""(?<corps>[^""]*)""") {
    $message = $Matches['corps']
}
elseif ($commande -match "-m\s+'(?<corps>[^']*)'") {
    $message = $Matches['corps']
}

# Message non analysable (editeur, --amend sans -m, -F fichier) : pas de controle.
if (-not $message) { exit 0 }

$premiereLigne = ($message.Trim() -split '\r?\n')[0].Trim()
$format = '^(feat|fix|refactor|docs|test|chore|ci)(\([^)\s]+\))?!?: \S.*$'

if ($premiereLigne -notmatch $format) {
    [Console]::Error.WriteLine("Message de commit non conforme : '$premiereLigne'")
    [Console]::Error.WriteLine('Format attendu (Conventional Commits, en francais) : <type>(<portee>): <description>')
    [Console]::Error.WriteLine('Types autorises : feat, fix, refactor, docs, test, chore, ci')
    exit 2
}

exit 0
