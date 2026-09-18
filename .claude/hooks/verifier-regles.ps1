# PostToolUse (Edit|Write) : signale a Claude les anti-patterns STUPID et les ecarts d'architecture
# dans le fichier C# qui vient d'etre modifie (code de sortie 2 = message renvoye a Claude).
$ErrorActionPreference = 'Continue'

$donnees = [Console]::In.ReadToEnd() | ConvertFrom-Json
$chemin = [string] $donnees.tool_input.file_path
if ($chemin -notmatch '\.cs$' -or -not (Test-Path -LiteralPath $chemin)) { exit 0 }

$normalise = $chemin -replace '\\', '/'
if ($normalise -notmatch '/src/erpWeb\.(Core|Infrastructure|Web)/') { exit 0 }
$couche = $Matches[1]
if ($normalise -match '/Migrations/') { exit 0 }

$nomFichier = Split-Path -Leaf $chemin
$estCompositionRoot = $nomFichier -in @('Program.cs', 'DependencyInjection.cs')
$estControleur = $normalise -match '/Controllers/'

$regles = [System.Collections.Generic.List[object]]::new()
function Ajouter-Regle([string] $motif, [string] $message, [bool] $active = $true) {
    if ($active) { $regles.Add([pscustomobject]@{ Motif = $motif; Message = $message }) }
}

Ajouter-Regle 'DateTime\.(Now|UtcNow|Today)\b' 'DateTime.Now/UtcNow/Today interdit : injecter TimeProvider (Untestability).'
Ajouter-Regle 'new\s+AppDbContext\s*\(' 'Instanciation directe de AppDbContext : passer par l''injection de dependances (Tight coupling).'
Ajouter-Regle '\bIServiceProvider\b' 'IServiceProvider hors composition root : Service Locator interdit, injecter par constructeur (Singleton/Tight coupling).' (-not $estCompositionRoot)
Ajouter-Regle '\b(private|internal|public|protected)\s+static\s+(?!readonly\b|extern\b|class\b|partial\b|async\b|void\b|implicit\b|explicit\b)[\w<>\[\],\.\?]+\s+\w+\s*(=(?!>)|;)' 'Champ statique modifiable : etat statique interdit (Singleton).'
Ajouter-Regle '\bstatic\s+[\w<>\[\],\.\?]+\s+\w+\s*\{\s*get;\s*set;' 'Propriete statique modifiable : etat statique interdit (Singleton).'
Ajouter-Regle '^\s*using\s+erpWeb\.(Infrastructure|Web)\b' 'Core ne doit dependre ni d''Infrastructure ni de Web (Dependency Inversion).' ($couche -eq 'Core')
Ajouter-Regle '^\s*using\s+(Microsoft\.Data\.SqlClient|Microsoft\.Data\.Sqlite|MailKit|MimeKit|ClosedXML)\b' 'Bibliotheque d''infrastructure dans Core : definir une interface dans Core, l''implementer dans Infrastructure.' ($couche -eq 'Core')
Ajouter-Regle '\b(File|Directory)\.\w+\s*\(' 'Acces direct au systeme de fichiers dans Core : passer par une interface (ex. IStockageFichiers).' ($couche -eq 'Core')
Ajouter-Regle '\bAppDbContext\b' 'Dependance a AppDbContext hors Infrastructure : utiliser IAppDbContext.' ($couche -ne 'Infrastructure' -and -not $estCompositionRoot)
Ajouter-Regle '\bIAppDbContext\b' 'Acces aux donnees dans un controleur : deleguer a un service de Core.' $estControleur

$lignes = @(Get-Content -LiteralPath $chemin -Encoding UTF8)
$violations = [System.Collections.Generic.List[string]]::new()
for ($index = 0; $index -lt $lignes.Count; $index++) {
    $ligne = [string] $lignes[$index]
    if ($ligne -match '^\s*(//|/\*|\*)') { continue }
    foreach ($regle in $regles) {
        if ($ligne -match $regle.Motif) {
            $violations.Add("  ligne $($index + 1) : $($regle.Message)")
        }
    }
}

if ($violations.Count -gt 0) {
    [Console]::Error.WriteLine("Regles de conception enfreintes dans $nomFichier ($couche) :")
    foreach ($violation in $violations) { [Console]::Error.WriteLine($violation) }
    [Console]::Error.WriteLine('Corriger avant de poursuivre (CLAUDE.md, section Regles de conception).')
    exit 2
}

exit 0
