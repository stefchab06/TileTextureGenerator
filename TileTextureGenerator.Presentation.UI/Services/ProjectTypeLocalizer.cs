using System.Globalization;

namespace TileTextureGenerator.Presentation.UI.Services;

/// <summary>
/// Service to localize project type technical names to user-friendly names.
/// Also manages application language switching.
/// </summary>
public class ProjectTypeLocalizer
{
    private readonly Dictionary<string, string> _typeToResourceKey = new()
    {
        ["FloorTileProject"] = "ProjectType_FloorTileProject",
        ["WallTileProject"] = "ProjectType_WallTileProject"
    };

    // Supported languages with their culture codes and flag image sources
    private readonly List<LanguageInfo> _supportedLanguages = new()
    {
        new("en", "flag_en.png", "English"),
        new("fr", "flag_fr.png", "Français")
    };

    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Gets the current language info.
    /// </summary>
    public LanguageInfo CurrentLanguage { get; private set; }

    public ProjectTypeLocalizer()
    {
        // Load saved language or detect system language
        var savedLanguageCode = Preferences.Get("AppLanguage", "");

        if (string.IsNullOrEmpty(savedLanguageCode))
        {
            // First run: detect system language
            var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            savedLanguageCode = _supportedLanguages.Any(l => l.Code == systemLanguage) 
                              ? systemLanguage 
                              : "en"; // Default to English if system language not supported
        }

        CurrentLanguage = _supportedLanguages.FirstOrDefault(l => l.Code == savedLanguageCode) 
                         ?? _supportedLanguages[0];

        ApplyLanguage();
    }

    /// <summary>
    /// Cycles to the next supported language.
    /// </summary>
    public void CycleLanguage()
    {
        var currentIndex = _supportedLanguages.FindIndex(l => l.Code == CurrentLanguage.Code);
        var nextIndex = (currentIndex + 1) % _supportedLanguages.Count;

        CurrentLanguage = _supportedLanguages[nextIndex];

        // Save preference
        Preferences.Set("AppLanguage", CurrentLanguage.Code);

        // Apply new language
        ApplyLanguage();

        // Notify UI
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyLanguage()
    {
        var culture = new CultureInfo(CurrentLanguage.Code);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    /// <summary>
    /// Gets the localized user-friendly name for a project type.
    /// </summary>
    /// <param name="technicalTypeName">The technical class name (e.g., "FloorTileProject").</param>
    /// <returns>Localized display name (e.g., "Floor Texture" or "Texture de sol").</returns>
    public string GetLocalizedName(string technicalTypeName)
    {
        if (string.IsNullOrWhiteSpace(technicalTypeName))
            return technicalTypeName;

        if (!_typeToResourceKey.TryGetValue(technicalTypeName, out var resourceKey))
            return technicalTypeName; // Fallback to technical name if not found

        return Resources.Strings.AppResources.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture)
               ?? technicalTypeName;
    }

    /// <summary>
    /// Gets all registered project types with their localized names.
    /// </summary>
    /// <param name="technicalTypeNames">Collection of technical type names.</param>
    /// <returns>Dictionary mapping technical names to localized names.</returns>
    public Dictionary<string, string> GetLocalizedNames(IEnumerable<string> technicalTypeNames)
    {
        return technicalTypeNames.ToDictionary(
            name => name,
            name => GetLocalizedName(name));
    }

    /// <summary>
    /// Gets a localized string by resource key.
    /// </summary>
    /// <param name="resourceKey">The resource key (e.g., "ProjectManagement_Title").</param>
    /// <returns>Localized string or key if not found.</returns>
    public string GetLocalizedString(string resourceKey)
    {
        return Resources.Strings.AppResources.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture)
               ?? resourceKey;
    }
}

/// <summary>
/// Information about a supported language.
/// </summary>
public record LanguageInfo(string Code, string FlagImageSource, string Name);
