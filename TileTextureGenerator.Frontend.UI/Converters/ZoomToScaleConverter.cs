using System.Globalization;

namespace TileTextureGenerator.Frontend.UI.Converters;

/// <summary>
/// Converter from zoom percentage (100) to scale factor (1.0)
/// </summary>
public class ZoomToScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double zoomPercent)
        {
            return zoomPercent / 100.0;
        }
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double scale)
        {
            return scale * 100.0;
        }
        return 100.0;
    }
}
