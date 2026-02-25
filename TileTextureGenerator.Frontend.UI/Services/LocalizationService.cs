using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TileTextureGenerator.Frontend.UI.Resources.Strings;

namespace TileTextureGenerator.Frontend.UI.Services;

public class LocalizationService
{
    public static LocalizationService Instance { get; } = new LocalizationService();

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;
    public event EventHandler? CultureChanged;

    public string GetString(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        var s = AppResources.ResourceManager.GetString(key, CurrentCulture);
        return string.IsNullOrEmpty(s) ? key : s;
    }

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null) throw new ArgumentNullException(nameof(culture));
        if (Equals(culture, CurrentCulture)) return;
        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }
}