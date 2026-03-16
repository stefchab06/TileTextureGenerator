# TileTextureGenerator

> A .NET application for generating printable texture tiles compatible with [Tales on Tiles](https://www.tales-on-tiles.com/) board game elements.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14.0-239120)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Architecture](https://img.shields.io/badge/architecture-hexagonal-orange)](ARCHITECTURE.md)

---

## 🎯 Purpose

**TileTextureGenerator** allows tabletop gaming enthusiasts to create **custom printable textures** for floor tiles, wall tiles, and other terrain elements compatible with the **Tales on Tiles** system. Generate personalized dungeon tiles, corridors, rooms, and scenery for your role-playing games and wargames.

### Key Features (planned)
- ✅ Custom texture application to floor and wall tiles
- ✅ Multiple transformation types (rotation, flipping, edge flap configuration)
- ✅ Support for different tile shapes (2"×2" full, 1"×2" half-horizontal, 2"×1" half-vertical)
- 🚧 File-based persistence (JSON)
- 📋 Multi-platform UI (.NET MAUI)
- 📋 Multi-language support
- 📋 High-quality printable output (PDF/PNG)

---

## 🏗️ Architecture

This project follows **Hexagonal Architecture (Ports & Adapters)** with **Domain-Driven Design (DDD)** principles:

```
TileTextureGenerator.Core/                    # Business logic (Domain + Application)
├── Entities/                                 # Domain entities
│   ├── ProjectBase.cs                        # Base class for all projects
│   ├── TransformationBase.cs                 # Base class for transformations
│   ├── ConcreteProjects/                     # FloorTileProject, WallTileProject
│   └── ConcreteTransformations/              # Concrete transformation types
├── DTOs/                                     # Data Transfer Objects
├── Enums/                                    # Domain enums
├── Models/                                   # Value Objects
├── Ports/                                    # Interfaces (contracts)
│   ├── Input/                                # Use cases / inbound services
│   └── Output/                               # Outbound adapters (stores, repos)
├── Registries/                               # Polymorphic instantiation registries
└── Services/                                 # Business services

TileTextureGenerator.Adapters.Persistence/    # JSON persistence adapter
TileTextureGenerator.Infrastructure.FileSystem/  # File I/O infrastructure
TileTextureGenerator.Core.Tests/              # Unit tests
TileTextureGenerator.Adapters.Persistence.Tests/
```

For more details, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## 🚀 Current Status

| Component | Status | Description |
|-----------|--------|-------------|
| **Core Domain** | ✅ Complete | Entities, registries, ports implemented |
| **Persistence** | 🚧 In Progress | File system-based JSON serialization |
| **UI** | 📋 Planned | Multi-platform with .NET MAUI |
| **Image Generation** | 📋 Planned | Actual texture processing and output |
| **Localization** | 📋 Planned | Multi-language support |

---

## 🛠️ Technologies

- **.NET 10** with C# 14
- **Hexagonal Architecture** (Clean Architecture)
- **xUnit** for testing
- **JSON** for persistence
- **Dependency Injection** (native .NET)
- **Future UI**: .NET MAUI (cross-platform)

---

## 📋 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Visual Studio 2022 (latest version) or Rider
- Git

### Clone the repository
```bash
git clone https://github.com/stefchab06/TileTextureGenerator.git
cd TileTextureGenerator
```

### Build the solution
```bash
dotnet build
```

### Run tests
```bash
dotnet test
```

### Test coverage (optional)
```bash
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

---

## 📚 Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Detailed architecture documentation
- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - GitHub Copilot instructions

---

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. **Code Language**: All code, comments, and documentation must be in **English**
2. **Discussions**: Team discussions may be in French (native language), but all code artifacts in English
3. **Architecture**: Follow the hexagonal architecture pattern
4. **Testing**: Write tests for all new features
5. **Conventions**: Follow existing C# 14 and .NET 10 conventions

### Adding a new feature
1. Read [ARCHITECTURE.md](ARCHITECTURE.md) first
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Implement changes (Core-first approach)
4. Add tests
5. Ensure all tests pass: `dotnet test`
6. Commit and push: `git push origin feature/my-feature`
7. Open a Pull Request

---

## 🧪 Testing Strategy

- **Unit tests** for Core domain logic (xUnit)
- **Integration tests** for adapters and infrastructure
- **Test naming**: `WhenConditionThenOutcome` pattern
- **Structure**: Arrange-Act-Assert (AAA)
- **Coverage target**: 80%+ for Core

---

## 📦 Project Structure

This solution uses a modular approach with clear separation of concerns:

- **Core**: Business logic (no external dependencies except .NET BCL)
- **Adapters**: Connect Core to external systems (Persistence, UI, etc.)
- **Infrastructure**: Technical concerns (file system, networking, etc.)
- **Tests**: xUnit-based unit and integration tests

---

## 🗺️ Roadmap

### Phase 1: Core Domain ✅
- [x] Define entities (ProjectBase, TransformationBase)
- [x] Implement concrete types (FloorTileProject, WallTileProject)
- [x] Create polymorphic registries
- [x] Define ports and DTOs

### Phase 2: Persistence 🚧 (Current)
- [ ] Implement JSON serialization/deserialization
- [ ] File system-based storage
- [ ] Integration tests for persistence layer

### Phase 3: UI 📋
- [ ] Multi-platform UI with .NET MAUI
- [ ] Project management interface
- [ ] Transformation configuration screens
- [ ] Image preview and export

### Phase 4: Image Generation 📋
- [ ] Texture processing pipeline
- [ ] Edge flap rendering
- [ ] High-quality output (300 DPI for printing)
- [ ] Export to PDF/PNG

---

## 🔗 Related Resources

- **Tales on Tiles**: https://www.tales-on-tiles.com/
- **3D Printable Tiles**: Compatible physical tiles for board games and RPGs
- **.NET 10 Documentation**: https://learn.microsoft.com/dotnet/
- **Hexagonal Architecture**: https://alistair.cockburn.us/hexagonal-architecture/

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Authors

- **StefChab06** - *Initial work* - [GitHub](https://github.com/stefchab06)

---

## 🙏 Acknowledgments

- **Tales on Tiles** community for the inspiration
- Contributors and testers (growing list)
- Open-source .NET community

---

**Note**: This project is under active development. The persistence layer is currently being implemented. UI and image generation features are planned for future releases.
