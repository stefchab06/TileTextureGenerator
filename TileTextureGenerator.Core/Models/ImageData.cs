namespace TileTextureGenerator.Core.Models;

/// <summary>
/// Represents image data in PNG format.
/// Immutable value type wrapping raw byte array for type safety.
/// </summary>
public readonly record struct ImageData
{
    /// <summary>
    /// Raw PNG image bytes.
    /// </summary>
    public byte[] Bytes { get; init; }

    /// <summary>
    /// Creates a new ImageData instance from raw bytes.
    /// </summary>
    /// <param name="bytes">PNG image bytes.</param>
    /// <exception cref="ArgumentNullException">If bytes is null.</exception>
    /// <exception cref="ArgumentException">If bytes is empty.</exception>
    public ImageData(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        
        if (bytes.Length == 0)
            throw new ArgumentException("Image data cannot be empty.", nameof(bytes));
        
        Bytes = bytes;
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
