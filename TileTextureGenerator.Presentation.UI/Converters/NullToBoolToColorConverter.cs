using System.Globalization;
using System.Reflection;

namespace TileTextureGenerator.Presentation.UI.Converters;

/// <summary>
/// Converts null/not-null to boolean, then to color.
/// Used for Add button: green if item selected, gray otherwise.
/// Parameter format: "TrueColor|FalseColor" (e.g., "Green|Gray").
/// </summary>
public class NullToBoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;

        if (parameter is string colorPair)
        {
            var colors = colorPair.Split('|');
            if (colors.Length == 2)
            {
                string colorName = isNotNull ? colors[0] : colors[1];
                return GetColorByName(colorName);
            }
        }

        return isNotNull ? Colors.Green : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a Color from the Colors class by name (case-insensitive).
    /// Falls back to Gray if not found.
    /// </summary>
    private static Color GetColorByName(string colorName)
    {
        // Try to get the color from the Colors class via reflection
        var colorProperty = typeof(Colors).GetProperty(colorName, 
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

        if (colorProperty != null && colorProperty.PropertyType == typeof(Color))
        {
            return (Color)colorProperty.GetValue(null)!;
        }

        // Fallback to gray if color not found
        return Colors.Gray;
    }
}
