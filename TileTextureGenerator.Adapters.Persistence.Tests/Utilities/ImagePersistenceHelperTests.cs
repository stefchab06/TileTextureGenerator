using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using Xunit;

namespace TileTextureGenerator.Adapters.Persistence.Tests.Utilities;

/// <summary>
/// Tests for image persistence helper.
/// </summary>
public class ImagePersistenceHelperTests
{
    private readonly InMemoryFileStorage _fileStorage;
    private readonly ImagePersistenceHelper _helper;
    private readonly string _baseDirectory;

    public ImagePersistenceHelperTests()
    {
        _fileStorage = new InMemoryFileStorage();
        _helper = new ImagePersistenceHelper(_fileStorage);
        _baseDirectory = Path.Combine(_fileStorage.GetApplicationDataPath(), "TestProject");
    }

    [Fact]
    public async Task SaveImageAsync_WithExplicitFileName_SavesWithProvidedName()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        string fileName = "TestImage.png";

        // Act
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Sources", fileName);

        // Assert
        Assert.Equal("Sources/TestImage.png", jsonPath);
        string fullPath = Path.Combine(_baseDirectory, "Sources", fileName);
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
        byte[] saved = await _fileStorage.ReadAllBytesAsync(fullPath);
        Assert.Equal(imageData, saved);
    }

    [Fact]
    public async Task SaveImageAsync_WithoutFileName_GeneratesGuidName()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];

        // Act
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace");

        // Assert
        Assert.StartsWith("Workspace/", jsonPath);
        Assert.EndsWith(".png", jsonPath);
        Assert.Contains("-", jsonPath); // GUID format

        string platformPath = PathHelper.ToPlatformPath(jsonPath);
        string fullPath = Path.Combine(_baseDirectory, platformPath);
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
    }

    [Fact]
    public async Task SaveImageAsync_WithExistingPath_ReusesFileName()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        string existingPath = "Workspace/abc-123.png";

        // Act
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace", existingPath: existingPath);

        // Assert
        Assert.Equal("Workspace/abc-123.png", jsonPath);
        string fullPath = Path.Combine(_baseDirectory, "Workspace", "abc-123.png");
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
    }

    [Fact]
    public async Task SaveImageAsync_CreatesSubdirectory()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];

        // Act
        await _helper.SaveImageAsync(imageData, _baseDirectory, "Outputs", "result.png");

        // Assert
        string outputsDir = Path.Combine(_baseDirectory, "Outputs");
        Assert.True(await _fileStorage.DirectoryExistsAsync(outputsDir));
    }

    [Fact]
    public async Task SaveImageAsync_ReturnsJsonPathWithForwardSlashes()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];

        // Act
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Sources", "test.png");

        // Assert
        Assert.Contains('/', jsonPath);
        Assert.DoesNotContain('\\', jsonPath);
    }

    [Fact]
    public async Task SaveImageAsync_WithNullImageData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.SaveImageAsync(null!, _baseDirectory, "Sources", "test.png"));
    }

    [Fact]
    public async Task SaveImageAsync_WithEmptyImageData_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.SaveImageAsync(Array.Empty<byte>(), _baseDirectory, "Sources", "test.png"));
    }

    [Fact]
    public async Task LoadImageAsync_WithExistingImage_ReturnsImageData()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47, 0x01, 0x02];
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Sources", "test.png");

        // Act
        byte[]? loaded = await _helper.LoadImageAsync(jsonPath, _baseDirectory);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(imageData, loaded);
    }

    [Fact]
    public async Task LoadImageAsync_WithNonExistentImage_ReturnsNull()
    {
        // Act
        byte[]? loaded = await _helper.LoadImageAsync("Sources/nonexistent.png", _baseDirectory);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadImageAsync_WithNullPath_ReturnsNull()
    {
        // Act
        byte[]? loaded = await _helper.LoadImageAsync(null, _baseDirectory);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadImageAsync_WithEmptyPath_ReturnsNull()
    {
        // Act
        byte[]? loaded = await _helper.LoadImageAsync(string.Empty, _baseDirectory);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SavePropertyImageAsync_UsesPropertyNameAsFileName()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];

        // Act
        string jsonPath = await _helper.SavePropertyImageAsync(imageData, _baseDirectory, "Sources", "DisplayImage");

        // Assert
        Assert.Equal("Sources/DisplayImage.png", jsonPath);
        string fullPath = Path.Combine(_baseDirectory, "Sources", "DisplayImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
    }

    [Fact]
    public async Task SavePropertyImageAsync_WithNullPropertyName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _helper.SavePropertyImageAsync([0x89], _baseDirectory, "Sources", null!));
    }

    [Fact]
    public async Task SavePropertyImageAsync_WithEmptyPropertyName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.SavePropertyImageAsync([0x89], _baseDirectory, "Sources", "   "));
    }

    [Fact]
    public async Task DeleteImageAsync_RemovesFile()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Sources", "test.png");
        string fullPath = Path.Combine(_baseDirectory, PathHelper.ToPlatformPath(jsonPath));
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));

        // Act
        await _helper.DeleteImageAsync(jsonPath, _baseDirectory);

        // Assert
        Assert.False(await _fileStorage.FileExistsAsync(fullPath));
    }

    [Fact]
    public async Task DeleteImageAsync_WithNullPath_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _helper.DeleteImageAsync(null, _baseDirectory);
    }

    [Fact]
    public async Task DeleteImageAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Act & Assert (should not throw)
        await _helper.DeleteImageAsync("Sources/nonexistent.png", _baseDirectory);
    }

    [Fact]
    public async Task ImageExistsAsync_WithExistingImage_ReturnsTrue()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];
        string jsonPath = await _helper.SaveImageAsync(imageData, _baseDirectory, "Sources", "test.png");

        // Act
        bool exists = await _helper.ImageExistsAsync(jsonPath, _baseDirectory);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ImageExistsAsync_WithNonExistentImage_ReturnsFalse()
    {
        // Act
        bool exists = await _helper.ImageExistsAsync("Sources/nonexistent.png", _baseDirectory);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ImageExistsAsync_WithNullPath_ReturnsFalse()
    {
        // Act
        bool exists = await _helper.ImageExistsAsync(null, _baseDirectory);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task SaveImageAsync_MultipleCalls_GeneratesDifferentGuids()
    {
        // Arrange
        byte[] imageData = [0x89, 0x50, 0x4E, 0x47];

        // Act
        string path1 = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace");
        string path2 = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace");

        // Assert
        Assert.NotEqual(path1, path2);
    }

    [Fact]
    public async Task SaveLoadRoundTrip_PreservesImageData()
    {
        // Arrange
        byte[] originalData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        // Act
        string jsonPath = await _helper.SaveImageAsync(originalData, _baseDirectory, "Sources", "test.png");
        byte[]? loadedData = await _helper.LoadImageAsync(jsonPath, _baseDirectory);

        // Assert
        Assert.NotNull(loadedData);
        Assert.Equal(originalData, loadedData);
    }
}
