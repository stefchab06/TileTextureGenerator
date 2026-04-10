using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Core.Models;

/// <summary>
/// Represents image data in PNG format.
/// Immutable value type wrapping raw byte array for type safety.
/// Automatically converts non-PNG images to PNG in constructor (business rule).
/// </summary>
public readonly record struct ImageData
{
    /// <summary>
    /// Raw PNG image bytes.
    /// </summary>
    public byte[] Bytes { get; init; }

    /// <summary>
    /// Creates a new ImageData instance from raw bytes.
    /// Automatically converts to PNG if not already in PNG format (business rule).
    /// </summary>
    /// <param name="bytes">Image bytes (any format, will be converted to PNG).</param>
    /// <exception cref="ArgumentNullException">If bytes is null.</exception>
    /// <exception cref="ArgumentException">If bytes is empty.</exception>
    public ImageData(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(bytes));

        // Business rule: All images in Core MUST be PNG
        Bytes = ImageProcessor.IsPng(bytes) 
            ? bytes 
            : ImageProcessor.ConvertToPng(bytes);
    }

    /// <summary>
    /// Implicit conversion from ImageData to byte array for compatibility.
    /// </summary>
    public static implicit operator byte[](ImageData image) => image.Bytes;

    /// <summary>
    /// Implicit conversion from byte array to ImageData for convenience.
    /// Returns default ImageData if bytes is null.
    /// </summary>
    public static implicit operator ImageData(byte[]? bytes) 
    {
        if (bytes == null || bytes.Length == 0)
            return default;

        return new ImageData(bytes);
    }
}
