# Instructions GitHub Copilot - TileTextureGenerator

## Vue d'ensemble du projet

**TileTextureGenerator** est une application de génération de textures pour tuiles de jeu de plateau (floor/wall tiles).
Le projet suit une **architecture hexagonale (Ports & Adapters)** avec DDD et .NET 10 / C# 14.

### Objectif métier
L'application génère des images imprimables destinées à être utilisées avec les éléments vendus sur **https://www.tales-on-tiles.com/**.
Elle permet de créer des textures personnalisées pour floor tiles et wall tiles compatibles avec ce système.

### Langue et conventions
- **Discussion** : Français (langue maternelle de l'équipe)
- **Code** : 100% Anglais (noms de classes, membres, documentation XML, commentaires)
- **Raison** : Maintenabilité internationale et conformité aux standards .NET
- **Visual Studio** : Interface en français - donner les instructions UI en français
- **Documentation technique** : Inclure les termes français ET anglais pour les menus/options VS

### État actuel du développement
1. ✅ **Cœur métier (Core)** : Implémenté (entities, registries, ports)
2. 🚧 **Persistance** : En cours (file system-based JSON)
3. 📋 **UI** : À venir (multi-plateforme avec .NET MAUI, sujet à changement)
4. 📋 **Multi-langue** : Planifié pour l'UI finale

---

## Architecture et Organisation

### Structure des projets

```
TileTextureGenerator.Core/                    # Cœur métier (Domain + Application)
├── Entities/                                 # Entités du domaine
│   ├── ProjectBase.cs                        # Base abstraite pour tous les projets
│   ├── TransformationBase.cs                 # Base abstraite pour toutes les transformations
│   ├── ConcreteProjects/                     # Projets concrets (FloorTileProject, WallTileProject)
│   └── ConcreteTransformations/              # Transformations concrètes (Horizontal, Vertical)
├── DTOs/                                     # Data Transfer Objects
├── Enums/                                    # Énumérations (TileShape, ProjectStatus, ImageSide, etc.)
├── Models/                                   # Value Objects (EdgeFlapConfiguration, etc.)
├── Ports/                                    # Interfaces (contrats)
│   ├── Input/                                # Use cases / services entrants
│   └── Output/                               # Adaptateurs sortants (stores, repos)
├── Registries/                               # Registres pour l'instanciation polymorphique
│   ├── TextureProjectRegistry.cs
│   └── TransformationTypeRegistry.cs
└── Services/                                 # Services métier

TileTextureGenerator.Adapters.Persistence/    # Adapter pour la persistance (JSON)
TileTextureGenerator.Adapters.Persistence.Tests/

TileTextureGenerator.Infrastructure.FileSystem/  # Infrastructure fichiers

TileTextureGenerator.Core.Tests/              # Tests unitaires du Core
```

---

## Concepts métier clés

### 1. ProjectBase (Entité racine)

- **Classe abstraite** pour tous les types de projets (FloorTileProject, WallTileProject)
- **Propriétés principales** :
  - `Name` : nom unique, immutable après `Initialize()`
  - `Type` : identifiant de type (nom de classe) pour instanciation polymorphique
  - `Status` : état du projet (ProjectStatus enum)
  - `DisplayImage` : image PNG 256x256 pour l'UI
  - `Transformations` : liste de `TransformationDTO` (ordre = index dans la liste)
  - `SourceImage` : données image source
- **Pattern** : Initialize-after-construction (appeler `Initialize(name)` après DI)
- **Store** : injecté via DI (`IProjectStore<ProjectBase>`)
- **Pas de logique de sérialisation** dans l'entité (responsabilité de l'adaptateur Persistence)

### 2. TransformationBase (Entité)

- **Classe abstraite** pour tous les types de transformations
- **Propriétés principales** :
  - `Id` : GUID unique, immutable après `Initialize()`
  - `Type` : identifiant de type pour instanciation polymorphique
  - `Icon` : propriété abstraite (PNG), doit être implémentée par les classes concrètes
  - `RequiredPaperType` : type de papier (Standard ou Heavy)
  - `this[ImageSide]` : indexeur pour accéder aux `EdgeFlapConfiguration` (Top, Right, Bottom, Left)
