using TileTextureGenerator.Core.Ports.Input;

namespace TileTextureGenerator.Frontend.UI.Services;

/// <summary>
/// MAUI implementation of image selection service
/// </summary>
public class ImageSelectionService : IImageSelectionService
{
    public async Task<byte[]?> PickImageFromFileAsync()
    {
        try
        {
            // Get the Pictures folder path (cross-platform)
            var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select an image",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null)
                return null;

            // Read the file into a byte array
            using var stream = await result.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"Error picking image: {ex.Message}");
#endif
            return null;
        }
    }
}
