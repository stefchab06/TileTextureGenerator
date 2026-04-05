# TileTextureGenerator - Technical Documentation

This technical documentation describes the architecture and design of the **TileTextureGenerator** application.

---

## 📐 PlantUML Diagrams

Diagrams are in **PlantUML** format (`.puml`). To visualize them:

### **Recommended Tools**
- **Visual Studio Code**: [PlantUML Extension](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml)
- **IntelliJ IDEA**: Built-in PlantUML plugin
- **Online**: [PlantUML Web Server](http://www.plantuml.com/plantuml/uml/)

### **Diagram List**

#### **1. Hexagonal Architecture Overview**
- **File**: [`diagrams/architecture/architecture-hexagonale-overview.puml`](diagrams/architecture/architecture-hexagonale-overview.puml)
- **Description**: Overview of Hexagonal Architecture (Ports & Adapters pattern)
- **Content**: Layers (Core, Adapters, Infrastructure, UI), dependency flow, Ports & Adapters pattern

#### **2. Hexagonal Architecture Detailed**
- **File**: [`diagrams/architecture/architecture-hexagonale.puml`](diagrams/architecture/architecture-hexagonale.puml)
- **Description**: Detailed component diagram with all classes and interfaces
- **Content**: Complete architecture with packages, interfaces, and implementations

#### **3. Domain Entities**
- **File**: [`diagrams/class-diagrams/domain-entities.puml`](diagrams/class-diagrams/domain-entities.puml)
- **Description**: Detailed class diagram of domain entities
- **Content**: 
  - `ProjectBase` and subclasses (`FloorTileProject`, `WallTileProject`)
  - `TransformationBase` and subclasses (`HorizontalFloorTransformation`, `VerticalWallTransformation`)
  - Registries (`TextureProjectRegistry`, `TransformationTypeRegistry`)
  - Value Objects (`ImageData`, `EdgeFlapConfiguration`)
  - Enums (`ProjectStatus`, `TileShape`, `ImageSide`, `EdgeFlapMode`, etc.)

#### **4. Sequence: Create Project**
- **File**: [`diagrams/sequence-diagrams/sequence-create-project.puml`](diagrams/sequence-diagrams/sequence-create-project.puml)
- **Description**: Complete flow for creating a new project
- **Actors**: User → UI → ViewModel → UseCase → Core → Persistence → FileSystem
- **Key Points**:
  - Validation (name, type, uniqueness)
  - Registry pattern (polymorphic instantiation)
  - Initialize-after-construction
  - Ports isolate Core from infrastructure
  - Navigation with ViewModel

#### **5. Sequence: Load Project**
- **File**: [`diagrams/sequence-diagrams/sequence-load-project.puml`](diagrams/sequence-diagrams/sequence-load-project.puml)
- **Description**: Complete flow for loading an existing project
- **Actors**: User → UI → ViewModel → UseCase → Core → Persistence → FileSystem
- **Key Points**:
  - Polymorphic deserialization (via Registry)
  - Loading images from disk
  - Reuses `EditProjectUseCase` (same workflow as Create)

---

## 🏗️ Architecture

### **Hexagonal Architecture (Ports & Adapters)**

The project follows a **strict hexagonal architecture**:

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

### **Dependency Rule**

Dependencies always point **INWARD**:

```
UI → Adapters → CORE ← Adapters ← Infrastructure
```

**Core has NO dependencies** on outer layers (complete isolation).

---

## 🎯 Design Patterns Used

### **1. Hexagonal Architecture (Ports & Adapters)**
- **Input Ports**: Interfaces defined in Core, implemented by Core.Services
- **Output Ports**: Interfaces defined in Core, implemented by Adapters
- **Adapters**: Intermediate layer between UI and Core, between Core and Infrastructure

### **2. Registry Pattern (Factory + Dependency Injection)**
- `TextureProjectRegistry`: Polymorphic instantiation of projects
- `TransformationTypeRegistry`: Polymorphic instantiation of transformations
- Factory injected at startup via DI
- Automatic registration via static constructors

### **3. Initialize-after-construction**
- Constructor: DI only (dependency injection)
- `Initialize(...)`: Runtime data + validation
- Enables selective immutability (`Name`, `Id`)

### **4. Use Cases (Application Layer)**
- `ManageProjectListUseCase`: Orchestration of project list operations
- `EditProjectUseCase`: Facade for individual project editing
- Use Cases **use** Input Ports, they do **not** implement them

### **5. Domain-Driven Design (DDD)**
- **Entities**: `ProjectBase`, `TransformationBase` (with identity)
- **Value Objects**: `ImageData`, `EdgeFlapConfiguration` (immutable)
- **Aggregates**: `ProjectBase` (aggregate root, manages its Transformations)
- **DTOs**: Data transfer objects between layers

---

## 📂 Project Structure

| Project | Responsibility | Dependencies |
|---------|---------------|--------------|
| **TileTextureGenerator.Core** | Pure business logic (Entities, Services, Ports) | None (except SkiaSharp for image generation) |
| **TileTextureGenerator.Adapters.Persistence** | JSON persistence (implements Output Ports) | Core, Infrastructure.FileSystem |
| **TileTextureGenerator.Adapters.UseCases** | Use Cases (orchestration for UI) | Core |
| **TileTextureGenerator.Infrastructure.FileSystem** | File I/O services | None |
| **TileTextureGenerator** (MAUI App) | Entry point, DI configuration | All projects |
| **TileTextureGenerator.Presentation.UI** (Library) | Pages, ViewModels, Converters MAUI | Adapters.UseCases |

---

## 🧪 Tests

| Test Project | Coverage |
|--------------|----------|
| **TileTextureGenerator.Core.Tests** | 91 tests (Entities, Services, Registries) |
| **TileTextureGenerator.Adapters.Persistence.Tests** | 147 tests (Stores, Helpers, Serialization) |
| **TileTextureGenerator.Adapters.UseCases.Tests** | 46 tests (Use Cases, Results) |

**Total: 284 tests** ✅

---

## 🔄 Main Workflows

### **Create Project**
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

### **Load Project**
1. User → ManageProjectListPage → Click "Load"
2. ViewModel → ManageProjectListUseCase.LoadProjectAsync()
3. UseCase → IProjectsManager.SelectProjectAsync()
4. ProjectsManager → IProjectsStore.LoadAsync()
5. JsonProjectsStore deserializes JSON → ProjectBase
6. Registry used for polymorphic type resolution
7. UseCase wraps in EditProjectUseCase
8. Navigate to EditProjectPage

### **Add Transformation**
1. User → EditProjectPage → Select type in Picker + Click "+"
2. ViewModel → EditProjectUseCase.AddTransformationAsync()
3. EditProjectUseCase → ProjectBase.AddTransformationAsync()
4. ProjectBase → TransformationTypeRegistry.Create()
5. Registry creates TransformationBase via DI factory
6. ProjectBase → IProjectStore.AddTransformationAsync()
7. JSonProjectStore saves transformation to JSON

---

## 🔑 Key Concepts

### **Selective Immutability**
- `ProjectBase.Name`: Immutable after `Initialize()`
- `TransformationBase.Id`: Immutable after `Initialize()`
- Other properties remain mutable for business logic

### **Separation of Concerns**
- **Entities (Core)**: Business logic only
- **Adapters (Persistence)**: JSON serialization/deserialization
- **Infrastructure**: File I/O, external services
- **No serialization in Core** (no `[JsonProperty]` attributes, etc.)

### **Transformation Order**
- Order in `ProjectBase.Transformations` (List) determines execution order
- Index 0 = first transformation, 1 = second, etc.

### **Polymorphic Management**
- `TextureProjectRegistry`: Project types (FloorTile, WallTile)
- `TransformationTypeRegistry`: Transformation types (HorizontalFloor, VerticalWall)
- Reflection + DI factory for dynamic instantiation

---

## 📚 References

- **Hexagonal Architecture**: [Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- **Ports & Adapters**: Pattern to isolate domain logic
- **DDD**: Domain-Driven Design (Evans)
- **Clean Architecture**: Robert C. Martin (Uncle Bob)

---

## 🛠️ Technologies Used

- **.NET 10** / **C# 14**
- **.NET MAUI** (Cross-platform UI)
- **SkiaSharp** (Image generation)
- **xUnit** (Unit testing)
- **JSON** (Persistence)
- **Dependency Injection** (.NET built-in)

---

## 📝 Development Notes

- **Code language**: 100% English (international maintainability)
- **Team language**: French
- **Conventions**: File-scoped namespaces, target-typed new, expression properties
- **Tests**: Arrange-Act-Assert (AAA), descriptive naming (`WhenX_ThenY`)

---

**Last updated**: 2025-01-24
