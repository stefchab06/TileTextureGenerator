# Architecture de la sérialisation JSON - Analyse et règles

## ⚠️ PRINCIPE FONDAMENTAL

**On ne sérialise JAMAIS le JSON complet d'un projet ou transformation concret.**

Chaque store sérialise **uniquement ce dont il a besoin** pour son cas d'usage spécifique.
Cette approche est **intentionnelle** et **optimale** pour la performance et la clarté.

---

## Les 3 stores et leurs responsabilités

### 1. JsonProjectsStore (IProjectsStore - Gestion collection)

**Rôle** : Création initiale et listing des projets

**Ce qu'il sérialise** :
- `ProjectDto` uniquement (structure minimale)
- Propriétés : Name, Type, Status, LastModifiedDate, DisplayImage (optionnel)
- Transformations : Structure Option A (object avec GUID keys + Type uniquement)

**Ce qu'il NE sérialise PAS** :
- ❌ Propriétés concrètes des projets (TileSize, OutputFormat, etc.)
- ❌ Propriétés complètes des transformations (EdgeFlap, GeneratedTexture, etc.)
- ❌ SourceImage (volumineux, pas nécessaire pour listing)

**Pourquoi** :
- Création : On crée juste la structure de base, les propriétés concrètes seront ajoutées après
- Listing : On a besoin uniquement de Name, Type, Status pour afficher la liste

**Désérialisation** :
- `LoadAsync()` : Charge un projet complet via registry + reflection
- `DeserializeProjectProperties()` : Charge toutes les propriétés présentes dans le JSON
- `DeserializeTransformationsFromOptionA()` : Charge la liste des TransformationDTO (Id, Type, Icon)

**Gestion ImageData** :
- DisplayImage → dossier `Sources/` avec nom "DisplayImage.png"
- Path stocké : `"displayimagePath": "Sources/DisplayImage.png"`

---

### 2. JSonProjectStore (IProjectStore - Gestion projet complet)

**Rôle** : Sauvegarde des propriétés du projet (pas les transformations complètes)

**Ce qu'il sérialise** :
- Toutes les propriétés publiques de ProjectBase + propriétés concrètes
- SAUF : Transformations (gérées séparément)
- SAUF : ImageData (remplacés par paths)

**Méthodes spécifiques** :
- `SaveAsync(ProjectBase project)` : Sauvegarde toutes propriétés projet
- `AddTransformationAsync(ProjectBase, Guid)` : Ajoute transformation minimale (Type uniquement)
- `RemoveTransformationAsync(ProjectBase, Guid)` : Supprime transformation
- `LoadTransformationAsync(ProjectBase, Guid)` : Charge transformation complète via JSonTransformationStore

**Gestion transformations** :
- Structure : `"transformations": { "guid-1": { "type": "HorizontalFloorTransformation" }, ... }`
- **Pas de propriétés complètes** : elles sont dans les fichiers individuels gérés par JSonTransformationStore
- Icon : dérivé du Type via TransformationTypeRegistry (pas stocké)

**Gestion ImageData** :
- DisplayImage → `Sources/DisplayImage.png`
- SourceImage → `Sources/SourceImage.png`
- Tous dans `Sources/` pour le projet

**Pourquoi** :
- Les transformations sont lourdes et modifiées fréquemment
- On ne veut pas réécrire tout le JSON projet à chaque modification de transformation
- Séparation des responsabilités : projet vs transformations

---

### 3. JSonTransformationStore (ITransformationStore - Gestion transformations)

**Rôle** : Sauvegarde complète des transformations (toutes propriétés)

**Ce qu'il sérialise** :
- Toutes les propriétés publiques de TransformationBase + propriétés concrètes
- **Sérialisation récursive** pour objets imbriqués (EdgeFlapConfiguration, etc.)
- SAUF au root level : Icon, ParentProject, Id (système)

**Sérialisation récursive** (`SerializeValueRecursivelyAsync`) :
- Collections → JsonArray
- Objets complexes → JsonObject avec récursion
- Types simples → JsonSerializer.SerializeToNode
- ImageData imbriqués → Gérés comme les ImageData root

**Gestion ImageData spéciale** :
1. **GeneratedTexture** → dossier `Outputs/` avec nom de fichier réutilisable
   - Path : `"generatedTexturePath": "Outputs/{guid}.png"`
   - Réutilise GUID existant si déjà sauvegardé
2. **Autres ImageData** → dossier `Workspace/` avec GUID unique
   - Path : `"xxxxxPath": "Workspace/{guid}.png"`
   - Pour images temporaires/intermédiaires

