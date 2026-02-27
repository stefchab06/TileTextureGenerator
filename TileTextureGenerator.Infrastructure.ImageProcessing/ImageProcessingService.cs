using System.Drawing;
using System.Drawing.Imaging;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Infrastructure.ImageProcessing;

/// <summary>
/// Image processing implementation using System.Drawing
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    public byte[] ConvertToPng(byte[] imageData)
    {
        using var inputStream = new MemoryStream(imageData);
        using var image = Image.FromStream(inputStream);
        
        using var outputStream = new MemoryStream();
        image.Save(outputStream, ImageFormat.Png);
        
        return outputStream.ToArray();
    }

    public byte[] ConvertToPng(byte[] imageData, int width, int height)
    {
        using var inputStream = new MemoryStream(imageData);
        using var originalImage = Image.FromStream(inputStream);
        
        // Create resized image
        using var resizedImage = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(resizedImage))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(originalImage, 0, 0, width, height);
        }
        
        using var outputStream = new MemoryStream();
        resizedImage.Save(outputStream, ImageFormat.Png);
        
        return outputStream.ToArray();
    }
}
