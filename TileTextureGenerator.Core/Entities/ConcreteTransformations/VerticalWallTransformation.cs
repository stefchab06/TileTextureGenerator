using SkiaSharp;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Helpers;
using TileTextureGenerator.Core.Models;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Registries;

namespace TileTextureGenerator.Core.Entities.ConcreteTransformations;

/// <summary>
/// Vertical wall tile transformation that creates a completely flat tile texture.
/// Adds rectangular flaps on all four sides (0.25" height) to be folded.
/// The visible surface is oriented for vertical wall mounting.
/// </summary>
public sealed class VerticalWallTransformation : TransformationBase
{
    private const double FlapHeightInInches = 0.25;
    private const double MaxTileDimensionInInches = 2.0;
    private const float BlankBorderWidth = 2.0f;

    static VerticalWallTransformation()
    {
        // TODO: Create TransformationRegistry similar to ProjectRegistry
        // TransformationRegistry.RegisterType<VerticalWallTransformation>();
    }

    /// <summary>
    /// Base texture image for the tile surface.
    /// Copied from project's SourceImage when transformation is added.
    /// </summary>
    public byte[]? BaseTexture { get; set; }

    /// <summary>
    /// Shape of the tile defining its dimensions.
    /// </summary>
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// Icon for this transformation type (PNG, 64x64).
    /// Generated programmatically showing a vertical wall tile perspective.
    /// </summary>
    public override byte[]? Icon => TransformationIconGenerator.GenerateVerticalWallIcon();

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    public VerticalWallTransformation(ITransformationStore<TransformationBase> store) : base(store)
    {
    }

    /// <inheritdoc />
    public override async Task<byte[]> ExecuteAsync()
    {
        // Validate BaseTexture
        if (BaseTexture == null || BaseTexture.Length == 0)
            throw new InvalidOperationException("BaseTexture is required for transformation execution.");

        // FULL IMPLEMENTATION with SkiaSharp
        using var baseStream = new MemoryStream(BaseTexture);
        using var baseTexture = SKBitmap.Decode(baseStream);
        
        if (baseTexture == null)
            throw new InvalidOperationException("Failed to decode base texture.");

        // Calculate DPI and flap dimensions
        var maxSide = Math.Max(baseTexture.Width, baseTexture.Height);
        var dpi = maxSide / MaxTileDimensionInInches;
        var flapPixels = (int)(FlapHeightInInches * dpi);

        // Create canvas with flaps on all sides
        var canvasWidth = baseTexture.Width + 2 * flapPixels;
        var canvasHeight = baseTexture.Height + 2 * flapPixels;

        using var canvas = new SKBitmap(canvasWidth, canvasHeight);
        using var skCanvas = new SKCanvas(canvas);

        skCanvas.Clear(SKColors.Transparent);

        // Draw base texture in center
        skCanvas.DrawBitmap(baseTexture, flapPixels, flapPixels);

        // Draw flaps for all four sides
        DrawFlap(skCanvas, ImageSide.Top, baseTexture, flapPixels, canvasWidth, canvasHeight);
        DrawFlap(skCanvas, ImageSide.Right, baseTexture, flapPixels, canvasWidth, canvasHeight);
        DrawFlap(skCanvas, ImageSide.Bottom, baseTexture, flapPixels, canvasWidth, canvasHeight);
        DrawFlap(skCanvas, ImageSide.Left, baseTexture, flapPixels, canvasWidth, canvasHeight);

        // Encode to PNG
        using var image = SKImage.FromBitmap(canvas);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        
        await Task.CompletedTask;
        return encoded.ToArray();
    }

