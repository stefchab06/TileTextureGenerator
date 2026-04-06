using System.Globalization;

namespace TileTextureGenerator.Presentation.UI.Converters;

/// <summary>
/// Converts item count to CollectionView height (count * 70 pixels per item)
/// </summary>
public class CountToHeightConverter : IValueConverter
{
    private const double ItemHeight = 70;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int count)
            return 0;

        return count * ItemHeight;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
