# TODO List - Tile Texture Generator

## High Priority

### UI Improvements
- [ ] **Color Picker visuel** : Ajouter un vrai color picker au lieu d'une Entry hexa
  - Actuellement : Entry avec valeur hexa (#808080)
  - Souhaité : Dialog avec palette de couleurs visuelles
  - Options : Package NuGet (ColorPicker control) ou custom control
  - Référence : `EdgeFlapConfigControl.xaml` ligne ~30

### Image Snap Issues
- [ ] **Snap au resize** : L'aimantation ne fonctionne pas pendant le redimensionnement
  - Fichier : `ImageCropCanvasView.cs`
  - Problème : Snap uniquement au relâchement, pas pendant l'opération
  - Solutions possibles :
    1. Appliquer snap pendant resize tout en gardant anchor-based resizing
    2. Snap aux lignes de grille (pas seulement crop edges)
    3. Logique différente pour side handles vs corner handles

## Medium Priority

### Transformations
- [ ] Afficher l'image générée dans la liste des transformations
- [ ] Sauvegarder l'image générée sur disque (Output/)
- [ ] Implémenter l'édition de transformation (bouton ⚙️)
- [ ] Implémenter la suppression avec confirmation
- [ ] Preview en temps réel dans le formulaire de config

### Localization
- [ ] Ajouter les clés manquantes dans AppResources.resx :
  - `TransformationType_FlatHorizontal` (EN: "Flat Horizontal", FR: "Plat Horizontal")
  - `CardinalDirection_North` (EN: "North", FR: "Nord")
  - `CardinalDirection_South` (EN: "South", FR: "Sud")
  - `CardinalDirection_East` (EN: "East", FR: "Est")
  - `CardinalDirection_West` (EN: "West", FR: "Ouest")
  - `EdgeFlap_Mode` (EN: "Mode", FR: "Mode")
  - `EdgeFlap_Color` (EN: "Color", FR: "Couleur")
  - `EdgeFlap_Texture` (EN: "Texture", FR: "Texture")
  - `EdgeFlap_SelectImage` (EN: "Select Image", FR: "Sélectionner une image")

### PDF Generation
- [ ] Implémenter `GeneratePdfAsync()` dans le workflow
- [ ] Compiler toutes les images transformées
- [ ] Générer le PDF dans Output/
- [ ] Marquer le projet comme "Generated"

### Archive
- [ ] Implémenter `ArchiveAsync()` dans le workflow
- [ ] Supprimer Sources/ et Workspace/
- [ ] Garder uniquement le PDF dans Output/
- [ ] Marquer le projet comme "Archived"

## Low Priority

### Code Quality
- [ ] Ajouter tests unitaires pour TransformationBase
- [ ] Ajouter tests pour EdgeFlap rendering
- [ ] Documenter l'architecture des transformations

### Performance
- [ ] Cache des images générées
- [ ] Lazy loading des miniatures
- [ ] Optimisation SkiaSharp (réutilisation des bitmaps)

### Features
- [ ] Undo/Redo pour les transformations
- [ ] Duplication de transformation
- [ ] Export d'une transformation seule (sans PDF)
- [ ] Import/Export de configurations de transformation

---

## Completed ✅
- [x] Architecture de base des transformations
- [x] TransformationTypeRegistry avec auto-enregistrement
- [x] JSON serialization/deserialization
- [x] FlatHorizontalTransformation avec EdgeFlaps
- [x] TransformationsManagementView (liste)
- [x] FlatHorizontalTransformationConfigView (formulaire)
- [x] EdgeFlapConfigControl (control réutilisable)
- [x] ExecuteAsync() génération d'image
- [x] File picker pour textures
- [x] Refresh de la liste après Save
