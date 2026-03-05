using SkiaSharp;
using TileTextureGenerator.Core.Attributes;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Transformations;

namespace TileTextureGenerator.Core.Transformations.Implementations;

/// <summary>
/// Flat horizontal transformation that creates a completely flat tile texture.
/// Adds rectangular flaps on all four sides (0.25" height) to be folded.
/// The visible top surface remains flat without inclination.
/// </summary>
[TransformationCardinality(MaxPerProject = 1)]
public class FlatHorizontalTransformation : TransformationBase
{
    // Auto-registration: triggers when the type is first loaded
    static FlatHorizontalTransformation()
    {
        Registries.TransformationTypeRegistry.Register<FlatHorizontalTransformation>();
    }

    /// <summary>
    /// Base texture image for the tile surface.
    /// This is typically the cropped/rotated source image from the project.
    /// Not serialized in JSON - managed separately by the persistence layer.
    /// </summary>
    public byte[]? BaseTexture { get; set; }

    /// <summary>
    /// Shape of the tile defining its dimensions.
    /// Determines MeridionalExtent and ZonalExtent values.
    /// </summary>
    [TransformationProperty]
    public TileShape TileShape { get; set; } = TileShape.Full;

    /// <summary>
    /// Height of the flaps (edges to be folded) in inches.
    /// Standard value is 0.25" (quarter inch) for horizontal tiles.
    /// </summary>
    public const double FlapHeightInInches = 0.25;

    public override string GetDisplayName()
    {
        // TODO: Localize this using resource strings
        var shape = TileShape switch
        {
            TileShape.Full => "2\"×2\"",
            TileShape.HalfHorizontal => "1\"×2\"",
            TileShape.HalfVertical => "2\"×1\"",
            _ => "2\"×2\""
        };
        return $"Flat Horizontal ({shape})";
    }

    public override string GetSafeFileName()
    {
        var shape = TileShape switch
        {
            TileShape.Full => "2x2",
            TileShape.HalfHorizontal => "1x2",
            TileShape.HalfVertical => "2x1",
            _ => "2x2"
        };
        return $"flat_horizontal_{shape}";
    }

    public override string GetIconResourcePath()
    {
        return "Resources/Images/Transformations/flat_horizontal.png";
    }

    public override async Task<TransformationResult> ExecuteAsync(
        ProjectContext context,
        CancellationToken ct = default)
    {
        var result = new TransformationResult();

        try
        {
            // 1. Load or use BaseTexture
            SKBitmap baseTexture;
            if (BaseTexture != null && BaseTexture.Length > 0)
            {
                using var stream = new MemoryStream(BaseTexture);
                baseTexture = SKBitmap.Decode(stream);
            }
            else if (context.SourceImage != null && context.SourceImage.Length > 0)
            {
                using var stream = new MemoryStream(context.SourceImage);
                baseTexture = SKBitmap.Decode(stream);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No base texture provided";
                return result;
            }

            if (baseTexture == null)
            {
                result.Success = false;
                result.ErrorMessage = "Failed to decode base texture";
                return result;
            }

            // 2. Calculate DPI from base texture
            var maxSide = Math.Max(baseTexture.Width, baseTexture.Height);
            var dpi = maxSide / MaxTileDimensionInInches; // Max side is always 2 inches
            var flapPixels = (int)(FlapHeightInInches * dpi);

            // 3. Create canvas with flaps on all sides
            var canvasWidth = baseTexture.Width + 2 * flapPixels;
            var canvasHeight = baseTexture.Height + 2 * flapPixels;

            using var canvas = new SKBitmap(canvasWidth, canvasHeight);
            using var skCanvas = new SKCanvas(canvas);

            // Clear with transparency
            skCanvas.Clear(SKColors.Transparent);

            // 4. Draw base texture in center
            skCanvas.DrawBitmap(baseTexture, flapPixels, flapPixels);

            // 5. Draw flaps
            DrawFlap(skCanvas, CardinalDirection.North, baseTexture, flapPixels, canvasWidth, canvasHeight);
            DrawFlap(skCanvas, CardinalDirection.South, baseTexture, flapPixels, canvasWidth, canvasHeight);
            DrawFlap(skCanvas, CardinalDirection.East, baseTexture, flapPixels, canvasWidth, canvasHeight);
            DrawFlap(skCanvas, CardinalDirection.West, baseTexture, flapPixels, canvasWidth, canvasHeight);

            // 6. Encode to PNG
            using var image = SKImage.FromBitmap(canvas);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            result.OutputImage = encoded.ToArray();

            result.Success = true;
            result.Metadata["DPI"] = dpi;
            result.Metadata["CanvasWidth"] = canvasWidth;
            result.Metadata["CanvasHeight"] = canvasHeight;

            baseTexture.Dispose();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error executing flat horizontal transformation: {ex.Message}";
        }

        return result;
    }

    private void DrawFlap(
        SKCanvas canvas,
        CardinalDirection direction,
        SKBitmap baseTexture,
        int flapPixels,
        int canvasWidth,
        int canvasHeight)
    {
        var config = EdgeFlaps[direction];

        if (config.Mode == EdgeFlapMode.None)
            return;

        // Calculate flap rectangle (excluding corners)
        SKRect flapRect = direction switch
        {
            CardinalDirection.North => new SKRect(flapPixels, 0, canvasWidth - flapPixels, flapPixels),
            CardinalDirection.South => new SKRect(flapPixels, canvasHeight - flapPixels, canvasWidth - flapPixels, canvasHeight),
            CardinalDirection.East => new SKRect(canvasWidth - flapPixels, flapPixels, canvasWidth, canvasHeight - flapPixels),
            CardinalDirection.West => new SKRect(0, flapPixels, flapPixels, canvasHeight - flapPixels),
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
                DrawSymmetricFlap(canvas, flapRect, baseTexture, direction, flapPixels);
                break;
        }
    }

    private void DrawBlankFlap(SKCanvas canvas, SKRect rect)
    {
        // Black border + transparent/white interior
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

        using var paint = new SKPaint
        {
            IsAntialias = true
        };

        // DrawBitmap(bitmap, destRect, paint) - without obsolete FilterQuality
        canvas.DrawBitmap(bitmap, rect, paint);
    }

    private void DrawSymmetricFlap(
        SKCanvas canvas,
        SKRect flapRect,
        SKBitmap baseTexture,
        CardinalDirection direction,
        int flapPixels)
    {
        // Extract the edge strip from base texture and flip it
        SKRect sourceRect;
        bool flipHorizontal = false;
        bool flipVertical = false;

        switch (direction)
        {
            case CardinalDirection.North:
                sourceRect = new SKRect(0, 0, baseTexture.Width, flapPixels);
                flipHorizontal = true;
                break;

            case CardinalDirection.South:
                sourceRect = new SKRect(0, baseTexture.Height - flapPixels, baseTexture.Width, baseTexture.Height);
                flipHorizontal = true;
                break;

            case CardinalDirection.East:
                sourceRect = new SKRect(baseTexture.Width - flapPixels, 0, baseTexture.Width, baseTexture.Height);
                flipVertical = true;
                break;

            case CardinalDirection.West:
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
