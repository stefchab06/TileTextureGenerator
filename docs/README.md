# TileTextureGenerator - Documentation Technique

Cette documentation technique décrit l'architecture et le design de l'application **TileTextureGenerator**.

---

## 📐 Diagrammes PlantUML

Les diagrammes sont au format **PlantUML** (`.puml`). Pour les visualiser :

### **Outils recommandés**
- **Visual Studio Code** : Extension [PlantUML](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml)
- **IntelliJ IDEA** : Plugin PlantUML intégré
- **En ligne** : [PlantUML Web Server](http://www.plantuml.com/plantuml/uml/)

### **Liste des diagrammes**

#### **1. Architecture Hexagonale**
- **Fichier** : [`diagrams/architecture-hexagonale-overview.puml`](diagrams/architecture-hexagonale-overview.puml)
- **Description** : Vue d'ensemble de l'architecture Ports & Adapters (Hexagonal Architecture)
- **Contenu** : Couches (Core, Adapters, Infrastructure, UI), flux de dépendances, pattern Ports & Adapters

#### **2. Entités du Domaine**
- **Fichier** : [`diagrams/domain-entities.puml`](diagrams/domain-entities.puml)
- **Description** : Diagramme de classes détaillé des entités du domaine
- **Contenu** : 
  - `ProjectBase` et sous-classes (`FloorTileProject`, `WallTileProject`)
  - `TransformationBase` et sous-classes (`HorizontalFloorTransformation`, `VerticalWallTransformation`)
  - Registries (`TextureProjectRegistry`, `TransformationTypeRegistry`)
  - Value Objects (`ImageData`, `EdgeFlapConfiguration`)
  - Enums (`ProjectStatus`, `TileShape`, `ImageSide`, `EdgeFlapMode`, etc.)

#### **3. Séquence : Création de Projet**
- **Fichier** : [`diagrams/sequence-create-project.puml`](diagrams/sequence-create-project.puml)
- **Description** : Flux complet de création d'un nouveau projet
- **Acteurs** : User → UI → ViewModel → UseCase → Core → Persistence → FileSystem
- **Points clés** :
  - Validation (name, type, uniqueness)
  - Registry pattern (instanciation polymorphique)
  - Initialize-after-construction
  - Ports isolent le Core
  - Navigation avec ViewModel

#### **4. Séquence : Chargement de Projet**
- **Fichier** : [`diagrams/sequence-load-project.puml`](diagrams/sequence-load-project.puml)
- **Description** : Flux complet de chargement d'un projet existant
- **Acteurs** : User → UI → ViewModel → UseCase → Core → Persistence → FileSystem
- **Points clés** :
  - Désérialisation polymorphique (via Registry)
  - Chargement images depuis disque
  - Réutilisation de `EditProjectUseCase` (même workflow que Create)

---

## 🏗️ Architecture

### **Hexagonal Architecture (Ports & Adapters)**

Le projet suit une **architecture hexagonale stricte** :

```
┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION (UI)                       │
│           MAUI App + ViewModels + Pages + Converters         │
└───────────────────────────┬─────────────────────────────────┘
                            │ uses
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    DRIVING ADAPTERS                          │
│              Adapters.UseCases (Use Cases)                   │
└───────────────────────────┬─────────────────────────────────┘
                            │ uses interfaces
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                         CORE                                 │
│     ┌─────────────────────────────────────────────┐         │
│     │ INPUT PORTS (Driving)                       │         │
│     │   IProjectsManager                          │         │
│     └────────────┬────────────────────────────────┘         │
│                  │ implemented by                            │
│                  ▼                                            │
│     ┌─────────────────────────────────────────────┐         │
│     │ DOMAIN LOGIC                                │         │
│     │   Entities (ProjectBase, TransformationBase)│         │
│     │   Services (ProjectsManager)                │         │
│     │   Registries (Factory pattern)              │         │
│     └────────────┬────────────────────────────────┘         │
│                  │ uses interfaces                           │
│                  ▼                                            │
│     ┌─────────────────────────────────────────────┐         │
│     │ OUTPUT PORTS (Driven)                       │         │
│     │   IProjectsStore, IProjectStore, etc.       │         │
│     └─────────────────────────────────────────────┘         │
└───────────────────────────┬─────────────────────────────────┘
                            │ implemented by
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    DRIVEN ADAPTERS                           │
│        Adapters.Persistence (JSON storage)                   │
└───────────────────────────┬─────────────────────────────────┘
                            │ uses
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    INFRASTRUCTURE                            │
│         FileSystem Service + Embedded Resources              │
└─────────────────────────────────────────────────────────────┘
```

### **Règle de dépendance**

Les dépendances pointent **toujours vers l'intérieur** :

```
UI → Adapters → CORE ← Adapters ← Infrastructure
```

Le **Core n'a AUCUNE dépendance** vers les couches externes (isolation complète).

---

## 🎯 Patterns de conception utilisés

### **1. Hexagonal Architecture (Ports & Adapters)**
- **Ports Input** : Interfaces définies dans Core, implémentées par Core.Services
- **Ports Output** : Interfaces définies dans Core, implémentées par Adapters
- **Adapters** : Couche intermédiaire entre UI et Core, entre Core et Infrastructure

### **2. Registry Pattern (Factory + Dependency Injection)**
- `TextureProjectRegistry` : Instanciation polymorphique de projets
- `TransformationTypeRegistry` : Instanciation polymorphique de transformations
- Factory injectée au démarrage par DI
- Enregistrement automatique via constructeurs statiques

### **3. Initialize-after-construction**
- Constructeur : DI uniquement (injection de dépendances)
- `Initialize(...)` : Données runtime + validation
- Permet l'immutabilité sélective (`Name`, `Id`)

### **4. Use Cases (Application Layer)**
- `ManageProjectListUseCase` : Orchestration des opérations sur la liste de projets
- `EditProjectUseCase` : Façade pour l'édition d'un projet individuel
- Les Use Cases **utilisent** les Ports Input, ne les implémentent **pas**

### **5. Domain-Driven Design (DDD)**
- **Entities** : `ProjectBase`, `TransformationBase` (avec identité)
- **Value Objects** : `ImageData`, `EdgeFlapConfiguration` (immutables)
- **Aggregates** : `ProjectBase` (racine d'agrégat, gère ses Transformations)
- **DTOs** : Objets de transfert entre couches

---

## 📂 Structure des projets

| Projet | Responsabilité | Dépendances |
|--------|---------------|-------------|
| **TileTextureGenerator.Core** | Logique métier pure (Entities, Services, Ports) | Aucune (sauf SkiaSharp pour génération d'images) |
| **TileTextureGenerator.Adapters.Persistence** | Persistance JSON (implémente Ports Output) | Core, Infrastructure.FileSystem |
| **TileTextureGenerator.Adapters.UseCases** | Use Cases (orchestration pour UI) | Core |
| **TileTextureGenerator.Infrastructure.FileSystem** | Services I/O fichiers | Aucune |
| **TileTextureGenerator** (MAUI App) | Point d'entrée, configuration DI | Tous les projets |
| **TileTextureGenerator.Presentation.UI** (Library) | Pages, ViewModels, Converters MAUI | Adapters.UseCases |

---

## 🧪 Tests

| Projet de tests | Couverture |
|-----------------|------------|
| **TileTextureGenerator.Core.Tests** | 91 tests (Entities, Services, Registries) |
| **TileTextureGenerator.Adapters.Persistence.Tests** | 147 tests (Stores, Helpers, Serialization) |
| **TileTextureGenerator.Adapters.UseCases.Tests** | 46 tests (Use Cases, Results) |

**Total : 284 tests** ✅

---

## 🔄 Workflows principaux

### **Créer un projet**
1. User → ManageProjectListPage → Click "Create"
2. Enter name + select type
3. ViewModel → ManageProjectListUseCase.CreateProjectAsync()
4. UseCase → IProjectsManager.CreateProjectAsync()
5. ProjectsManager → TextureProjectRegistry.Create()
6. Registry creates ProjectBase via DI factory
7. ProjectsManager → IProjectsStore.SaveAsync()
8. JsonProjectsStore saves to JSON file
9. UseCase wraps in EditProjectUseCase
10. Navigate to EditProjectPage

### **Charger un projet**
1. User → ManageProjectListPage → Click "Load"
2. ViewModel → ManageProjectListUseCase.LoadProjectAsync()
3. UseCase → IProjectsManager.SelectProjectAsync()
4. ProjectsManager → IProjectsStore.LoadAsync()
5. JsonProjectsStore deserializes JSON → ProjectBase
6. Registry used for polymorphic type resolution
7. UseCase wraps in EditProjectUseCase
8. Navigate to EditProjectPage

### **Ajouter une transformation**
1. User → EditProjectPage → Select type in Picker + Click "+"
2. ViewModel → EditProjectUseCase.AddTransformationAsync()
3. EditProjectUseCase → ProjectBase.AddTransformationAsync()
4. ProjectBase → TransformationTypeRegistry.Create()
5. Registry creates TransformationBase via DI factory
6. ProjectBase → IProjectStore.AddTransformationAsync()
7. JSonProjectStore saves transformation to JSON

---

## 🔑 Concepts clés

### **Immutabilité sélective**
- `ProjectBase.Name` : Immutable après `Initialize()`
- `TransformationBase.Id` : Immutable après `Initialize()`
- Autres propriétés mutables pour la logique métier

### **Séparation des responsabilités**
- **Entities (Core)** : Logique métier uniquement
- **Adapters (Persistence)** : Sérialisation/désérialisation JSON
- **Infrastructure** : I/O fichiers, services externes
- **Pas de sérialisation dans Core** (pas d'attributs `[JsonProperty]`, etc.)

### **Ordre des transformations**
- L'ordre dans `ProjectBase.Transformations` (List) détermine l'ordre d'exécution
- Index 0 = première transformation, 1 = deuxième, etc.

### **Gestion polymorphique**
- `TextureProjectRegistry` : Types de projets (FloorTile, WallTile)
- `TransformationTypeRegistry` : Types de transformations (HorizontalFloor, VerticalWall)
- Reflection + DI factory pour instanciation dynamique

---

## 📚 Références

- **Architecture Hexagonale** : [Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- **Ports & Adapters** : Pattern pour isoler le domaine métier
- **DDD** : Domain-Driven Design (Evans)
- **Clean Architecture** : Robert C. Martin (Uncle Bob)

---

## 🛠️ Technologies utilisées

- **.NET 10** / **C# 14**
- **.NET MAUI** (UI multi-plateforme)
- **SkiaSharp** (Génération d'images)
- **xUnit** (Tests unitaires)
- **JSON** (Persistance)
- **Dependency Injection** (.NET built-in)

---

## 📝 Notes de développement

- **Langue code** : 100% Anglais (maintenabilité internationale)
- **Langue discussions** : Français (langue de l'équipe)
- **Conventions** : File-scoped namespaces, target-typed new, expression properties
- **Tests** : Arrange-Act-Assert (AAA), nommage descriptif (`WhenX_ThenY`)

---

**Dernière mise à jour** : 2026-04-03
