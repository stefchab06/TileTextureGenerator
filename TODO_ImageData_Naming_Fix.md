# TODO: Fix ImageData Property Naming Bug

## ⚠️ Problem Identified
The ImageData property naming in JSON is inconsistent between save and load operations, causing the `LoadTransformationAsync_WithImageData_LoadsImageFromPath` test to fail.

## 🐛 Current Bug
**Property:** `BaseTexture`  
**Expected JSON:** `"basetexturePath"` (ALL lowercase + "Path")  
**Current Generate:** `"baseTexturePath"` (camelCase + "Path")

## 🔧 Files to Fix

### 1. JSonProjectStore.cs
- **Line 97:** `string pathPropertyName = $"{char.ToLowerInvariant(kvp.Key[0])}{kvp.Key.Substring(1)}Path";`
- **Line 472:** `string pathPropertyName = $"{char.ToLowerInvariant(imageProperty.Name[0])}{imageProperty.Name.Substring(1)}Path";`

### 2. JsonProjectsStore.cs  
- **Line 82:** `jsonDoc["displayImagePath"] = JsonSerializer.SerializeToElement(displayImagePath, JsonOptions);`
- **Line 239:** `string pathPropertyName = $"{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}Path";`

## ✅ Correct Implementation
```csharp
// Change from:
string pathPropertyName = $"{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}Path";

// To:
string pathPropertyName = $"{property.Name.ToLowerInvariant()}Path";
```

## 📋 Rule Reminder
**ImageData Properties:**
- `DisplayImage` → JSON: `"displayimagePath"` (ALL lowercase + Path)
- `BaseTexture` → JSON: `"basetexturePath"` (ALL lowercase + Path)  
- File: `Sources/PropertyName.png` (Projects) or `Workspace/{GUID}.png` (Transformations)

## 🎯 Expected Result
After fixing, the test `LoadTransformationAsync_WithImageData_LoadsImageFromPath` should pass because:
1. Test saves with `"basetexturePath"` in JSON
2. Load code will look for `"basetexturePath"` (matching!)
3. BaseTexture property will be populated correctly

## 📊 Current Status
- ✅ Basic property deserialization working (TileShape, ID preservation)
- ✅ RequiredPaperType correctly comes from concrete class (by design)
- ❌ ImageData loading broken due to naming mismatch
- ✅ 18/19 JSonProjectStore tests passing (1 fails due to this bug)

---
**Date:** 2024-12-19
**Next Session:** Fix the 4 lines above to use `ToLowerInvariant()` instead of just first character
