using System.Text.Json;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Utilities;

/// <summary>
/// Tests for JSON path manipulation helper.
/// </summary>
public class JsonPathHelperTests
{
    [Fact]
    public void SetValueAtPath_WithSimpleProperty_SetsValue()
    {
        // Arrange
        string json = """{"name": "OldName", "status": "New"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "name", "NewName");

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("NewName", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("New", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void SetValueAtPath_WithNestedProperty_SetsValue()
    {
        // Arrange
        string json = """{"project": {"name": "Test", "version": 1}}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "project.version", 2);

        // Assert
        var doc = JsonDocument.Parse(result);
        var project = doc.RootElement.GetProperty("project");
        Assert.Equal("Test", project.GetProperty("name").GetString());
        Assert.Equal(2, project.GetProperty("version").GetInt32());
    }

    [Fact]
    public void SetValueAtPath_WithNonExistentProperty_CreatesProperty()
    {
        // Arrange
        string json = """{"name": "Test"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "newProperty", "NewValue");

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Test", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("NewValue", doc.RootElement.GetProperty("newProperty").GetString());
    }

    [Fact]
    public void SetValueAtPath_WithNullValue_SetsNull()
    {
        // Arrange
        string json = """{"name": "Test", "image": "path.png"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "image", null);

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("image").ValueKind);
    }

    [Fact]
    public void SetValueAtPath_WithStringValue_SetsString()
    {
        // Arrange
        string json = """{"displayImage": "old.png"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "displayImage", "Sources/DisplayImage.png");

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("Sources/DisplayImage.png", doc.RootElement.GetProperty("displayImage").GetString());
    }

    [Fact]
    public void SetValueAtPath_CaseInsensitive_FindsProperty()
    {
        // Arrange
        string json = """{"DisplayImage": "old.png"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "displayImage", "new.png");

        // Assert
        var doc = JsonDocument.Parse(result);
        // Original casing should be preserved
        Assert.Equal("new.png", doc.RootElement.GetProperty("DisplayImage").GetString());
    }

    [Fact]
    public void GetValueAtPath_WithSimpleProperty_ReturnsValue()
    {
        // Arrange
        string json = """{"name": "TestProject", "status": "New"}""";

        // Act
        string? value = JsonPathHelper.GetValueAtPath(json, "name");

        // Assert
        Assert.NotNull(value);
        Assert.Equal("TestProject", value);
    }

    [Fact]
    public void GetValueAtPath_WithNestedProperty_ReturnsValue()
    {
        // Arrange
        string json = """{"project": {"name": "Test", "version": 1}}""";

        // Act
        string? value = JsonPathHelper.GetValueAtPath(json, "project.name");

        // Assert
        Assert.NotNull(value);
        Assert.Equal("Test", value);
    }

    [Fact]
    public void GetValueAtPath_WithNonExistentProperty_ReturnsNull()
    {
        // Arrange
        string json = """{"name": "Test"}""";

        // Act
        string? value = JsonPathHelper.GetValueAtPath(json, "nonExistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetValueAtPath_WithObjectIndexer_ReturnsValue()
    {
        // Arrange
        string json = """{"transformations": {"abc-123": {"type": "HorizontalFloor"}}}""";

        // Act
        string? value = JsonPathHelper.GetValueAtPath(json, "transformations[abc-123].type");

        // Assert
        Assert.NotNull(value);
        Assert.Equal("HorizontalFloor", value);
    }

    [Fact]
    public void SetValueAtPath_WithNullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            JsonPathHelper.SetValueAtPath(null!, "path", "value"));
    }

    [Fact]
    public void SetValueAtPath_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            JsonPathHelper.SetValueAtPath("{}", null!, "value"));
    }

    [Fact]
    public void SetValueAtPath_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            JsonPathHelper.SetValueAtPath("{}", "   ", "value"));
    }

    [Fact]
    public void GetValueAtPath_WithNullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            JsonPathHelper.GetValueAtPath(null!, "path"));
    }

    [Fact]
    public void GetValueAtPath_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            JsonPathHelper.GetValueAtPath("{}", null!));
    }

    [Fact]
    public void GetValueAtPath_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            JsonPathHelper.GetValueAtPath("{}", "   "));
    }

    [Fact]
    public void SetGetRoundTrip_PreservesValue()
    {
        // Arrange
        string json = """{"name": "Test"}""";
        string newValue = "Sources/DisplayImage.png";

        // Act
        string modified = JsonPathHelper.SetValueAtPath(json, "displayImage", newValue);
        string? retrieved = JsonPathHelper.GetValueAtPath(modified, "displayImage");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(newValue, retrieved);
    }

    [Fact]
    public void SetValueAtPath_PreservesFormatting()
    {
        // Arrange
        string json = """{"name": "Test"}""";

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "image", "test.png");

        // Assert
        Assert.Contains("\n", result); // Should be indented
    }

    [Fact]
    public void SetValueAtPath_WithDeepNesting_SetsCorrectly()
    {
        // Arrange
        string json = """
        {
          "transformations": {
            "abc-123": {
              "type": "Floor",
              "icon": null
            }
          }
        }
        """;

        // Act
        string result = JsonPathHelper.SetValueAtPath(json, "transformations.abc-123.icon", "Workspace/abc-123.png");

        // Assert
        string? iconValue = JsonPathHelper.GetValueAtPath(result, "transformations.abc-123.icon");
        Assert.NotNull(iconValue);
        Assert.Equal("Workspace/abc-123.png", iconValue);

        // Ensure other properties preserved
        string? typeValue = JsonPathHelper.GetValueAtPath(result, "transformations.abc-123.type");
        Assert.NotNull(typeValue);
        Assert.Equal("Floor", typeValue);
    }
}
