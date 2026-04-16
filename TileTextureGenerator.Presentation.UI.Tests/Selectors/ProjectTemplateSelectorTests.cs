using TileTextureGenerator.Core.Registries;
using TileTextureGenerator.Presentation.UI.Selectors;

namespace TileTextureGenerator.Presentation.UI.Test.Selectors;

/// <summary>
/// Tests integrity between Core Registry and UI Template Selector.
/// Verifies that every registered project type has a corresponding DataTemplate,
/// and vice versa.
/// 
/// IMPORTANT: These tests must NOT run in parallel with other Registry tests,
/// as some tests modify TextureProjectRegistry state.
/// </summary>
[Collection("RegistryIntegrity")]
public class ProjectTemplateSelectorTests
{
    public ProjectTemplateSelectorTests()
    {
        // Force registration of all project types in Core
        // (static constructors might not run automatically in test context)
        TextureProjectRegistry.ForceAutoRegistrationFromCore();
    }

    /// <summary>
    /// Verifies that all concrete project types registered in Core
    /// have a corresponding DataTemplate in the UI.
    /// 
    /// If this test fails, it means a developer added a new project type
    /// but forgot to register its template in ProjectTemplateSelector.
    /// 
    /// Expected to FAIL for WallTileProject (not yet implemented).
    /// </summary>
    [Fact]
    public void AllRegisteredProjectTypes_HaveCorrespondingTemplate()
    {
        // Arrange
        var selector = new ProjectTemplateSelector();

        // Get all project types registered in Core
        var registeredInCore = TextureProjectRegistry.GetRegisteredTypes()
            .OrderBy(name => name)
            .ToList();

        // Get all templates registered in UI
        var registeredInUI = selector.RegisteredProjectTypes
            .OrderBy(name => name)
            .ToList();

        // Find missing templates (types in Core but not in UI)
        var missingTemplates = registeredInCore.Except(registeredInUI).ToList();

        // Act & Assert
        Assert.True(
            missingTemplates.Count == 0,
            $"Project type(s) without registered template in TileTextureGenerator.Presentation.UI.Selectors.ProjectTemplateSelector: {string.Join(", ", missingTemplates)}"
        );
    }

    /// <summary>
    /// Verifies that all registered DataTemplates in the UI
    /// correspond to a concrete project type in Core.
    /// 
    /// If this test fails, it means a developer registered a template
    /// for a project type that doesn't exist or isn't registered in Core.
    /// 
    /// This is less common but can happen if:
    /// - Template was added before the Core type was created
    /// - Core type was deleted but template registration wasn't removed
    /// </summary>
    [Fact]
    public void AllRegisteredTemplates_HaveCorrespondingProjectType()
    {
        // Arrange
        var selector = new ProjectTemplateSelector();

        // Get all templates registered in UI
        var registeredInUI = selector.RegisteredProjectTypes
            .OrderBy(name => name)
            .ToList();

        // Get all project types registered in Core
        var registeredInCore = TextureProjectRegistry.GetRegisteredTypes()
            .OrderBy(name => name)
            .ToList();

        // Find orphaned templates (types in UI but not in Core)
        var orphanedTemplates = registeredInUI.Except(registeredInCore).ToList();

        // Act & Assert
        Assert.True(
            orphanedTemplates.Count == 0,
            $"Registered template(s) without corresponding project type in TileTextureGenerator.Core.Registries.TextureProjectRegistry: {string.Join(", ", orphanedTemplates)}"
        );
    }
}
