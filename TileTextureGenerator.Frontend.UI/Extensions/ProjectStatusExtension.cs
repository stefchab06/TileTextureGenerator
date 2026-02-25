using TileTextureGenerator.Frontend.UI.Services;

public static class ProjectStatusExtensions
{
    private const string ResourcePrefix = "ProjectStatus_";

    // Uses the UI localization service singleton; prefer DI in real code.
    public static string ToLocalizedString(this string projectStatus)
    {
        var key = ResourcePrefix + projectStatus;
        // LocalizationService returns the localized value or the key as fallback.
        return LocalizationService.Instance.GetString(key);
    }
}