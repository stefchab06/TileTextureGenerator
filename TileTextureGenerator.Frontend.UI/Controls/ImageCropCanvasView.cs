using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Frontend.UI.Controls;

/// <summary>
/// Interaction mode for image editing
/// </summary>
public enum InteractionMode
{
    Resize,  // Square handles for resizing
    Rotate   // Round handles for rotation
}

/// <summary>
/// Handle position for resize operations
/// </summary>
public enum HandlePosition
{
    None,
    TopLeft,
    TopCenter,
    TopRight,
    MiddleRight,
    BottomRight,
    BottomCenter,
    BottomLeft,
    MiddleLeft,
    Center // For drag/move
}

/// <summary>
/// Custom SkiaSharp control to display image with crop mask and interactive editing
/// </summary>
public class ImageCropCanvasView : SKCanvasView
{
    // Configuration constants
    private const float HandleSize = 20f;
    private const int CropAreaSizePercent = 80; // Crop area size as percentage of available space
    private const float SnapAngleTolerance = 5f; // Degrees tolerance for rotation snapping to 90° multiples
    private const float SnapDistanceTolerance = 5f; // Pixels tolerance for edge snapping (entering snap zone)
    private const float UnsnapDistanceTolerance = 15f; // Pixels required to break out of snap (exit snap zone)
    private const float SnapIconSize = 40f; // Size of snap toggle icon

    private SKBitmap? _bitmap;
    private bool _needsAutoFit = false;

    // Interaction state
    private InteractionMode _interactionMode = InteractionMode.Resize;
    private HandlePosition _activeHandle = HandlePosition.None;
    private SKPoint _lastTouchPoint;
    private bool _isDragging = false;
    private SKRect _snapIconRect; // Bounds of snap icon for hit testing
    private bool _isSnappedHorizontal = false; // Track if currently snapped to horizontal edge
    private bool _isSnappedVertical = false; // Track if currently snapped to vertical edge

    public static readonly BindableProperty ImageDataProperty =
        BindableProperty.Create(
            nameof(ImageData),
            typeof(byte[]),
            typeof(ImageCropCanvasView),
            null,
            propertyChanged: OnImageDataChanged);

    public static readonly BindableProperty TileShapeProperty =
        BindableProperty.Create(
            nameof(TileShape),
            typeof(TileShape),
            typeof(ImageCropCanvasView),
            TileShape.Full,
            propertyChanged: OnTileShapeChanged);

