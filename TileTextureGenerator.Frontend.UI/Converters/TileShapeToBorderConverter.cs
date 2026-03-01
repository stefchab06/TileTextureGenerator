using System.Globalization;
using TileTextureGenerator.Core.Enums;

namespace TileTextureGenerator.Frontend.UI.Converters;

/// <summary>
/// Converter to highlight the selected tile shape with a colored border
/// </summary>
public class TileShapeToBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TileShape selected && parameter is TileShape target)
        {
            return selected == target 
                ? Colors.Blue  // Selected
                : Colors.Transparent;  // Not selected
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