**Exceptions selon niveau d'imbrication** :
- **Root level** : Skip Icon, ParentProject, Id, Type (Type géré séparément)
- **Niveaux imbriqués** : Pas de skip (sauf indexed properties)
- **ImageData** : Traitement spécial à tous niveaux (dossier selon propriété)

**Pourquoi** :
- Transformations peuvent avoir des structures complexes imbriquées
- Besoin de tout sauvegarder pour restaurer l'état complet
- GeneratedTexture = output final, doit être dans Outputs/ pour archivage

---

## Règles de sérialisation ImageData

### Contexte détermine le dossier

| Propriété | Store | Dossier | Nom fichier | Réutilisation GUID |
|-----------|-------|---------|-------------|-------------------|
| DisplayImage (Project) | JsonProjectsStore | Sources/ | DisplayImage.png | Non |
| SourceImage (Project) | JSonProjectStore | Sources/ | SourceImage.png | Non |
| GeneratedTexture (Transformation) | JSonTransformationStore | **Outputs/** | {guid}.png | **Oui** |
| Autres ImageData (Transformation) | JSonTransformationStore | Workspace/ | {guid}.png | Oui |
| ImageData imbriqués | JSonTransformationStore | Workspace/ | {guid}.png | Oui |

**Raisons** :
- **Sources/** : Fichiers d'entrée, immuables, noms descriptifs
- **Outputs/** : Résultats finaux de génération, à conserver pour archivage
- **Workspace/** : Fichiers temporaires/intermédiaires, supprimés lors archivage

---

## Structure JSON typique

### Projet (après création + ajout transformation)

```json
{
  "name": "MonProjet",
  "type": "FloorTileProject",
  "status": "Pending",
  "lastModifiedDate": "2025-01-15T10:30:00Z",
  "displayimagePath": "Sources/DisplayImage.png",
  "sourceImagePath": "Sources/SourceImage.png",
  "tileSize": "Full",
  "outputFormat": "A4",
  "transformations": {
    "guid-1": {
      "type": "HorizontalFloorTransformation"
    },
    "guid-2": {
      "type": "VerticalFloorTransformation"
    }
  }
}
```

**Notes** :
- Pas de propriétés de transformation complètes ici
- Icon non stocké (dérivé de Type)
- ImageData remplacés par paths

### Transformation (dans le JSON projet, stockée par JSonTransformationStore)

```json
{
  "guid-1": {
    "type": "HorizontalFloorTransformation",
    "requiredPaperType": "Standard",
    "generatedTexturePath": "Outputs/guid-1.png",
    "edgeFlap": {
      "top": {
        "mode": "None"
      },
      "right": {
        "mode": "Fold",
        "foldAngle": 90
      },
      "bottom": {
        "mode": "None"
      },
      "left": {
        "mode": "Fold",
        "foldAngle": 90
      }
    }
  }
}
```

**Notes** :
- Toutes les propriétés concrètes présentes
- Structures imbriquées (EdgeFlapConfiguration)
- ImageData imbriqués → paths Workspace/
- GeneratedTexture → path Outputs/

---

## Propriétés exclues de la sérialisation

### Système (jamais sérialisées)

| Propriété | Raison | Store concerné |
|-----------|--------|----------------|
| Name | Immutable, identifiant | Tous (sauf création) |
| Type | Identifiant polymorphique | Tous (géré séparément) |
| Id | Immutable, clé | JSonTransformationStore |
| ParentProject | Référence circulaire | JSonTransformationStore |
| Icon | Dérivé de Type via registry | JSonTransformationStore |
| AvailableActions | Propriété calculée (enum Flags) | JsonProjectsStore (ProjectDto) |

### Selon contexte (skip conditionnel)

| Propriété | Contexte skip | Store |
|-----------|---------------|-------|
| Transformations | Save projet | JSonProjectStore (gérées séparément) |
| ImageData | Toujours | Tous (remplacés par paths) |
| SourceImage | Listing | JsonProjectsStore (volumineux) |

---

## Tests actuels et couverture

### JsonProjectsStoreTests (23 tests)

**Couverture** :
- ✅ CRUD basique (Create, Load, Delete, Exists, List)
- ✅ Name conflicts
- ✅ DisplayImage persistence
- ✅ Transformations (structure Option A)
- ✅ Tri par LastModifiedDate

**Manques potentiels** :
- ⚠️ Load avec propriétés concrètes variées ?
- ⚠️ Load avec ImageData autres que DisplayImage ?
- ⚠️ Désérialisation polymorphique complète ?

### JSonTransformationStoreTests (nombre ?)

**À vérifier** :
- ✅ Sérialisation récursive (collections, objets imbriqués) ?
- ✅ ImageData imbriqués ?
- ✅ GeneratedTexture → Outputs/ vs autres → Workspace/ ?
- ✅ Skip logic selon niveau ?
- ✅ Réutilisation GUID pour images ?

---

## Implications pour l'archivage

### Besoin identifié
**ArchiveAsync()** doit :
1. Supprimer dossier `Workspace/` et son contenu
2. Réduire JSON aux propriétés essentielles :
   - **ProjectBase** : Name, Type, Status, DisplayImage, LastModifiedDate, Transformations
   - **TransformationBase** : Id, Type, GeneratedTexture, RequiredPaperType
3. Exclure : SourceImage, EdgeFlap, propriétés concrètes

### Problème actuel
❌ Aucun store ne sérialise "uniquement les propriétés de base"

### Solutions possibles

#### Option A : Helper de filtrage PropertyFilterHelper
- Détermine si une propriété appartient à la classe de base
- Utilisé par une nouvelle méthode `SerializeBasePropertiesOnly()`
- Pas de refactoring massif, juste un ajout ciblé

#### Option B : Mode de sérialisation dans stores existants
- Ajouter paramètre `mode` aux méthodes Save (Full, BaseOnly)
- Modifier logic interne pour skip propriétés selon mode
- Plus invasif, mais cohérent avec architecture existante

#### Option C : Store spécifique ArchiveStore
- Nouveau store dédié à l'archivage
- Logique de sérialisation simplifiée
- Duplication de code, mais isolation complète

---

## Décisions en attente

### Questions à trancher
1. **Approche refactoring** : Option A, B, C ou autre ?
2. **Tests** : Compléter tests existants avant archivage ?
3. **ImageData archivé** : DisplayImage + GeneratedTexture uniquement ?
4. **Transformations archivées** : Structure minimale (Type + Id) ou avec RequiredPaperType ?
5. **Réversibilité** : Peut-on "désarchiver" un projet ?

### Contraintes métier
- ✅ Archivé = génération PDF possible (besoin GeneratedTexture)
- ❌ Archivé = modification transformations impossible (pas de EdgeFlap, SourceImage)
- ✅ Archivé = économie d'espace (suppression Workspace + propriétés inutiles)

---

## Prochaines étapes (après décision approche)

1. **Phase analyse** (session actuelle - 2h)
   - ✅ Documenter architecture existante (ce fichier)
   - ✅ Identifier contraintes et exceptions
   - 🔄 Décider approche implémentation archivage
   - 🔄 Créer plan détaillé avec étapes

2. **Phase implémentation** (sessions futures)
   - Implémenter helper/service selon approche choisie
   - Ajouter ArchiveAsync() à IProjectsStore + JsonProjectsStore
   - Compléter tests si nécessaire
   - Tests d'archivage

3. **Phase intégration** (sessions futures)
   - Service Core (ProjectsManager.ArchiveProjectAsync)
   - UseCase (ManageProjectListUseCase.ArchiveProjectAsync)
   - ViewModel (déjà prêt)
   - Validation end-to-end

---

## Références code importantes

### Helpers existants
- `ImagePersistenceHelper` : Gestion ImageData (save/load, paths, GUID reuse)
- `ProjectJsonHelper` : Load/Save JSON projet (thread-safe)
- `FileNameHelper` : Nettoyage noms fichiers

### Méthodes clés à comprendre
- `JSonTransformationStore.SerializeValueRecursivelyAsync()` : Sérialisation profonde
- `JSonTransformationStore.SerializeTransformationPropertiesAsync()` : Root level
- `JsonProjectsStore.DeserializeProjectProperties()` : Reflection-based load
- `JSonProjectStore.SaveAsync()` : Property-by-property save

### Registres polymorphiques
- `TextureProjectRegistry` : Factory projets (Type → ProjectBase)
- `TransformationTypeRegistry` : Factory transformations (Type → TransformationBase, Icon)

---

## Notes de session

**Date** : 2025-01-XX
**Durée** : 2h
**Contexte** : Implémentation archivage, découverte complexité sérialisation

**Décisions prises** :
- ✅ Documenter architecture avant tout changement
- ⏸️ Décision approche refactoring en attente retour utilisateur

**Points bloquants** :
- Approche finale non décidée (A, B, C ?)
- Tests actuels à valider/compléter

**À faire session suivante** :
1. Décider approche implémentation
2. Créer plan détaillé ARCHIVING_IMPLEMENTATION_PLAN.md
3. Commencer implémentation si temps restant
