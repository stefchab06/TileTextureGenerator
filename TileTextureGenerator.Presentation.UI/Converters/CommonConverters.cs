using System.Globalization;

namespace TileTextureGenerator.Presentation.UI.Converters;

/// <summary>
/// Converter to check if a value is not null.
/// Returns true if value is not null, false otherwise.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter to invert a boolean value.
/// Used for enabling/disabling controls based on IsBusy state.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return true; // Default to enabled if not bool
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }
}

/// <summary>
/// Converter for Picker binding to ProjectTypeItem.
/// Converts between SelectedProjectType (string) and ProjectTypeItem (object).
/// </summary>
public class ProjectTypeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This converter is not used for Picker with ItemsSource
        // The Picker handles this automatically with ItemDisplayBinding
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ViewModels.ProjectTypeItem item)
            return item.TechnicalName;

        return value;
    }
}

/// <summary>
/// Converter that returns green color when true (button active), gray when false (button inactive).
/// </summary>
public class BoolToButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Color.FromArgb("#2E7D32"); // Green (Material Design Green 800)
        }
        return Color.FromArgb("#BDBDBD"); // Gray (Material Design Gray 400)
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that returns white when true (active text), dark gray when false (inactive text).
/// </summary>
public class BoolToButtonTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Colors.White;
        }
        return Color.FromArgb("#757575"); // Dark gray (Material Design Gray 600)
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
