using SkiaSharp;
using TileTextureGenerator.Adapters.Persistence.Tests.Mocks;
using TileTextureGenerator.Adapters.Persistence.Utilities;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Tests.Common;
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
    public async Task SerializeProjectImageDataAsync_WithEmptyPropertyName_ThrowsArgumentException()
    {
        // Arrange
        var imageData = TestImageFactory.CreateDisplayImage();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _helper.SerializeProjectImageDataAsync("   ", imageData, _baseDirectory));
    }

    [Fact]
    public async Task DeleteImageAsync_RemovesFile()
    {
        // Arrange
        byte[] imageData = TestImageFactory.CreateDisplayImage();
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
        byte[] imageData = TestImageFactory.CreateDisplayImage();
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
        byte[] imageData = TestImageFactory.CreateDisplayImage();

        // Act
        string path1 = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace");
        string path2 = await _helper.SaveImageAsync(imageData, _baseDirectory, "Workspace");

        // Assert
        Assert.NotEqual(path1, path2);
    }


    #region SerializeProjectImageDataAsync Tests

    [Fact]
    public async Task SerializeProjectImageDataAsync_ValidImageData_ReturnsCorrectJsonProperty()
    {
        // Arrange
        var imageData = TestImageFactory.CreateDisplayImage();
        string propertyName = "DisplayImage";

        // Act
        var (jsonPropertyName, jsonPathValue) = await _helper.SerializeProjectImageDataAsync(
            propertyName, imageData, _baseDirectory);

        // Assert
        Assert.Equal("displayimagePath", jsonPropertyName); // Fully lowercase + Path
        Assert.Equal("Sources/DisplayImage.png", jsonPathValue);

        // Verify file was created
        string fullPath = Path.Combine(_baseDirectory, "Sources", "DisplayImage.png");
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
        byte[] savedBytes = await _fileStorage.ReadAllBytesAsync(fullPath);
        Assert.Equal(imageData.Bytes, savedBytes);
    }

    [Fact]
    public async Task SerializeProjectImageDataAsync_MixedCasePropertyName_FullyLowercase()
    {
        // Arrange
        var imageData = TestImageFactory.CreateDisplayImage();
        string propertyName = "SourceImageData";

        // Act
        var (jsonPropertyName, _) = await _helper.SerializeProjectImageDataAsync(
            propertyName, imageData, _baseDirectory);

        // Assert
        Assert.Equal("sourceimagedataPath", jsonPropertyName); // ALL lowercase + Path
    }

    [Fact]
    public async Task SerializeProjectImageDataAsync_NullPropertyName_ThrowsArgumentNullException()
    {
        // Arrange
        var imageData = TestImageFactory.CreateDisplayImage();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _helper.SerializeProjectImageDataAsync(null!, imageData, _baseDirectory));
    }

    #endregion

    #region SerializeTransformationImageDataAsync Tests

    [Fact]
    public async Task SerializeTransformationImageDataAsync_NewImage_GeneratesGuidFilename()
    {
        // Arrange
        byte[] imageBytes = TestImageFactory.CreateImageData();
        var imageData = new Core.Models.ImageData(imageBytes);
        string propertyName = "BaseTexture";
        var emptyNode = new System.Text.Json.Nodes.JsonObject();

        // Act
        var (jsonPropertyName, jsonPathValue) = await _helper.SerializeTransformationImageDataAsync(
            propertyName, imageData, _baseDirectory, emptyNode);

        // Assert
        Assert.Equal("basetexturePath", jsonPropertyName); // Fully lowercase + Path
        Assert.StartsWith("Workspace/", jsonPathValue);
        Assert.EndsWith(".png", jsonPathValue);

        // Verify it's a GUID-based filename
        string fileName = Path.GetFileNameWithoutExtension(jsonPathValue.Replace("Workspace/", ""));
        Assert.True(Guid.TryParse(fileName, out _), "Filename should be a valid GUID");

        // Verify file was created
        string platformPath = PathHelper.ToPlatformPath(jsonPathValue);
        string fullPath = Path.Combine(_baseDirectory, platformPath);
        Assert.True(await _fileStorage.FileExistsAsync(fullPath));
    }

    [Fact]
    public async Task SerializeTransformationImageDataAsync_ExistingPath_ReusesGuid()
    {
        // Arrange
        byte[] oldImageBytes = TestImageFactory.CreateImageData();
        byte[] newImageBytes = new ImageData(TestImageFactory.CreatePng(1, 1, SKColor.Parse("#00000000")));
        var newImageData = new Core.Models.ImageData(newImageBytes);
        string propertyName = "BaseTexture";

        // Create existing image with specific GUID
        var existingGuid = Guid.NewGuid();
        string existingPath = $"Workspace/{existingGuid}.png";
        string fullPath = Path.Combine(_baseDirectory, "Workspace", $"{existingGuid}.png");
        await _fileStorage.WriteAllBytesAsync(fullPath, oldImageBytes);

        // Create node with existing path
        var existingNode = new System.Text.Json.Nodes.JsonObject
        {
            ["basetexturePath"] = existingPath
        };

        // Act
        var (jsonPropertyName, jsonPathValue) = await _helper.SerializeTransformationImageDataAsync(
            propertyName, newImageData, _baseDirectory, existingNode);

        // Assert
        Assert.Equal("basetexturePath", jsonPropertyName);
        Assert.Equal(existingPath, jsonPathValue); // Same path = GUID reused

        // Verify image was updated with new data
        byte[] savedBytes = await _fileStorage.ReadAllBytesAsync(fullPath);
        Assert.Equal(newImageBytes, savedBytes);
    }

    [Fact]
    public async Task SerializeTransformationImageDataAsync_MultipleProperties_DifferentGuids()
    {
        // Arrange
        byte[] imageData1 = TestImageFactory.CreateImageData();
        byte[] imageData2 = new ImageData(TestImageFactory.CreatePng(1, 1, SKColor.Parse("#00000000")));
        var emptyNode = new System.Text.Json.Nodes.JsonObject();

        // Act
        var (_, path1) = await _helper.SerializeTransformationImageDataAsync(
            "BaseTexture", imageData1, _baseDirectory, emptyNode);
        var (_, path2) = await _helper.SerializeTransformationImageDataAsync(
            "OverlayTexture", imageData2, _baseDirectory, emptyNode);

        // Assert - Different GUIDs
        Assert.NotEqual(path1, path2);

        string guid1 = Path.GetFileNameWithoutExtension(path1.Replace("Workspace/", ""));
        string guid2 = Path.GetFileNameWithoutExtension(path2.Replace("Workspace/", ""));
        Assert.NotEqual(guid1, guid2);
    }

    #endregion

    #region DeserializeImageDataAsync Tests

    [Fact]
    public async Task DeserializeImageDataAsync_ValidPath_ReturnsImageData()
    {
        // Arrange
        byte[] imageBytes = TestImageFactory.CreateDisplayImage();
        string relativePath = "Sources/TestImage.png";
        string fullPath = Path.Combine(_baseDirectory, "Sources", "TestImage.png");
        await _fileStorage.WriteAllBytesAsync(fullPath, imageBytes);

        // Act
        var imageData = await _helper.DeserializeImageDataAsync(relativePath, _baseDirectory);

        // Assert
        Assert.NotNull(imageData);
        Assert.Equal(imageBytes, imageData.Value.Bytes);
    }

    [Fact]
    public async Task DeserializeImageDataAsync_NullPath_ReturnsNull()
    {
        // Act
        var imageData = await _helper.DeserializeImageDataAsync(null, _baseDirectory);

        // Assert
        Assert.Null(imageData);
    }

    [Fact]
    public async Task DeserializeImageDataAsync_EmptyPath_ReturnsNull()
    {
        // Act
        var imageData = await _helper.DeserializeImageDataAsync("", _baseDirectory);

        // Assert
        Assert.Null(imageData);
    }

    [Fact]
    public async Task DeserializeImageDataAsync_FileDoesNotExist_ReturnsNull()
    {
        // Act
        var imageData = await _helper.DeserializeImageDataAsync("NonExistent/Image.png", _baseDirectory);

        // Assert
        Assert.Null(imageData);
    }

    [Fact]
    public async Task DeserializeImageDataAsync_EmptyFile_ReturnsNull()
    {
        // Arrange
        string relativePath = "Sources/EmptyImage.png";
        string fullPath = Path.Combine(_baseDirectory, "Sources", "EmptyImage.png");
        await _fileStorage.WriteAllBytesAsync(fullPath, Array.Empty<byte>());

        // Act
        var imageData = await _helper.DeserializeImageDataAsync(relativePath, _baseDirectory);

        // Assert
        Assert.Null(imageData); // Empty bytes = null ImageData
    }

    #endregion
}