    public static readonly BindableProperty ImageTranslationXProperty =
        BindableProperty.Create(
            nameof(ImageTranslationX),
            typeof(double),
            typeof(ImageCropCanvasView),
            0.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ImageTranslationYProperty =
        BindableProperty.Create(
            nameof(ImageTranslationY),
            typeof(double),
            typeof(ImageCropCanvasView),
            0.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty RotationAngleProperty =
        BindableProperty.Create(
            nameof(RotationAngle),
            typeof(double),
            typeof(ImageCropCanvasView),
            0.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ZoomLevelProperty =
        BindableProperty.Create(
            nameof(ZoomLevel),
            typeof(double),
            typeof(ImageCropCanvasView),
            100.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ScaleXProperty =
        BindableProperty.Create(
            nameof(ScaleX),
            typeof(double),
            typeof(ImageCropCanvasView),
            1.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty ScaleYProperty =
        BindableProperty.Create(
            nameof(ScaleY),
            typeof(double),
            typeof(ImageCropCanvasView),
            1.0,
            propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty IsSnapEnabledProperty =
        BindableProperty.Create(
            nameof(IsSnapEnabled),
            typeof(bool),
            typeof(ImageCropCanvasView),
            true,
            propertyChanged: OnPropertyChanged);

    public byte[]? ImageData
    {
        get => (byte[]?)GetValue(ImageDataProperty);
        set => SetValue(ImageDataProperty, value);
    }

    public TileShape TileShape
    {
        get => (TileShape)GetValue(TileShapeProperty);
        set => SetValue(TileShapeProperty, value);
    }

    public double ImageTranslationX
    {
        get => (double)GetValue(ImageTranslationXProperty);
        set => SetValue(ImageTranslationXProperty, value);
    }

    public double ImageTranslationY
    {
        get => (double)GetValue(ImageTranslationYProperty);
        set => SetValue(ImageTranslationYProperty, value);
    }

    public double RotationAngle
    {
        get => (double)GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public double ScaleX
    {
        get => (double)GetValue(ScaleXProperty);
        set => SetValue(ScaleXProperty, value);
    }

    public double ScaleY
    {
        get => (double)GetValue(ScaleYProperty);
        set => SetValue(ScaleYProperty, value);
    }

    public bool IsSnapEnabled
    {
        get => (bool)GetValue(IsSnapEnabledProperty);
        set => SetValue(IsSnapEnabledProperty, value);
    }

    public ImageCropCanvasView()
    {
        EnableTouchEvents = true;

        // Setup gesture recognizers for mouse/touch
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnTapped;
        GestureRecognizers.Add(tapGesture);
    }

    private static void OnImageDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ImageCropCanvasView view)
        {
            view._bitmap?.Dispose();
            view._bitmap = null;

            if (newValue is byte[] imageData && imageData.Length > 0)
            {
                using var stream = new MemoryStream(imageData);
                view._bitmap = SKBitmap.Decode(stream);
                view._needsAutoFit = true;
            }

            view.InvalidateSurface();
        }
    }

    private static void OnTileShapeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ImageCropCanvasView view)
        {
            view._needsAutoFit = true;
            view.InvalidateSurface();
        }
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ImageCropCanvasView view)
        {
            view.InvalidateSurface();
        }
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (_bitmap == null)
        {
            return;
        }

        var info = e.Info;
        var canvasWidth = info.Width;
        var canvasHeight = info.Height;

        // Calculate crop area size based on tile shape
        var cropSize = GetCropSize(canvasWidth, canvasHeight);

        // Auto-fit image to crop area when first loaded or tile shape changes
        if (_needsAutoFit)
        {
            AutoFitImageToCropArea(cropSize);
            _needsAutoFit = false;
        }

        canvas.Save();

        // Canvas center
        var centerX = canvasWidth / 2f;
        var centerY = canvasHeight / 2f;

        // Calculate scale for image display using separate X and Y scales
        // Base scale fits the image to the crop area
        float baseScaleX = cropSize.Width / _bitmap.Width;
        float baseScaleY = cropSize.Height / _bitmap.Height;
        float baseScale = Math.Min(baseScaleX, baseScaleY);

        // Apply user-defined scales
        var imageScaleX = baseScale * (float)ScaleX;
        var imageScaleY = baseScale * (float)ScaleY;

        // Apply transformations
        canvas.Translate(centerX + (float)ImageTranslationX, centerY + (float)ImageTranslationY);
        canvas.RotateDegrees((float)RotationAngle);
        canvas.Scale(imageScaleX, imageScaleY);

        // Draw blurred image (outside crop area)
        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.FilterQuality = SKFilterQuality.High;

            // Apply light blur filter
            paint.ImageFilter = SKImageFilter.CreateBlur(8, 8);
            canvas.DrawBitmap(_bitmap, -_bitmap.Width / 2, -_bitmap.Height / 2, paint);
        }

        canvas.Restore();

        // Draw semi-transparent mask with hole for crop area
        DrawMaskWithHole(canvas, centerX, centerY, cropSize);

        // Draw clear image in crop area
        canvas.Save();
        canvas.Translate(centerX + (float)ImageTranslationX, centerY + (float)ImageTranslationY);
        canvas.RotateDegrees((float)RotationAngle);
        canvas.Scale(imageScaleX, imageScaleY);

        // Clip to only draw inside crop area
        canvas.ClipRect(GetCropRect(0, 0, cropSize), SKClipOperation.Intersect);

        using (var paint = new SKPaint())
        {
            paint.IsAntialias = true;
            paint.FilterQuality = SKFilterQuality.High;
            canvas.DrawBitmap(_bitmap, -_bitmap.Width / 2, -_bitmap.Height / 2, paint);
        }

        canvas.Restore();

        // Draw crop area border and handles
        DrawCropBorder(canvas, centerX, centerY, cropSize, imageScaleX, imageScaleY);
    }

    private void AutoFitImageToCropArea(SKSize cropSize)
    {
        if (_bitmap == null)
            return;

        // Calculate scale factors needed to cover the crop area
        float scaleX = cropSize.Width / _bitmap.Width;
        float scaleY = cropSize.Height / _bitmap.Height;

        // To COVER (fill completely), use the larger scale for both dimensions
        float coverScale = Math.Max(scaleX, scaleY);
        float baseScale = Math.Min(scaleX, scaleY);

        // Set uniform scale to cover the area
        float scaleFactor = coverScale / baseScale;
        ScaleX = scaleFactor;
        ScaleY = scaleFactor;
        ZoomLevel = scaleFactor * 100.0; // Keep ZoomLevel in sync for compatibility

        // Calculate final dimensions after scaling
        float finalScaleX = baseScale * (float)ScaleX;
        float finalScaleY = baseScale * (float)ScaleY;
        float scaledWidth = _bitmap.Width * finalScaleX;
        float scaledHeight = _bitmap.Height * finalScaleY;

        // Calculate translation to align top-left corners
        ImageTranslationX = -(scaledWidth - cropSize.Width) / 2f;
        ImageTranslationY = -(scaledHeight - cropSize.Height) / 2f;
        RotationAngle = 0;
    }

    private SKSize GetCropSize(int canvasWidth, int canvasHeight)
    {
        var size = Math.Min(canvasWidth, canvasHeight) * (CropAreaSizePercent / 100f);

        return TileShape switch
        {
            TileShape.Full => new SKSize(size, size),
            TileShape.HalfHorizontal => new SKSize(size, size / 2),
            TileShape.HalfVertical => new SKSize(size / 2, size),
            _ => new SKSize(size, size)
        };
    }

    private SKRect GetCropRect(float centerX, float centerY, SKSize cropSize)
    {
        return new SKRect(
            centerX - cropSize.Width / 2,
            centerY - cropSize.Height / 2,
            centerX + cropSize.Width / 2,
            centerY + cropSize.Height / 2);
    }

    private void DrawMaskWithHole(SKCanvas canvas, float centerX, float centerY, SKSize cropSize)
    {
        using var maskPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(180),
            Style = SKPaintStyle.Fill
        };

        using var path = new SKPath();

        // Full canvas rectangle
        path.AddRect(new SKRect(0, 0, canvas.LocalClipBounds.Width, canvas.LocalClipBounds.Height));

        // Hole rectangle (crop area)
        var cropRect = GetCropRect(centerX, centerY, cropSize);
        path.AddRect(cropRect, SKPathDirection.CounterClockwise);

        path.FillType = SKPathFillType.EvenOdd;
        canvas.DrawPath(path, maskPaint);
    }

