using System.Globalization;

namespace TileTextureGenerator.Presentation.UI.Converters;

/// <summary>
/// Converts bool to text based on parameter format: "TrueText|FalseText"
/// Example: "▲|▼" returns ▲ if true, ▼ if false
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
            return "?";

        var parts = paramString.Split('|');
        if (parts.Length != 2)
            return "?";

        return boolValue ? parts[0] : parts[1];
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
