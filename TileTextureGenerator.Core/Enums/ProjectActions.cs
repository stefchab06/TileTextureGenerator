namespace TileTextureGenerator.Core.Enums;

/// <summary>
/// Flags enum representing available actions for a project.
/// Multiple actions can be available simultaneously.
/// </summary>
[Flags]
public enum ProjectActions
{
    None = 0,
    Load = 1 << 0,      // 1 - Can load/open the project
    Generate = 1 << 1,  // 2 - Can generate PDF
    Archive = 1 << 2,   // 4 - Can archive the project
    Delete = 1 << 3     // 8 - Can delete the project
}