    private void DrawFlap(
        SKCanvas canvas,
        ImageSide side,
        SKBitmap baseTexture,
        int flapPixels,
        int canvasWidth,
        int canvasHeight)
    {
        var config = this[side];

        if (config.Mode == EdgeFlapMode.None)
            return;

        // Calculate flap rectangle (excluding corners)
        SKRect flapRect = side switch
        {
            ImageSide.Top => new SKRect(flapPixels, 0, canvasWidth - flapPixels, flapPixels),
            ImageSide.Bottom => new SKRect(flapPixels, canvasHeight - flapPixels, canvasWidth - flapPixels, canvasHeight),
            ImageSide.Right => new SKRect(canvasWidth - flapPixels, flapPixels, canvasWidth, canvasHeight - flapPixels),
            ImageSide.Left => new SKRect(0, flapPixels, flapPixels, canvasHeight - flapPixels),
            _ => SKRect.Empty
        };

        switch (config.Mode)
        {
            case EdgeFlapMode.Blank:
                DrawBlankFlap(canvas, flapRect);
                break;

            case EdgeFlapMode.Color:
                DrawColorFlap(canvas, flapRect, config.Color);
                break;

            case EdgeFlapMode.Texture:
                DrawTextureFlap(canvas, flapRect, config.TextureImage);
                break;

            case EdgeFlapMode.Symmetric:
                DrawSymmetricFlap(canvas, flapRect, baseTexture, side, flapPixels);
                break;
        }
    }

    private void DrawBlankFlap(SKCanvas canvas, SKRect rect)
    {
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = BlankBorderWidth,
            IsAntialias = true
        };

        canvas.DrawRect(rect, borderPaint);
    }

    private void DrawColorFlap(SKCanvas canvas, SKRect rect, string? hexColor)
    {
        var color = ParseHexColor(hexColor ?? "#808080");

        using var paint = new SKPaint
        {
            Color = color,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        canvas.DrawRect(rect, paint);
    }

    private void DrawTextureFlap(SKCanvas canvas, SKRect rect, byte[]? textureImage)
    {
        if (textureImage == null || textureImage.Length == 0)
        {
            DrawBlankFlap(canvas, rect);
            return;
        }

        using var stream = new MemoryStream(textureImage);
        using var bitmap = SKBitmap.Decode(stream);

        if (bitmap == null)
        {
            DrawBlankFlap(canvas, rect);
            return;
        }

        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(bitmap, rect, paint);
    }

    private void DrawSymmetricFlap(
        SKCanvas canvas,
        SKRect flapRect,
        SKBitmap baseTexture,
        ImageSide side,
        int flapPixels)
    {
        // Extract the edge strip from base texture and flip it
        SKRect sourceRect;
        bool flipHorizontal = false;
        bool flipVertical = false;

        switch (side)
        {
            case ImageSide.Top:
                sourceRect = new SKRect(0, 0, baseTexture.Width, flapPixels);
                flipHorizontal = true;
                break;

            case ImageSide.Bottom:
                sourceRect = new SKRect(0, baseTexture.Height - flapPixels, baseTexture.Width, baseTexture.Height);
                flipHorizontal = true;
                break;

            case ImageSide.Right:
                sourceRect = new SKRect(baseTexture.Width - flapPixels, 0, baseTexture.Width, baseTexture.Height);
                flipVertical = true;
                break;

            case ImageSide.Left:
                sourceRect = new SKRect(0, 0, flapPixels, baseTexture.Height);
                flipVertical = true;
                break;

            default:
                return;
        }

        // Extract the strip
        using var strip = new SKBitmap((int)sourceRect.Width, (int)sourceRect.Height);
        using var stripCanvas = new SKCanvas(strip);
        stripCanvas.DrawBitmap(baseTexture, sourceRect, new SKRect(0, 0, sourceRect.Width, sourceRect.Height));

        // Apply flip transformation
        canvas.Save();

        if (flipHorizontal)
        {
            canvas.Translate(flapRect.Left, flapRect.Bottom);
            canvas.Scale(1, -1);
            canvas.DrawBitmap(strip, 0, 0);
        }
        else if (flipVertical)
        {
            canvas.Translate(flapRect.Right, flapRect.Top);
            canvas.Scale(-1, 1);
            canvas.DrawBitmap(strip, 0, 0);
        }

        canvas.Restore();
    }

    private SKColor ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new SKColor(r, g, b);
        }

        return SKColors.Gray;
    }
}