    private void DrawCropBorder(SKCanvas canvas, float centerX, float centerY, SKSize cropSize, float imageScaleX, float imageScaleY)
    {
        var cropRect = GetCropRect(centerX, centerY, cropSize);

        using var borderPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawRect(cropRect, borderPaint);

        // Draw grid lines according to tile shape
        using var gridPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(128),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };

        // Determine number of divisions based on shape
        int horizontalDivisions;
        int verticalDivisions;

        switch (TileShape)
        {
            case TileShape.Full:
                // Square tile: 4x4
                horizontalDivisions = 4;
                verticalDivisions = 4;
                break;
            case TileShape.HalfHorizontal:
                // Horizontal rectangle: 4 horizontally, 2 vertically
                horizontalDivisions = 4;
                verticalDivisions = 2;
                break;
            case TileShape.HalfVertical:
                // Vertical rectangle: 2 horizontally, 4 vertically
                horizontalDivisions = 2;
                verticalDivisions = 4;
                break;
            default:
                horizontalDivisions = 4;
                verticalDivisions = 4;
                break;
        }

        // Draw vertical lines
        for (int i = 1; i < horizontalDivisions; i++)
        {
            float x = cropRect.Left + (cropRect.Width * i / horizontalDivisions);
            canvas.DrawLine(x, cropRect.Top, x, cropRect.Bottom, gridPaint);
        }

        // Draw horizontal lines
        for (int i = 1; i < verticalDivisions; i++)
        {
            float y = cropRect.Top + (cropRect.Height * i / verticalDivisions);
            canvas.DrawLine(cropRect.Left, y, cropRect.Right, y, gridPaint);
        }

        // Draw handles for interactive editing
        DrawHandles(canvas, centerX, centerY, imageScaleX, imageScaleY);

