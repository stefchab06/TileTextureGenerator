# Image Cropping Control - Extraction Strategy

## Overview

This document describes the strategy for extracting the **Image Cropping** functionality into a reusable **NuGet package** for the MAUI community.

**Current Status**: Developing internally in TileTextureGenerator  
**Target**: Standalone NuGet package `Maui.ImageCropping` (or similar name)  
**Timeline**: Extract after Phase 3 (Pan/Zoom/Rotation functional)

---

## Extractable Components

### ✅ **Core Controls** (ZERO dependencies on TileTextureGenerator)
Located in: `Controls/ImageCropping/`

| File | Description | Dependencies |
|------|-------------|--------------|
| `CroppingCanvasControl.cs` | SkiaSharp rendering control | SkiaSharp only |
| `CroppingTransformation.cs` | Immutable transformation record | None |
| `CroppingMath.cs` | Math helpers (zoom, fit-to-fill) | None |
| `CroppingToolbar.cs` | Toolbar with mode buttons | MAUI only |

### ✅ **Tests** (extractable)
Located in: `TileTextureGenerator.Presentation.UI.Tests/Controls/ImageCropping/`

| File | Description |
|------|-------------|
| `CroppingMathTests.cs` | Tests for math calculations |
| `CroppingTransformationTests.cs` | Tests for transformation model |

### ⚠️ **App-Specific** (NOT extractable)
| File | Purpose | Why Not Extractable |
|------|---------|---------------------|
| `TileShapeHelper.cs` | Convert TileShape → polygon | App-specific enum |
| `ImageCroppingService.cs` | Navigation orchestration | Shell-specific |

---

## Architecture Guidelines

### **Namespace Isolation**
All reusable code MUST use:
```csharp
namespace Maui.ImageCropping; // Future NuGet namespace
// OR during development:
namespace TileTextureGenerator.Presentation.UI.Controls.ImageCropping;
```

### **Zero Coupling**
- ❌ NO references to `TileTextureGenerator.Core`
- ❌ NO references to app-specific enums/models
- ✅ ONLY: `Microsoft.Maui`, `SkiaSharp.Views.Maui`

### **Documentation Requirements**
Every public member MUST have:
- XML `<summary>` comment
- `<param>` for all parameters
- `<returns>` for return values
- Usage example in class-level comment

---

## Extraction Checklist (Future)

### **Phase 1: Preparation** (During Development)
- [x] Isolate code in `Controls/ImageCropping/` folder
- [ ] Ensure all code uses extractable namespace
- [ ] Add comprehensive XML comments
- [ ] Write unit tests for all math/logic
- [ ] Create README.md with usage examples

### **Phase 2: Project Creation** (When Functional)
- [ ] Create new project: `Maui.ImageCropping.csproj`
- [ ] Copy files from `Controls/ImageCropping/`
- [ ] Update namespaces to `Maui.ImageCropping`
- [ ] Add NuGet metadata (author, license, icon)

### **Phase 3: Testing**
- [ ] Copy tests to `Maui.ImageCropping.Tests`
- [ ] Verify all tests pass
- [ ] Add integration sample app

### **Phase 4: Publication**
- [ ] Create GitHub repository
- [ ] Push code + tests + sample
- [ ] Publish to NuGet.org
- [ ] Write blog post / documentation
- [ ] Share on MAUI Discord / forums

---

## API Design Principles

### **Keep It Simple**
```csharp
// Simple API for consumers
var control = new CroppingCanvasControl();
control.CroppingPolygon = myPolygon;
control.SetImage(imageBytes);
var croppedImage = await control.GetCroppedImageAsync();
```

### **Flexibility**
- Support any polygon (not just rectangles)
- Allow custom toolbar (or use built-in)
- Events for transformation changes

### **Cross-Platform**
- Works on Android, iOS, Windows, macOS
- Touch + mouse support
- Responsive layout

---

## Future NuGet Package Info

**Package Name**: `Maui.ImageCropping` (or `Maui.Controls.ImageCropper`)  
**Author**: StefChab (+ contributors)  
**License**: MIT  
**Target**: net10.0-android, net10.0-ios, net10.0-windows, net10.0-maccatalyst  
**Dependencies**:
- `Microsoft.Maui.Controls` >= 10.0.0
- `SkiaSharp.Views.Maui.Controls` >= 3.0.0

---

## Notes for Developers

**When adding new features**:
1. Ask: "Could another app use this?"
2. If YES → Add to `Controls/ImageCropping/`
3. If NO → Keep in app-specific code
4. Document thoroughly with XML comments
5. Write tests

**Goal**: Make extraction a 1-hour task, not 1-week refactoring!

---

**Last Updated**: 2025-01-XX (Initial draft during Phase 1 development)
