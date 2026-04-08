using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using TileTextureGenerator.Presentation.UI.Services;
using TileTextureGenerator.Adapters.UseCases.Enums;

namespace TileTextureGenerator.Presentation.UI.Converters;

public class ProjectTypeToLocalizedNameConverter : IValueConverter
{
    public ProjectTypeLocalizer? Localizer { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string technicalType && Localizer is not null)
            return Localizer.GetLocalizedName(technicalType);
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ProjectStatusToLocalizedStringConverter : IValueConverter
{
    public ProjectTypeLocalizer? Localizer { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProjectStatus status && Localizer is not null)
        {
            return status switch
            {
                ProjectStatus.Unexisting => Localizer.GetLocalizedString("ProjectStatus_Unexisting"),
                ProjectStatus.New => Localizer.GetLocalizedString("ProjectStatus_New"),
                ProjectStatus.Pending => Localizer.GetLocalizedString("ProjectStatus_Pending"),
                ProjectStatus.Generated => Localizer.GetLocalizedString("ProjectStatus_Generated"),
                ProjectStatus.Archived => Localizer.GetLocalizedString("ProjectStatus_Archived"),
                _ => status.ToString()
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ProjectImageOrPlaceholderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
            return ImageSource.FromStream(() => new System.IO.MemoryStream(bytes));
        return "placeholder_project.png";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