        // Draw snap toggle icon
        DrawSnapIcon(canvas, cropRect);
    }

    private void DrawHandles(SKCanvas canvas, float centerX, float centerY, float imageScaleX, float imageScaleY)
    {
        if (_bitmap == null)
            return;

        // Get actual corner and midpoint positions after rotation
        var corners = GetTransformedImageCorners(centerX, centerY, imageScaleX, imageScaleY);

        using var handlePaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var handleBorderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };

        if (_interactionMode == InteractionMode.Resize)
        {
            // Draw 8 square handles for resize mode on actual image corners/midpoints
            DrawSquareHandle(canvas, corners.topLeft.X, corners.topLeft.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.midTop.X, corners.midTop.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.topRight.X, corners.topRight.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.midRight.X, corners.midRight.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.bottomRight.X, corners.bottomRight.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.midBottom.X, corners.midBottom.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.bottomLeft.X, corners.bottomLeft.Y, handlePaint, handleBorderPaint);
            DrawSquareHandle(canvas, corners.midLeft.X, corners.midLeft.Y, handlePaint, handleBorderPaint);
        }
        else // Rotate mode
        {
            // Draw 4 round handles at corners for rotate mode on actual corners
            DrawRoundHandle(canvas, corners.topLeft.X, corners.topLeft.Y, handlePaint, handleBorderPaint);
            DrawRoundHandle(canvas, corners.topRight.X, corners.topRight.Y, handlePaint, handleBorderPaint);
            DrawRoundHandle(canvas, corners.bottomRight.X, corners.bottomRight.Y, handlePaint, handleBorderPaint);
            DrawRoundHandle(canvas, corners.bottomLeft.X, corners.bottomLeft.Y, handlePaint, handleBorderPaint);
        }
    }

    private SKRect GetTransformedImageBounds(float centerX, float centerY, float imageScaleX, float imageScaleY)
    {
        if (_bitmap == null)
            return SKRect.Empty;

        // Calculate image dimensions after scaling
        float scaledWidth = _bitmap.Width * imageScaleX;
        float scaledHeight = _bitmap.Height * imageScaleY;

        // Image center after translation
        float imageX = centerX + (float)ImageTranslationX;
        float imageY = centerY + (float)ImageTranslationY;

        // If no rotation, simple axis-aligned bounds
        if (Math.Abs(RotationAngle) < 0.01)
        {
            return new SKRect(
                imageX - scaledWidth / 2,
                imageY - scaledHeight / 2,
                imageX + scaledWidth / 2,
                imageY + scaledHeight / 2);
        }

        // With rotation: calculate actual corner positions
        double angleRad = RotationAngle * Math.PI / 180.0;
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);

        // Four corners of the image (before rotation, relative to center)
        float halfW = scaledWidth / 2;
        float halfH = scaledHeight / 2;

        // Rotate each corner around image center
        var corners = new SKPoint[4];
        corners[0] = RotatePoint(-halfW, -halfH, cos, sin, imageX, imageY); // Top-left
        corners[1] = RotatePoint(halfW, -halfH, cos, sin, imageX, imageY);  // Top-right
        corners[2] = RotatePoint(halfW, halfH, cos, sin, imageX, imageY);   // Bottom-right
        corners[3] = RotatePoint(-halfW, halfH, cos, sin, imageX, imageY);  // Bottom-left

        // Find bounding box of rotated corners
        float minX = corners.Min(p => p.X);
        float minY = corners.Min(p => p.Y);
        float maxX = corners.Max(p => p.X);
        float maxY = corners.Max(p => p.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }

    private SKPoint RotatePoint(float x, float y, float cos, float sin, float centerX, float centerY)
    {
        float rotatedX = x * cos - y * sin;
        float rotatedY = x * sin + y * cos;
        return new SKPoint(centerX + rotatedX, centerY + rotatedY);
    }

    private (SKPoint topLeft, SKPoint topRight, SKPoint bottomRight, SKPoint bottomLeft, SKPoint midTop, SKPoint midRight, SKPoint midBottom, SKPoint midLeft) GetTransformedImageCorners(float centerX, float centerY, float imageScaleX, float imageScaleY)
    {
        if (_bitmap == null)
            return default;

        float scaledWidth = _bitmap.Width * imageScaleX;
        float scaledHeight = _bitmap.Height * imageScaleY;
        float imageX = centerX + (float)ImageTranslationX;
        float imageY = centerY + (float)ImageTranslationY;

        double angleRad = RotationAngle * Math.PI / 180.0;
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);

        float halfW = scaledWidth / 2;
        float halfH = scaledHeight / 2;

        // Calculate all handle positions after rotation
        var topLeft = RotatePoint(-halfW, -halfH, cos, sin, imageX, imageY);
        var topRight = RotatePoint(halfW, -halfH, cos, sin, imageX, imageY);
        var bottomRight = RotatePoint(halfW, halfH, cos, sin, imageX, imageY);
        var bottomLeft = RotatePoint(-halfW, halfH, cos, sin, imageX, imageY);
        var midTop = RotatePoint(0, -halfH, cos, sin, imageX, imageY);
        var midRight = RotatePoint(halfW, 0, cos, sin, imageX, imageY);
        var midBottom = RotatePoint(0, halfH, cos, sin, imageX, imageY);
        var midLeft = RotatePoint(-halfW, 0, cos, sin, imageX, imageY);

        return (topLeft, topRight, bottomRight, bottomLeft, midTop, midRight, midBottom, midLeft);
    }

    private void DrawSquareHandle(SKCanvas canvas, float x, float y, SKPaint fillPaint, SKPaint borderPaint)
    {
        var handleRect = new SKRect(
            x - HandleSize / 2,
            y - HandleSize / 2,
            x + HandleSize / 2,
            y + HandleSize / 2);

        canvas.DrawRect(handleRect, fillPaint);
        canvas.DrawRect(handleRect, borderPaint);
    }

    private void DrawRoundHandle(SKCanvas canvas, float x, float y, SKPaint fillPaint, SKPaint borderPaint)
    {
        canvas.DrawCircle(x, y, HandleSize / 2, fillPaint);
        canvas.DrawCircle(x, y, HandleSize / 2, borderPaint);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        // Get tap position (need to convert from MAUI coordinates to SKCanvas coordinates)
        var position = e.GetPosition(this);
        if (position.HasValue)
        {
            // Convert to SKCanvas coordinates
            float density = (float)(DeviceDisplay.MainDisplayInfo.Density);
            var skPoint = new SKPoint((float)position.Value.X * density, (float)position.Value.Y * density);

            // Don't toggle mode if tapping on snap icon
            if (_snapIconRect.Contains(skPoint))
                return;
        }

        // Toggle between resize and rotate modes
        _interactionMode = _interactionMode == InteractionMode.Resize 
            ? InteractionMode.Rotate 
            : InteractionMode.Resize;

        InvalidateSurface();
    }

    protected override void OnTouch(SKTouchEventArgs e)
    {
        base.OnTouch(e);

        var touchPoint = e.Location;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                HandleTouchPressed(touchPoint);
                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (_isDragging)
                {
                    HandleTouchMoved(touchPoint);
                    e.Handled = true;
                }
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                HandleTouchReleased();
                e.Handled = true;
                break;
        }
    }

    private void HandleTouchPressed(SKPoint touchPoint)
    {
        // Check if click is on snap icon first
        if (_snapIconRect.Contains(touchPoint))
        {
            IsSnapEnabled = !IsSnapEnabled;
            InvalidateSurface();
            return;
        }

        var canvasWidth = CanvasSize.Width;
        var canvasHeight = CanvasSize.Height;
        var centerX = canvasWidth / 2f;
        var centerY = canvasHeight / 2f;
        var cropSize = GetCropSize((int)canvasWidth, (int)canvasHeight);

        // Calculate image scales
        float baseScaleX = cropSize.Width / _bitmap.Width;
        float baseScaleY = cropSize.Height / _bitmap.Height;
        float baseScale = Math.Min(baseScaleX, baseScaleY);
        float imageScaleX = baseScale * (float)ScaleX;
        float imageScaleY = baseScale * (float)ScaleY;

        var corners = GetTransformedImageCorners(centerX, centerY, imageScaleX, imageScaleY);
        var imageBounds = GetTransformedImageBounds(centerX, centerY, imageScaleX, imageScaleY);

        // Check if touch is on a handle (using actual rotated positions)
        _activeHandle = GetHandleAtPoint(touchPoint, corners);

        if (_activeHandle == HandlePosition.None)
        {
            // Check if touch is inside image bounds for drag/move
            if (imageBounds.Contains(touchPoint))
            {
                _activeHandle = HandlePosition.Center;
            }
        }

        if (_activeHandle != HandlePosition.None)
        {
            _isDragging = true;
            _lastTouchPoint = touchPoint;
        }
    }

    private void HandleTouchMoved(SKPoint touchPoint)
    {
        float deltaX = touchPoint.X - _lastTouchPoint.X;
        float deltaY = touchPoint.Y - _lastTouchPoint.Y;

        if (_activeHandle == HandlePosition.Center)
        {
            // Drag to move image
            double newX = ImageTranslationX + deltaX;
            double newY = ImageTranslationY + deltaY;

            // Apply snapping
            var snapped = ApplyTranslationSnapping(newX, newY);
            ImageTranslationX = snapped.x;
            ImageTranslationY = snapped.y;
        }
        else if (_interactionMode == InteractionMode.Rotate && _activeHandle != HandlePosition.None)
        {
            // Rotate mode: calculate angle from center
            var canvasWidth = CanvasSize.Width;
            var canvasHeight = CanvasSize.Height;
            var centerX = canvasWidth / 2f + (float)ImageTranslationX;
            var centerY = canvasHeight / 2f + (float)ImageTranslationY;

            float angleOld = (float)Math.Atan2(_lastTouchPoint.Y - centerY, _lastTouchPoint.X - centerX);
            float angleNew = (float)Math.Atan2(touchPoint.Y - centerY, touchPoint.X - centerX);
            float angleDelta = (angleNew - angleOld) * 180f / MathF.PI;

            double newAngle = (RotationAngle + angleDelta) % 360;

            // Apply rotation snapping
            RotationAngle = ApplyRotationSnapping(newAngle);
        }
        else if (_interactionMode == InteractionMode.Resize && _activeHandle != HandlePosition.None)
        {
            // Resize mode: adjust zoom based on handle
            HandleResize(deltaX, deltaY);
        }

        _lastTouchPoint = touchPoint;
        InvalidateSurface();
    }

    private void HandleTouchReleased()
    {
        // TODO: Edge snapping during resize needs review - currently only snaps on release
        // The snap doesn't work properly during active resize operations.
        // Possible solutions to explore:
        // 1. Apply snap during resize while maintaining anchor-based resizing
        // 2. Snap to grid subdivision lines (not just crop edges)
        // 3. Different snap logic for side handles vs corner handles

        // Apply snapping if we just finished a resize operation
        if (_interactionMode == InteractionMode.Resize && _activeHandle != HandlePosition.None && _activeHandle != HandlePosition.Center)
        {
            var snapped = ApplyTranslationSnapping(ImageTranslationX, ImageTranslationY);
            ImageTranslationX = snapped.x;
            ImageTranslationY = snapped.y;
            InvalidateSurface();
        }

        _isDragging = false;
        _activeHandle = HandlePosition.None;
        _isSnappedHorizontal = false;
        _isSnappedVertical = false;
    }

    private HandlePosition GetHandleAtPoint(SKPoint point, (SKPoint topLeft, SKPoint topRight, SKPoint bottomRight, SKPoint bottomLeft, SKPoint midTop, SKPoint midRight, SKPoint midBottom, SKPoint midLeft) corners)
    {
        float hitRadius = HandleSize * 1.5f; // Larger hit area for easier interaction

        // Check handles based on mode
        if (_interactionMode == InteractionMode.Resize)
        {
            // 8 handles - check in order of corners first, then sides
            if (IsPointNearHandle(point, corners.topLeft.X, corners.topLeft.Y, hitRadius))
                return HandlePosition.TopLeft;
            if (IsPointNearHandle(point, corners.topRight.X, corners.topRight.Y, hitRadius))
                return HandlePosition.TopRight;
            if (IsPointNearHandle(point, corners.bottomRight.X, corners.bottomRight.Y, hitRadius))
                return HandlePosition.BottomRight;
            if (IsPointNearHandle(point, corners.bottomLeft.X, corners.bottomLeft.Y, hitRadius))
                return HandlePosition.BottomLeft;
            if (IsPointNearHandle(point, corners.midTop.X, corners.midTop.Y, hitRadius))
                return HandlePosition.TopCenter;
            if (IsPointNearHandle(point, corners.midRight.X, corners.midRight.Y, hitRadius))
                return HandlePosition.MiddleRight;
            if (IsPointNearHandle(point, corners.midBottom.X, corners.midBottom.Y, hitRadius))
                return HandlePosition.BottomCenter;
            if (IsPointNearHandle(point, corners.midLeft.X, corners.midLeft.Y, hitRadius))
                return HandlePosition.MiddleLeft;
        }
        else // Rotate mode
        {
            // 4 corner handles only
            if (IsPointNearHandle(point, corners.topLeft.X, corners.topLeft.Y, hitRadius))
                return HandlePosition.TopLeft;
            if (IsPointNearHandle(point, corners.topRight.X, corners.topRight.Y, hitRadius))
                return HandlePosition.TopRight;
            if (IsPointNearHandle(point, corners.bottomRight.X, corners.bottomRight.Y, hitRadius))
                return HandlePosition.BottomRight;
            if (IsPointNearHandle(point, corners.bottomLeft.X, corners.bottomLeft.Y, hitRadius))
                return HandlePosition.BottomLeft;
        }

        return HandlePosition.None;
    }

    private bool IsPointNearHandle(SKPoint point, float handleX, float handleY, float radius)
    {
        float dx = point.X - handleX;
        float dy = point.Y - handleY;
        return (dx * dx + dy * dy) <= (radius * radius);
    }

    private void HandleResize(float deltaX, float deltaY)
    {
        if (_bitmap == null)
            return;

        // Get current dimensions
        var canvasWidth = CanvasSize.Width;
        var canvasHeight = CanvasSize.Height;
        var cropSize = GetCropSize((int)canvasWidth, (int)canvasHeight);
        float baseScaleX = cropSize.Width / _bitmap.Width;
        float baseScaleY = cropSize.Height / _bitmap.Height;
        float baseScale = Math.Min(baseScaleX, baseScaleY);

        float currentScaleX = baseScale * (float)ScaleX;
        float currentScaleY = baseScale * (float)ScaleY;

        // Transform delta into image's local coordinate system
        double angleRad = -RotationAngle * Math.PI / 180.0;
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);

        float localDX = deltaX * cos - deltaY * sin;
        float localDY = deltaX * sin + deltaY * cos;

        // Calculate scale changes and anchor point
        double scaleXChange = 0;
        double scaleYChange = 0;
        SKPoint anchorLocalPos = SKPoint.Empty; // Anchor point in local image space

        switch (_activeHandle)
        {
            case HandlePosition.TopLeft:
                // Proportional, anchor at bottom-right
                float avgDelta1 = -(localDX + localDY) / 2f;
                scaleXChange = (avgDelta1 / _bitmap.Width) / baseScale;
                scaleYChange = scaleXChange; // Proportional
                anchorLocalPos = new SKPoint(_bitmap.Width / 2, _bitmap.Height / 2);
                break;

            case HandlePosition.TopCenter:
                // Height only, anchor at bottom edge
                scaleYChange = (-localDY / _bitmap.Height) / baseScale;
                anchorLocalPos = new SKPoint(0, _bitmap.Height / 2);
                break;

            case HandlePosition.TopRight:
                // Proportional, anchor at bottom-left
                float avgDelta2 = (localDX - localDY) / 2f;
                scaleXChange = (avgDelta2 / _bitmap.Width) / baseScale;
                scaleYChange = scaleXChange;
                anchorLocalPos = new SKPoint(-_bitmap.Width / 2, _bitmap.Height / 2);
                break;

            case HandlePosition.MiddleRight:
                // Width only, anchor at left edge
                scaleXChange = (localDX / _bitmap.Width) / baseScale;
                anchorLocalPos = new SKPoint(-_bitmap.Width / 2, 0);
                break;

            case HandlePosition.BottomRight:
                // Proportional, anchor at top-left
                float avgDelta3 = (localDX + localDY) / 2f;
                scaleXChange = (avgDelta3 / _bitmap.Width) / baseScale;
                scaleYChange = scaleXChange;
                anchorLocalPos = new SKPoint(-_bitmap.Width / 2, -_bitmap.Height / 2);
                break;

            case HandlePosition.BottomCenter:
                // Height only, anchor at top edge
                scaleYChange = (localDY / _bitmap.Height) / baseScale;
                anchorLocalPos = new SKPoint(0, -_bitmap.Height / 2);
                break;

            case HandlePosition.BottomLeft:
                // Proportional, anchor at top-right
                float avgDelta4 = (-localDX + localDY) / 2f;
                scaleXChange = (avgDelta4 / _bitmap.Width) / baseScale;
                scaleYChange = scaleXChange;
                anchorLocalPos = new SKPoint(_bitmap.Width / 2, -_bitmap.Height / 2);
                break;

            case HandlePosition.MiddleLeft:
                // Width only, anchor at right edge
                scaleXChange = (-localDX / _bitmap.Width) / baseScale;
                anchorLocalPos = new SKPoint(_bitmap.Width / 2, 0);
                break;
        }

        // Apply scale changes
        double newScaleX = Math.Max(0.5, Math.Min(4.0, ScaleX + scaleXChange));
        double newScaleY = Math.Max(0.5, Math.Min(4.0, ScaleY + scaleYChange));

        // Calculate anchor point position in screen space BEFORE scale change
        float oldScaledAnchorX = anchorLocalPos.X * currentScaleX;
        float oldScaledAnchorY = anchorLocalPos.Y * currentScaleY;

        // Apply rotation to anchor position
        double rotRad = RotationAngle * Math.PI / 180.0;
        float cosRot = (float)Math.Cos(rotRad);
        float sinRot = (float)Math.Sin(rotRad);
        float oldScreenAnchorX = oldScaledAnchorX * cosRot - oldScaledAnchorY * sinRot;
        float oldScreenAnchorY = oldScaledAnchorX * sinRot + oldScaledAnchorY * cosRot;

        // Calculate anchor point position AFTER scale change
        float newScaleXVal = baseScale * (float)newScaleX;
        float newScaleYVal = baseScale * (float)newScaleY;
        float newScaledAnchorX = anchorLocalPos.X * newScaleXVal;
        float newScaledAnchorY = anchorLocalPos.Y * newScaleYVal;
        float newScreenAnchorX = newScaledAnchorX * cosRot - newScaledAnchorY * sinRot;
        float newScreenAnchorY = newScaledAnchorX * sinRot + newScaledAnchorY * cosRot;

        // Adjust translation to keep anchor point fixed
        float anchorShiftX = oldScreenAnchorX - newScreenAnchorX;
        float anchorShiftY = oldScreenAnchorY - newScreenAnchorY;

        // Apply changes
        ScaleX = newScaleX;
        ScaleY = newScaleY;
        ImageTranslationX += anchorShiftX;
        ImageTranslationY += anchorShiftY;
        ZoomLevel = Math.Max(ScaleX, ScaleY) * 100.0;
    }

    private void DrawSnapIcon(SKCanvas canvas, SKRect cropRect)
    {
        // Position: top-left corner of canvas, with some padding
        float iconX = 10;
        float iconY = 10;

        _snapIconRect = new SKRect(iconX, iconY, iconX + SnapIconSize, iconY + SnapIconSize);

        // Draw background circle
        using var bgPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(200),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(_snapIconRect.MidX, _snapIconRect.MidY, SnapIconSize / 2, bgPaint);

        // Draw magnet shape (U shape in black with red ends)
        float magnetWidth = SnapIconSize * 0.5f;
        float magnetHeight = SnapIconSize * 0.6f;
        float magnetThickness = SnapIconSize * 0.15f;
        float centerX = _snapIconRect.MidX;
        float centerY = _snapIconRect.MidY;

        using var magnetPath = new SKPath();

        // Left vertical bar
        magnetPath.AddRect(new SKRect(
            centerX - magnetWidth / 2,
            centerY - magnetHeight / 2,
            centerX - magnetWidth / 2 + magnetThickness,
            centerY + magnetHeight / 2));

        // Bottom horizontal bar
        magnetPath.AddRect(new SKRect(
            centerX - magnetWidth / 2,
            centerY + magnetHeight / 2 - magnetThickness,
            centerX + magnetWidth / 2,
            centerY + magnetHeight / 2));

        // Right vertical bar
        magnetPath.AddRect(new SKRect(
            centerX + magnetWidth / 2 - magnetThickness,
            centerY - magnetHeight / 2,
            centerX + magnetWidth / 2,
            centerY + magnetHeight / 2));

        using var magnetPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawPath(magnetPath, magnetPaint);

        // Draw red ends (poles)
        using var polePaint = new SKPaint
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        float poleHeight = magnetThickness * 1.5f;
        // Left pole (N)
        canvas.DrawRect(new SKRect(
            centerX - magnetWidth / 2,
            centerY - magnetHeight / 2,
            centerX - magnetWidth / 2 + magnetThickness,
            centerY - magnetHeight / 2 + poleHeight), polePaint);

        // Right pole (S)
        canvas.DrawRect(new SKRect(
            centerX + magnetWidth / 2 - magnetThickness,
            centerY - magnetHeight / 2,
            centerX + magnetWidth / 2,
            centerY - magnetHeight / 2 + poleHeight), polePaint);

        // If disabled, draw red X over it
        if (!IsSnapEnabled)
        {
            using var crossPaint = new SKPaint
            {
                Color = SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            float crossMargin = SnapIconSize * 0.2f;
            canvas.DrawLine(
                iconX + crossMargin, iconY + crossMargin,
                iconX + SnapIconSize - crossMargin, iconY + SnapIconSize - crossMargin,
                crossPaint);
            canvas.DrawLine(
                iconX + SnapIconSize - crossMargin, iconY + crossMargin,
                iconX + crossMargin, iconY + SnapIconSize - crossMargin,
                crossPaint);
        }

        // Draw border around icon
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(100),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true
        };
        canvas.DrawCircle(_snapIconRect.MidX, _snapIconRect.MidY, SnapIconSize / 2, borderPaint);
    }

    private double ApplyRotationSnapping(double angle)
    {
        if (!IsSnapEnabled)
            return angle;

        // Normalize angle to 0-360 range
        double normalizedAngle = angle % 360;
        if (normalizedAngle < 0)
            normalizedAngle += 360;

        // Check proximity to multiples of 90°
        double[] snapAngles = { 0, 90, 180, 270, 360 };

        foreach (double snapAngle in snapAngles)
        {
            double diff = Math.Abs(normalizedAngle - snapAngle);
            if (diff <= SnapAngleTolerance)
            {
                return snapAngle % 360;
            }
        }

        return angle;
    }

    private (double x, double y) ApplyTranslationSnapping(double translationX, double translationY)
    {
        if (!IsSnapEnabled || _bitmap == null)
            return (translationX, translationY);

        var canvasWidth = CanvasSize.Width;
        var canvasHeight = CanvasSize.Height;
        var centerX = canvasWidth / 2f;
        var centerY = canvasHeight / 2f;
        var cropSize = GetCropSize((int)canvasWidth, (int)canvasHeight);
        var cropRect = GetCropRect(centerX, centerY, cropSize);

        float baseScaleX = cropSize.Width / _bitmap.Width;
        float baseScaleY = cropSize.Height / _bitmap.Height;
        float baseScale = Math.Min(baseScaleX, baseScaleY);
        float imageScaleX = baseScale * (float)ScaleX;
        float imageScaleY = baseScale * (float)ScaleY;

        // Calculate where image will be with this translation
        float imageX = centerX + (float)translationX;
        float imageY = centerY + (float)translationY;

        // Get bounding box for snapping
        var imageBounds = CalculateImageBounds(imageX, imageY, imageScaleX, imageScaleY);

        double snappedX = translationX;
        double snappedY = translationY;

        // Horizontal snapping with hysteresis
        float leftDist = Math.Abs(imageBounds.Left - cropRect.Left);
        float rightDist = Math.Abs(imageBounds.Right - cropRect.Right);

        float snapThresholdX = _isSnappedHorizontal ? UnsnapDistanceTolerance : SnapDistanceTolerance;

        if (leftDist < rightDist && leftDist < snapThresholdX)
        {
            snappedX = translationX + (cropRect.Left - imageBounds.Left);
            _isSnappedHorizontal = true;
        }
        else if (rightDist < snapThresholdX)
        {
            snappedX = translationX + (cropRect.Right - imageBounds.Right);
            _isSnappedHorizontal = true;
        }
        else if (leftDist > snapThresholdX && rightDist > snapThresholdX)
        {
            _isSnappedHorizontal = false;
        }

        // Vertical snapping with hysteresis
        float topDist = Math.Abs(imageBounds.Top - cropRect.Top);
        float bottomDist = Math.Abs(imageBounds.Bottom - cropRect.Bottom);

        float snapThresholdY = _isSnappedVertical ? UnsnapDistanceTolerance : SnapDistanceTolerance;

        if (topDist < bottomDist && topDist < snapThresholdY)
        {
            snappedY = translationY + (cropRect.Top - imageBounds.Top);
            _isSnappedVertical = true;
        }
        else if (bottomDist < snapThresholdY)
        {
            snappedY = translationY + (cropRect.Bottom - imageBounds.Bottom);
            _isSnappedVertical = true;
        }
        else if (topDist > snapThresholdY && bottomDist > snapThresholdY)
        {
            _isSnappedVertical = false;
        }

        return (snappedX, snappedY);
    }

    private SKRect CalculateImageBounds(float imageX, float imageY, float imageScaleX, float imageScaleY)
    {
        if (_bitmap == null)
            return SKRect.Empty;

        float scaledWidth = _bitmap.Width * imageScaleX;
        float scaledHeight = _bitmap.Height * imageScaleY;

        // If no rotation, simple bounds
        if (Math.Abs(RotationAngle) < 0.01)
        {
            return new SKRect(
                imageX - scaledWidth / 2,
                imageY - scaledHeight / 2,
                imageX + scaledWidth / 2,
                imageY + scaledHeight / 2);
        }

        // With rotation: calculate rotated corners and find bounding box
        double angleRad = RotationAngle * Math.PI / 180.0;
        float cos = (float)Math.Cos(angleRad);
        float sin = (float)Math.Sin(angleRad);

        float halfW = scaledWidth / 2;
        float halfH = scaledHeight / 2;

        var corners = new SKPoint[4];
        corners[0] = RotatePoint(-halfW, -halfH, cos, sin, imageX, imageY);
        corners[1] = RotatePoint(halfW, -halfH, cos, sin, imageX, imageY);
        corners[2] = RotatePoint(halfW, halfH, cos, sin, imageX, imageY);
        corners[3] = RotatePoint(-halfW, halfH, cos, sin, imageX, imageY);

        float minX = corners.Min(p => p.X);
        float minY = corners.Min(p => p.Y);
        float maxX = corners.Max(p => p.X);
        float maxY = corners.Max(p => p.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }
}
