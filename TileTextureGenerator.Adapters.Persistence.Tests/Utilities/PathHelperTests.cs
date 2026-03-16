using TileTextureGenerator.Adapters.Persistence.Utilities;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Utilities;

/// <summary>
/// Tests for cross-platform path conversion utilities.
/// </summary>
public class PathHelperTests
{
    [Fact]
    public void ToJsonPath_WithBackslashes_ReturnsForwardSlashes()
    {
        // Arrange
        string windowsPath = "Workspace\\Projects\\MyProject\\source.png";

        // Act
        string result = PathHelper.ToJsonPath(windowsPath);

        // Assert
        Assert.Equal("Workspace/Projects/MyProject/source.png", result);
    }

    [Fact]
    public void ToJsonPath_WithForwardSlashes_ReturnsUnchanged()
    {
        // Arrange
        string unixPath = "Workspace/Projects/MyProject/source.png";

        // Act
        string result = PathHelper.ToJsonPath(unixPath);

        // Assert
        Assert.Equal("Workspace/Projects/MyProject/source.png", result);
    }

    [Fact]
    public void ToJsonPath_WithNullInput_ReturnsEmptyString()
    {
        // Act
        string result = PathHelper.ToJsonPath(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJsonPath_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        string result = PathHelper.ToJsonPath(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJsonPath_WithMixedSeparators_ReplacesAllBackslashes()
    {
        // Arrange
        string mixedPath = "Workspace\\Projects/MyProject\\source.png";

        // Act
        string result = PathHelper.ToJsonPath(mixedPath);

        // Assert
        Assert.Equal("Workspace/Projects/MyProject/source.png", result);
    }

    [Fact]
    public void ToPlatformPath_WithForwardSlashes_ReturnsCorrectSeparator()
    {
        // Arrange
        string jsonPath = "Workspace/Projects/MyProject/source.png";
        char expectedSeparator = Path.DirectorySeparatorChar;

        // Act
        string result = PathHelper.ToPlatformPath(jsonPath);

        // Assert
        Assert.Contains(expectedSeparator, result);
        Assert.Equal($"Workspace{expectedSeparator}Projects{expectedSeparator}MyProject{expectedSeparator}source.png", result);
    }

    [Fact]
    public void ToPlatformPath_WithNullInput_ReturnsEmptyString()
    {
        // Act
        string result = PathHelper.ToPlatformPath(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToPlatformPath_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        string result = PathHelper.ToPlatformPath(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToJsonPath_RoundTrip_PreservesPath()
    {
        // Arrange
        string originalPlatformPath = Path.Combine("Workspace", "Projects", "MyProject", "source.png");

        // Act
        string jsonPath = PathHelper.ToJsonPath(originalPlatformPath);
        string backToPlatformPath = PathHelper.ToPlatformPath(jsonPath);

        // Assert
        Assert.Equal(originalPlatformPath, backToPlatformPath);
    }

    [Fact]
    public void ToPlatformPath_RoundTrip_PreservesPath()
    {
        // Arrange
        string originalJsonPath = "Workspace/Projects/MyProject/source.png";

        // Act
        string platformPath = PathHelper.ToPlatformPath(originalJsonPath);
        string backToJsonPath = PathHelper.ToJsonPath(platformPath);

        // Assert
        Assert.Equal(originalJsonPath, backToJsonPath);
    }

    [Fact]
    public void ToJsonPaths_WithMultiplePaths_ConvertsAll()
    {
        // Arrange
        var platformPaths = new[]
        {
            "Workspace\\Projects\\Project1\\file1.png",
            "Workspace\\Projects\\Project2\\file2.png",
            "Data\\Images\\texture.jpg"
        };

        // Act
        var result = PathHelper.ToJsonPaths(platformPaths).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Workspace/Projects/Project1/file1.png", result[0]);
        Assert.Equal("Workspace/Projects/Project2/file2.png", result[1]);
        Assert.Equal("Data/Images/texture.jpg", result[2]);
    }

    [Fact]
    public void ToJsonPaths_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.ToJsonPaths(null!).ToList());
    }

    [Fact]
    public void ToJsonPaths_WithEmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyCollection = Array.Empty<string>();

        // Act
        var result = PathHelper.ToJsonPaths(emptyCollection).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToPlatformPaths_WithMultiplePaths_ConvertsAll()
    {
        // Arrange
        var jsonPaths = new[]
        {
            "Workspace/Projects/Project1/file1.png",
            "Workspace/Projects/Project2/file2.png",
            "Data/Images/texture.jpg"
        };
        char sep = Path.DirectorySeparatorChar;

        // Act
        var result = PathHelper.ToPlatformPaths(jsonPaths).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal($"Workspace{sep}Projects{sep}Project1{sep}file1.png", result[0]);
        Assert.Equal($"Workspace{sep}Projects{sep}Project2{sep}file2.png", result[1]);
        Assert.Equal($"Data{sep}Images{sep}texture.jpg", result[2]);
    }

    [Fact]
    public void ToPlatformPaths_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PathHelper.ToPlatformPaths(null!).ToList());
    }

    [Fact]
    public void ToPlatformPaths_WithEmptyCollection_ReturnsEmptyCollection()
    {
        // Arrange
        var emptyCollection = Array.Empty<string>();

        // Act
        var result = PathHelper.ToPlatformPaths(emptyCollection).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ToPlatformPath_PreservesSeparatorCount()
    {
        // Arrange
        string jsonPath = "Workspace/Projects/MyProject/source.png";
        int expectedSeparatorCount = 3; // Three forward slashes in input

        // Act
        string result = PathHelper.ToPlatformPath(jsonPath);
        int actualSeparatorCount = result.Count(c => c == Path.DirectorySeparatorChar);

        // Assert
        Assert.Equal(expectedSeparatorCount, actualSeparatorCount);
    }

    [Fact]
    public void ToPlatformPath_RemovesForwardSlashesWhenNotPlatformSeparator()
    {
        // Arrange
        string jsonPath = "Workspace/Projects/MyProject/source.png";

        // Act
        string result = PathHelper.ToPlatformPath(jsonPath);

        // Assert
        // If platform separator is NOT forward slash (Windows), result should not contain any
        if (Path.DirectorySeparatorChar != '/')
        {
            Assert.DoesNotContain('/', result);
        }
        // If platform separator IS forward slash (Unix), they should remain
        else
        {
            Assert.Contains('/', result);
        }
    }

    [Fact]
    public void ToJsonPath_AlwaysProducesForwardSlashes()
    {
        // Arrange
        string platformPath = Path.Combine("Workspace", "Projects", "MyProject", "source.png");

        // Act
        string result = PathHelper.ToJsonPath(platformPath);

        // Assert
        // Result must contain forward slashes (JSON standard)
        Assert.Contains('/', result);
        // Result must NOT contain backslashes (even if platform uses them)
        Assert.DoesNotContain('\\', result);
    }
}
