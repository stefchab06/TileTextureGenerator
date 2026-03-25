# Architecture TileTextureGenerator

## Vue d'ensemble

**TileTextureGenerator** est une application .NET 10 pour générer des textures de tuiles imprimables pour jeux de plateau (dungeon tiles, floor tiles, wall tiles, etc.). Le projet suit une **architecture hexagonale (Ports & Adapters)** avec des principes de **Domain-Driven Design (DDD)**.

### Objectif métier
L'application génère des **images imprimables** destinées à être utilisées avec les éléments du système **Tales on Tiles** (https://www.tales-on-tiles.com/).
Ces éléments physiques permettent de créer des donjons, terrains et décors pour jeux de rôle et wargames. L'application permet aux utilisateurs de personnaliser les textures appliquées à ces tuiles.

### Technologies
- **.NET 10** (C# 14)
- **Architecture hexagonale** (Clean Architecture)
- **xUnit** pour les tests
- **JSON** pour la persistance (file system-based)
- **DI natif .NET** pour l'injection de dépendances
- **UI future** : .NET MAUI (multi-plateforme : Windows, macOS, Linux, potentiellement mobile)

### Conventions linguistiques
- **Discussion d'équipe** : Français (langue maternelle)
- **Code source** : 100% Anglais (noms de classes, méthodes, propriétés, paramètres)
- **Documentation** : Anglais (XML docs, commentaires inline, README techniques)
- **Documentation métier** : Français acceptable (ce fichier, specifications fonctionnelles)
- **Instructions Visual Studio** : En français (l'IDE est configuré en français)
- **Raison** : Maintenabilité, collaboration internationale, standards de l'industrie

---

## Architecture hexagonale - Couches

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation.UI (MAUI)                   │
│              ViewModels, Pages, Views                       │
└────────────────────────┬────────────────────────────────────┘
                         │ utilise (classes concrètes)
                         ↓
┌─────────────────────────────────────────────────────────────┐
│               Adapters.UseCases                             │
│         Orchestrateurs de scénarios UML                     │
│    (CreateProjectUseCase, DeleteProjectUseCase, etc.)       │
└────────────────────────┬────────────────────────────────────┘
                         │ utilise (via interfaces)
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                   Core (Hexagone)                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Ports/Input (Interfaces)                            │   │
│  │    IProjectsManager, IProjectManager, etc.           │   │
│  └──────────────┬───────────────────────────────────────┘   │
│                 │ implémenté par                            │
│  ┌──────────────▼───────────────────────────────────────┐   │
│  │  Services (Implémentations)                          │   │
│  │    ProjectsManager (opérations métier atomiques)     │   │
│  └──────────────┬───────────────────────────────────────┘   │
│                 │ utilise                                   │
│  ┌──────────────▼───────────────────────────────────────┐   │
│  │  Entities (ProjectBase, TransformationBase)          │   │
│  │  DTOs, Enums, Models, Registries                     │   │
│  └──────────────┬───────────────────────────────────────┘   │
│                 │ utilise (via interfaces)                  │
│  ┌──────────────▼───────────────────────────────────────┐   │
│  │  Ports/Output (Interfaces)                           │   │
│  │    IProjectsStore, ITransformationStore              │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │ implémenté par
                         ↓
┌─────────────────────────────────────────────────────────────┐
│            Adapters.Persistence                             │
│         JsonProjectsStore, JsonTransformationStore          │
└────────────────────────┬────────────────────────────────────┘
                         │ utilise
                         ↓
┌─────────────────────────────────────────────────────────────┐
│          Infrastructure.FileSystem                          │
│              I/O fichiers bas niveau                        │
└─────────────────────────────────────────────────────────────┘
```

### Principes directeurs

1. **Le Core ne dépend de rien** : pas de références externes, seulement .NET BCL
2. **Les Adapters dépendent du Core** : implémentent les interfaces (Ports)
3. **Inversion de dépendances** : Core définit les interfaces, Adapters les implémentent
4. **Isolation de la persistance** : aucune logique de sérialisation dans Core
5. **Use Cases = Orchestrateurs** : Les use cases UTILISENT les ports Input, ne les implémentent PAS
6. **Ports Input implémentés dans Core/Services** : Les services métier implémentent les ports Input
7. **Pas d'interfaces superflues** : Use Cases sont des classes concrètes (pas d'interfaces sauf si testabilité requise)

---

## Core - Entités du domaine

### ProjectBase (Entité racine)

Classe abstraite pour tous les projets de génération de textures.

#### Propriétés
- `Name` : string (unique, immutable après Initialize)
- `Type` : string (nom de classe pour polymorphisme)
- `Status` : ProjectStatus enum
- `DisplayImage` : byte[]? (PNG 256x256 pour UI)
- `SourceImage` : byte[]? (PNG pleine résolution - dans classes concrètes)
- `LastModifiedDate` : DateTime (UTC)
- `Transformations` : List<TransformationDTO> (ordre = index)

#### Pattern Initialize-after-construction

```csharp
// Création via registry (avec DI)
var project = TextureProjectRegistry.Create("FloorTileProject", "MyFloorProject");
// Project est maintenant initialisé et prêt à l'emploi

// L'ordre est garanti :
// 1. Constructeur injecte le store
// 2. Initialize() définit Name et Type
// 3. L'objet est valide
```

**Pourquoi ?**
- Permet l'injection de dépendances via constructeur
- Valide les données runtime (name, id)
- Garantit l'immutabilité après initialisation

#### Classes concrètes
- `FloorTileProject` : tuiles horizontales, propriété `TileShape`
- `WallTileProject` : tuiles verticales

#### Méthodes importantes
- `Initialize(string name)` : initialise le projet (appelé une seule fois)
- `SaveChangesAsync()` : sauvegarde via le store injecté
- `SetDisplayImage(byte[], IImageProcessingService)` : convertit et stocke l'image d'affichage

---

### TransformationBase (Entité)

Classe abstraite pour tous les types de transformations (opérations sur images source).

#### Propriétés
- `Id` : Guid (unique, immutable après Initialize)
- `Type` : string (nom de classe)
- `Icon` : byte[]? (propriété abstraite, PNG 32x32 ou 64x64)
- `RequiredPaperType` : PaperType (virtual, défaut = Standard)
- `this[ImageSide]` : indexeur pour les `EdgeFlapConfiguration` (4 côtés)

#### Classes concrètes
- `HorizontalFloorTransformation` : transformation pour floor tiles
- `VerticalWallTransformation` : transformation pour wall tiles

#### EdgeFlapConfiguration
- Configuration des rabats sur les bords (Top, Right, Bottom, Left)
- Stockée dans un tableau interne de 4 éléments (indexé par ImageSide enum)

---

## Registres - Instanciation polymorphique

### TextureProjectRegistry

Permet de créer dynamiquement des projets par nom de type.

```csharp
// Au démarrage de l'application
TextureProjectRegistry.SetFactory(type => 
    (ProjectBase)serviceProvider.GetRequiredService(type));

// Les types sont enregistrés automatiquement via constructeurs statiques
// FloorTileProject, WallTileProject, etc.

// Création d'un projet
var project = TextureProjectRegistry.Create("FloorTileProject", "MyProject");
```

### TransformationTypeRegistry

Permet de créer dynamiquement des transformations par nom de type.

```csharp
// Configuration similaire
TransformationTypeRegistry.SetFactory(type => 
    (TransformationBase)serviceProvider.GetRequiredService(type));

// Création d'une transformation
var transformation = TransformationTypeRegistry.Create("HorizontalFloorTransformation");
// L'ID (Guid) est généré automatiquement
```

**Important** : Les factories DI doivent être injectées **au démarrage** de l'application, sinon `InvalidOperationException`.

---

## DTOs et Value Objects

### TransformationDTO
- Représentation sérialisable d'une transformation
- Contient `Id`, `Type`, et toutes les propriétés de configuration
- Utilisé dans `ProjectBase.Transformations`

### ProjectDto
- Représentation sérialisable complète d'un projet
- Utilisé par l'adaptateur Persistence pour JSON

### EdgeFlapConfiguration (Value Object)
- Configuration des rabats de bords
- 4 instances par transformation (une par côté)

---

## Ports (Interfaces)

### Input Ports (Use Cases / Services entrants)

- `IProjectManager` : opérations CRUD sur un projet
- `ITransformationManager` : opérations CRUD sur une transformation
- `IProjectsManager` : gestion de plusieurs projets
- `IImageInitializationService` : services d'initialisation d'images

### Output Ports (Adaptateurs sortants)

- `IProjectStore<TProject>` : persistance pour projets
- `ITransformationStore<TTransformation>` : persistance pour transformations
- `IImageProcessingService` : traitement d'images (redimensionnement, conversion)

**Principe** : Le Core définit les interfaces, les Adapters les implémentent.

---

## Adapters

### Persistence (JSON)

- **Responsabilité** : sérialiser/désérialiser ProjectBase et TransformationBase en JSON
- **Implémente** : `IProjectStore`, `ITransformationStore` (via Infrastructure.FileSystem)
- **Format** : JSON avec polymorphisme via propriété `Type`
- **Définit** : `IFileStorage` (son propre port pour I/O fichiers)
- **État** : `JsonProjectsStore` implémenté avec 23 tests ✅, `JsonTransformationsStore` à venir

#### ⚠️ TODO: Transformations

**État actuel** :
- ✅ Les transformations sont **sauvegardées** dans le JSON (préservées lors des updates)
- ❌ Les transformations **ne sont PAS chargées** depuis le JSON (skip intentionnel)
- 📋 **À faire** : Implémenter `JsonTransformationsStore` pour gérer le chargement complet
- 📝 **Rappel** : Test skippé `LoadAsync_WithTransformations_LoadsTransformationsFromJson` à réactiver

**Voir** : `JsonProjectsStore.cs` ligne avec `TODO: Load transformations when TransformationsStore is implemented`

#### Convention critique : Chemins dans JSON (PathHelper)

**TOUJOURS utiliser `PathHelper` pour tous les chemins de fichiers stockés en JSON** :

```csharp
// ✅ Lors de la sérialisation (Entity → JSON)
dto.SourceImagePath = PathHelper.ToJsonPath(entity.SourceImagePath);

// ✅ Lors de la désérialisation (JSON → Entity)
entity.SourceImagePath = PathHelper.ToPlatformPath(dto.SourceImagePath);
```

**Raison** : Les fichiers JSON utilisent **TOUJOURS** des slashes Unix (`/`) pour la portabilité entre Windows, Linux et macOS. 
Le `PathHelper` (dans `Adapters.Persistence/Utilities/`) convertit automatiquement :
- `ToJsonPath()` : chemin plateforme → JSON (`\` → `/`)
- `ToPlatformPath()` : JSON → chemin plateforme (`/` → `\` sur Windows)

**Résultat** : Les fichiers JSON peuvent être transférés entre systèmes sans modification.

### Infrastructure.FileSystem

- **Responsabilité** : I/O fichiers bas niveau, accès disque
- **Implémente** : `IFileStorage` (défini dans Adapters.Persistence)
- Fournit les implémentations concrètes de stores
- Ne connaît PAS les entités du domaine (ProjectBase, etc.)

---

## Énumérations du domaine

### TileShape
```csharp
Full            // 2"x2" carré
HalfHorizontal  // 1"x2" rectangle horizontal
HalfVertical    // 2"x1" rectangle vertical
```

### ProjectStatus
```csharp
Unexisting  // Projet pas encore créé
Empty       // Projet créé, pas d'image source
Ready       // Image source chargée, prêt à générer
Generating  // Génération en cours
Generated   // Textures générées avec succès
Error       // Erreur lors de la génération
```

### ImageSide
```csharp
Top    = 0  // Haut
Right  = 1  // Droite
Bottom = 2  // Bas
Left   = 3  // Gauche
```

### PaperType
```csharp
Standard  // Papier normal (80-120g)
Heavy     // Carton épais (200-300g)
```

---

## Patterns et principes appliqués

### SOLID
- **S** : Chaque entité a une responsabilité unique
- **O** : Extension via héritage (ProjectBase, TransformationBase)
- **L** : Substitution correcte (FloorTileProject IS-A ProjectBase)
- **I** : Interfaces ségrégées (IProjectManager, IProjectStore séparés)
- **D** : Inversion de dépendances (Core définit les Ports)

### DDD (Domain-Driven Design)
- **Entities** : ProjectBase, TransformationBase (identité + cycle de vie)
- **Value Objects** : EdgeFlapConfiguration (immutable, pas d'identité)
- **Aggregates** : ProjectBase est la racine (contient Transformations)
- **Repositories** : IProjectStore (abstraction de persistance)
- **Services** : ProjectsManager (orchestration)

### GoF Patterns
- **Factory** : TextureProjectRegistry, TransformationTypeRegistry
- **Strategy** : TransformationBase (différentes stratégies de transformation)
- **Repository** : IProjectStore, ITransformationStore
- **Template Method** : SaveChangesAsync() dans les entités

---

## Flux de données typique

### Création d'un nouveau projet

```
1. UI/CLI demande création
2. Registry.Create("FloorTileProject", "MyProject")
   ├── Factory DI crée l'instance
   └── Initialize(name) est appelé automatiquement
3. Project.SetDisplayImage(imageData, imageProcessor)
4. Project.TileShape = TileShape.Full
5. Project.SaveChangesAsync()
   └── Store.SaveAsync(project) via Adapter Persistence
6. JSON écrit sur disque via Infrastructure.FileSystem
```

### Ajout d'une transformation

```
1. UI/CLI sélectionne type de transformation
2. Registry.Create("HorizontalFloorTransformation")
   ├── Factory DI crée l'instance
   └── Initialize(Guid.NewGuid()) appelé automatiquement
3. Transformation[ImageSide.Top].Mode = EdgeFlapMode.X
4. Project.Transformations.Add(new TransformationDTO(transformation))
5. Project.SaveChangesAsync()
```

---

## Décisions d'architecture importantes

### Pourquoi Initialize-after-construction ?
- **Problème** : DI ne peut pas passer de paramètres runtime au constructeur
- **Solution** : Constructeur = DI, Initialize = paramètres runtime
- **Avantage** : Immutabilité + validation + DI

### Pourquoi pas d'interfaces pour chaque entité ?
- **Principe YAGNI** : pas d'abstraction sans besoin réel
- Les entités sont déjà abstraites (ProjectBase, TransformationBase)
- Les interfaces sont pour les **services externes** (stores, processors)

### Pourquoi Transformations en List<DTO> et pas List<TransformationBase> ?
- **Sérialisation** : DTOs sont serialization-friendly
- **Découplage** : Project ne dépend pas de toutes les classes de transformations
- **Ordre** : List maintient l'ordre d'exécution (index = ordre)

### Pourquoi deux stores (IProjectStore<ProjectBase> et IProjectStore<FloorTileProject>) ?
- **Covariance** : permet type-safe persistence pour types spécifiques
- **Adapter pattern** : `FloorTileProjectStoreAdapter` wraps le store spécifique
- **Flexibilité** : chaque type peut avoir sa propre stratégie de persistence

---

## Conventions de nommage

### Projets
- `[AppName].Core` : domaine + application
- `[AppName].Adapters.[Type]` : adaptateurs (Persistence, Web, etc.)
- `[AppName].Infrastructure.[Type]` : infrastructure (FileSystem, Database, etc.)
- `[AppName].[LayerName].Tests` : tests unitaires

### Fichiers
- Entités : `ProjectBase.cs`, `FloorTileProject.cs`
- DTOs : `ProjectDto.cs`, `TransformationDTO.cs`
- Enums : `TileShape.cs`, `ProjectStatus.cs`
- Interfaces : `IProjectStore.cs`, `IProjectManager.cs`

### Code
- Classes abstraites : suffixe `Base`
- Interfaces : préfixe `I`
- DTOs : suffixe `DTO` ou `Dto` (cohérent dans tout le projet)
- Async methods : suffixe `Async`

---

## État actuel du projet

### Phase actuelle : Persistance
Nous avons terminé le **cœur métier (Core)** et travaillons maintenant sur la **couche de persistance** (file system-based JSON).
L'objectif est de finaliser la persistance avant de commencer l'interface utilisateur.

### ✅ Implémenté
- Core entities (ProjectBase, TransformationBase)
- Types concrets (FloorTileProject, WallTileProject)
- Transformations concrètes (HorizontalFloorTransformation, VerticalWallTransformation)
- Registres avec factory pattern (TextureProjectRegistry, TransformationTypeRegistry)
- DTOs et enums (ProjectDto, TransformationDTO, TileShape, ProjectStatus, etc.)
- Value objects (EdgeFlapConfiguration)
- Ports/Interfaces (IProjectStore, ITransformationStore, IProjectManager, etc.)
- Structure de tests (Core.Tests, Persistence.Tests)

### 🚧 En cours (priorité actuelle)
- **Adaptateur Persistence** : JSON serialization/deserialization avec gestion du polymorphisme
- **Infrastructure.FileSystem** : Lecture/écriture de fichiers JSON
- **Tests de persistence** : Validation de la sérialisation/désérialisation

### 📋 À venir (après persistance)
1. **UI multi-plateforme** : .NET MAUI (Windows, macOS, Linux)
   - Proposition initiale : MAUI (sujet à révision)
   - Multi-langue (i18n/l10n)
   - Design responsive
2. **Génération d'images** : Traitement effectif des textures
   - Image processing (rotation, découpe, redimensionnement)
   - Application des EdgeFlaps
   - Génération des fichiers finaux
3. **Export** : Vers imprimante/PDF
4. **CLI tools** : Interface en ligne de commande (optionnel)

---

## Guide de contribution

### Ajouter une nouvelle fonctionnalité

1. **Identifier la couche** : Core, Adapter, ou Infrastructure ?
2. **Core-first** : commencer par le domaine
3. **Définir les Ports** : ajouter interfaces si nécessaire
4. **Implémenter les Adapters** : connecter le Core au monde extérieur
5. **Tester** : tests unitaires (Core) + tests d'intégration (Adapters)

### Modifier une entité existante

1. **Lire le code** actuel (ne pas deviner)
2. **Respecter l'immutabilité** (Name, Id)
3. **Maintenir la cohérence** : si une méthode change, vérifier les classes sœurs
4. **Tester** : ajouter/modifier tests pour couvrir le changement
5. **Compiler** : `dotnet build` avant de commit

### Ajouter un nouveau type de projet ou transformation

1. **Créer la classe concrète** dans `ConcreteProjects/` ou `ConcreteTransformations/`
2. **Hériter** de `ProjectBase` ou `TransformationBase`
3. **Constructeur** : injecter le store approprié
4. **Constructeur statique** : enregistrer dans le registry
5. **Propriétés spécifiques** : ajouter les propriétés métier
6. **Override** : `SaveChangesAsync()` pour logique spécifique
7. **Tests** : créer `[ClassName]Tests.cs` avec scénarios comportementaux

---

## Gestion des dépendances et DI

### Configuration au démarrage (pseudo-code)

```csharp
// Dans Program.cs ou Startup.cs
services.AddTransient<IProjectStore<FloorTileProject>, FloorTileProjectStore>();
services.AddTransient<IProjectStore<WallTileProject>, WallTileProjectStore>();
services.AddTransient<ITransformationStore<TransformationBase>, TransformationStore>();
services.AddTransient<IImageProcessingService, ImageProcessingService>();

services.AddTransient<FloorTileProject>();
services.AddTransient<WallTileProject>();
// etc.

// Configurer les registres
TextureProjectRegistry.SetFactory(type => 
    (ProjectBase)serviceProvider.GetRequiredService(type));
    
TransformationTypeRegistry.SetFactory(type => 
    (TransformationBase)serviceProvider.GetRequiredService(type));
```

---

## Patterns de tests

### Nomenclature
- Classe test : `[ClassName]Tests`
- Méthode test : `When[Condition]Then[Outcome]` ou `[MethodName]_[Scenario]_[ExpectedResult]`

### Structure AAA
```csharp
[Fact]
public void WhenProjectIsInitializedThenNameIsSet()
{
    // Arrange
    var store = new MockProjectStore();
    var project = new FloorTileProject(store);
    
    // Act
    project.Initialize("TestProject");
    
    // Assert
    Assert.Equal("TestProject", project.Name);
}
```

### Assertions
- Utiliser xUnit assertions
- Être spécifique : `Assert.Equal(expected, actual)` plutôt que `Assert.True(x == y)`
- Tester les cas limites : null, empty, invalid

---

## Gestion des erreurs

### Validation des paramètres
```csharp
ArgumentNullException.ThrowIfNull(parameter);
if (string.IsNullOrWhiteSpace(stringParam))
    throw new ArgumentException("Message", nameof(stringParam));
```

### Exceptions spécifiques
- `ArgumentNullException` : paramètre null
- `ArgumentException` : paramètre invalide
- `InvalidOperationException` : état invalide (ex: pas initialisé)
- Ne **jamais** throw ou catch `Exception` (type base)

### Async et CancellationToken
- Propager `CancellationToken` partout
- `await Task.Delay(ms, cancellationToken)`
- Vérifier `cancellationToken.ThrowIfCancellationRequested()` dans les boucles

---

## Performance et bonnes pratiques

### Asynchronisme
- Async end-to-end (pas de sync-over-async)
- Toutes les méthodes async se terminent par `Async`
- ConfigureAwait(false) dans les bibliothèques

### Mémoire
- Stream pour grandes images (éviter `byte[]` massifs si possible)
- Utiliser `Span<T>` / `Memory<T>` dans les hot paths si mesuré

### Logging
- ILogger injecté (si besoin)
- Logs structurés avec contexte
- Pas de log spam

---

## Checklist avant commit

- [ ] Code compile (`dotnet build` ou **Générer → Générer la solution** dans VS)
- [ ] Tests passent (`dotnet test` ou **Test → Exécuter tous les tests** dans VS)
- [ ] Couverture maintenue ou améliorée (`dotnet-coverage`)
- [ ] Pas de TODO ou code commenté laissé
- [ ] Conventions de nommage respectées
- [ ] Documentation XML pour APIs publiques
- [ ] Pas de secrets ou chemins hardcodés
- [ ] Pas de code auto-généré modifié
- [ ] Commit via **Affichage → Modifications Git** dans Visual Studio

---

## Glossaire métier

### Tuile (Tile)
Élément de décor imprimable pour jeux de plateau (floor, wall, etc.). Taille standard 2"x2".

### Texture
Image source utilisée pour générer les tuiles.

### Transformation
Opération appliquée à une texture source pour produire une tuile finale (rotation, découpe, flaps, etc.).

### Edge Flap (Rabat de bord)
Languette sur le bord d'une tuile pour assemblage 3D.

### Display Image
Miniature PNG 256x256 pour affichage rapide dans l'UI (pas pour génération).

### Source Image
Image pleine résolution utilisée pour la génération des tuiles finales.

---

## Contacts et ressources

- **Repository** : C:\Users\StefC\source\repos\TileTextureGenerator
- **Branch** : master
- **Documentation** : Ce fichier + `.github/copilot-instructions.md`

---

## Notes pour les nouveaux développeurs

1. **Lire cette doc en premier** avant toute modification
2. **Comprendre l'architecture hexagonale** : le Core est isolé
3. **Respecter les patterns** : Initialize-after-construction, Registry factory
4. **Tester** : chaque changement doit avoir un test
5. **Demander** : si un pattern semble étrange, il y a probablement une raison (voir ce doc)
6. **Langue** : On discute en français dans l'équipe, mais **TOUT le code est en anglais** (pas d'exception)
7. **Focus actuel** : Persistance file system → puis UI multi-plateforme → puis génération d'images
8. **Contexte métier** : Voir https://www.tales-on-tiles.com/ pour comprendre les tuiles physiques utilisées

---

## Évolutions futures possibles

- [ ] Interface utilisateur (WPF Desktop App)
- [ ] API REST pour génération côté serveur
- [ ] Support de formats d'image additionnels (SVG, WebP)
- [ ] Cache des transformations générées
- [ ] Batch processing pour plusieurs projets
- [ ] Export direct vers imprimante
- [ ] Templates de projets prédéfinis

---

**Date de création** : 2025  
**Dernière mise à jour** : [À compléter par les développeurs]  
**Mainteneurs** : [À compléter]
