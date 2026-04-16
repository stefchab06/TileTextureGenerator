# Script de génération de rapport de couverture de code
# Filtre : TileTextureGenerator.Core, Adapters.Persistence, Adapters.UseCases

# Nettoyer ancien rapport
Write-Host "Nettoyage ancien rapport..." -ForegroundColor Yellow
Remove-Item -Recurse -Force coveragereport -ErrorAction SilentlyContinue

# Lancer tests avec couverture
Write-Host "`nExécution des tests avec couverture..." -ForegroundColor Cyan
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test

# Vérifier que le fichier a été généré
if (Test-Path coverage.cobertura.xml) {
    Write-Host "`nFichier de couverture généré ✓" -ForegroundColor Green
    
    # Générer rapport HTML (seulement les 3 projets testés)
    Write-Host "`nGénération du rapport HTML..." -ForegroundColor Cyan
    reportgenerator `
      -reports:coverage.cobertura.xml `
      -targetdir:coveragereport `
      -reporttypes:Html `
      -assemblyfilters:"+TileTextureGenerator.Core;+TileTextureGenerator.Adapters.Persistence;+TileTextureGenerator.Adapters.UseCases;-*"
    
    Write-Host "`nRapport de couverture généré ✓" -ForegroundColor Green
    Write-Host "Ouverture dans le navigateur..." -ForegroundColor Cyan
    start coveragereport\index.html
} else {
    Write-Host "`nErreur : Le fichier de couverture n'a pas été généré ✗" -ForegroundColor Red
}
