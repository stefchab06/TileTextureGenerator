using TileTextureGenerator.Frontend.UI.Services;

public static class ProjectTypeExtensions
{
    private const string ResourcePrefix = "ProjectType_";

    // Uses the UI localization service singleton; prefer DI in real code.
    public static string ToLocalizedString(this string projectType)
    {
        var key = ResourcePrefix + projectType;
        // LocalizationService returns the localized value or the key as fallback.
        return LocalizationService.Instance.GetString(key);
    }
}