- **Pattern** : Initialize-after-construction (appeler `Initialize(id)` après DI)
- **Store** : injecté via DI (`ITransformationStore<TransformationBase>`)

### 3. Registres (Factory Pattern + DI)

#### TextureProjectRegistry
- Enregistre les types de projets pour instanciation dynamique
- `SetFactory(Func<Type, ProjectBase>)` : injecte le factory DI au démarrage
- `RegisterType<TProject>()` : enregistre un type de projet
- `Create(string key, string name)` : crée et initialise un projet

#### TransformationTypeRegistry
- Enregistre les types de transformations
- `SetFactory(Func<Type, TransformationBase>)` : injecte le factory DI
- `Register<T>()` : enregistre un type de transformation
- `Create(string typeName)` : crée et initialise une transformation avec un nouveau GUID

---

## Règles de conception importantes

### Immutabilité sélective
- `ProjectBase.Name` : immutable après `Initialize()`
- `TransformationBase.Id` : immutable après `Initialize()`
- Les autres propriétés restent mutables pour le fonctionnement métier

### Injection de dépendances
- **Toujours injecter les stores** via constructeur
- Les entités **ne doivent jamais créer leurs propres dépendances**
- Les registres utilisent un **factory pattern** fourni par DI

### Séparation des responsabilités
- **Entités (Core)** : logique métier uniquement
- **Adapters (Persistence)** : sérialisation/désérialisation JSON
- **Infrastructure** : I/O fichiers, services externes
- **Pas de sérialisation dans Core** (pas d'attributs `[JsonProperty]`, etc.)

### Pattern Initialize-after-construction
- Constructeur : injection DI uniquement
- `Initialize()` : données runtime (name, id)
- Raison : permet DI + validation + immutabilité

### Ordre des transformations
- L'ordre dans `ProjectBase.Transformations` (List) détermine l'ordre d'exécution
- Index 0 = première transformation, 1 = deuxième, etc.

---

## Enums et Types importants

### TileShape
- `Full` : 2"x2" carré
- `HalfHorizontal` : 1"x2" rectangle horizontal
- `HalfVertical` : 2"x1" rectangle vertical

### ProjectStatus
- `Unexisting`, `Empty`, `Ready`, `Generating`, `Generated`, `Error`

### ImageSide
- `Top` (0), `Right` (1), `Bottom` (2), `Left` (3)
- Utilisé comme index pour les `EdgeFlapConfiguration`

### PaperType
- `Standard` : papier normal
- `Heavy` : carton épais

### EdgeFlapMode
- Configuration des rabats sur les bords des tuiles

---

## Conventions de code .NET 10 / C# 14

### Style général
- **File-scoped namespaces** : `namespace X;` (pas de blocs)
- **Target-typed new** : `List<T> x = [];`
- **Null checks** : `ArgumentNullException.ThrowIfNull(x)`
- **String checks** : `string.IsNullOrWhiteSpace(x)`
- **Visibilité minimale** : `private` > `internal` > `protected` > `public`

### Patterns encouragés
- Async/Await partout (pas de sync-over-async)
- CancellationToken propagé dans les méthodes async
- Records pour les DTOs/Value Objects
- Expression properties quand approprié

### À éviter
- **N'ajoute PAS d'interfaces** si pas nécessaire pour DI ou tests
- **Ne wrappe PAS** des abstractions existantes
- **Pas de code mort** (méthodes/paramètres inutilisés)
- **Pas de commentaires évidents** (explique le "pourquoi", pas le "quoi")

---

## Tests (xUnit)

### Conventions
- Projet test : `[ProjectName].Tests`
- Classe test : `[ClassName]Tests`
- Nom test : `WhenCatMeowsThenCatDoorOpens` (comportement descriptif)
- Structure : **Arrange-Act-Assert (AAA)**
- Packages : `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`

### Règles strictes
- **Un comportement par test**
- **Tests indépendants** (pas d'ordre, exécutable en parallèle)
- **Pas de branchement** (if/switch) dans les tests
- **Tester via APIs publiques** (pas d'`InternalsVisibleTo`)
- **Assertions spécifiques** (valeurs exactes, pas de vagues `Assert.True`)

---

## Workflow de développement

1. **Comprendre le besoin** (quelle entité, quelle fonctionnalité)
2. **Identifier les couches** (Core, Adapter, Infrastructure)
3. **Modifier/créer les fichiers** appropriés (Core d'abord)
4. **Ajouter/modifier les tests** (TDD si pertinent)
5. **Compiler** (`dotnet build` ou dans VS : **Générer → Générer la solution**)
6. **Exécuter les tests** (`dotnet test` ou dans VS : **Test → Exécuter tous les tests**)
7. **Vérifier la couverture** (si tests ajoutés) : `dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test`
8. **Commit et push** : Via **Affichage → Modifications Git** dans Visual Studio ou en ligne de commande

### Commits Git sous PowerShell
⚠️ **Important** : PowerShell interprète les tirets (`-`) comme des opérateurs dans les messages multi-lignes.

**❌ Ne PAS faire** :
```powershell
git commit -m "feat: Title
- Point 1
- Point 2"
```

**✅ À faire** : Utiliser plusieurs flags `-m` (un pour le titre, un pour le corps) :
```powershell
git commit -m "feat: Title" -m "Point 1. Point 2. Point 3. All tests passing."
```

Ou utiliser des points au lieu de tirets dans un seul message :
```powershell
git commit -m "feat: Title. Point 1. Point 2. All tests passing."
```

---

## Points d'attention spécifiques au projet

### Registres et polymorphisme
- Les types de projets et transformations sont enregistrés **statiquement** au démarrage
- Le factory DI **doit être injecté** via `SetFactory()` avant toute création
- La création d'instances passe **toujours par le registre** (pas de `new` direct)

### Stores et persistance
- Les entités reçoivent leur `IProjectStore` ou `ITransformationStore` via DI
- `SaveChangesAsync()` appelle le store injecté
- La persistance JSON est gérée par l'adaptateur `TileTextureGenerator.Adapters.Persistence`

### Gestion d'images
- `DisplayImage` : PNG 256x256 (UI)
- `SourceImage` : PNG pleine résolution (génération)
- Conversion via `IImageProcessingService` (injecté)

---

## Exemple typique : Ajouter un nouveau type de projet

1. Créer `NewProject.cs` dans `Core/Entities/ConcreteProjects/`
2. Hériter de `ProjectBase`
3. Ajouter constructeur avec `IProjectStore<NewProject>`
4. Implémenter `SaveChangesAsync()` (appeler base + store spécifique)
5. Ajouter propriétés métier spécifiques
6. Enregistrer dans le constructeur statique : `TextureProjectRegistry.RegisterType<NewProject>()`
7. Créer tests dans `Core.Tests/`

---

## Ressources et références

- Voir `ARCHITECTURE.md` pour plus de détails sur l'architecture
- Les DTOs sont dans `Core/DTOs/` (pas de logique métier)
- Les Ports définissent les contrats entre couches
- Les Adapters implémentent les Ports

---

## Notes pour GitHub Copilot

- **Toujours** suivre les conventions du projet existant
- **Ne jamais** ajouter de code de sérialisation dans `Core`
- **Vérifier** que les factories sont bien injectées avant utilisation
- **Respecter** le pattern Initialize-after-construction
- **Compiler** après chaque modification pour valider
- **Langue code** : TOUT le code, documentation et commentaires DOIT être en anglais (même si on discute en français)
- **Langue instructions** : Donner les instructions Visual Studio en FRANÇAIS (l'IDE est en français)
- **Priorité actuelle** : Implémenter la persistance (file system), pas l'UI
