using System.Collections.Generic;

namespace TileTextureGenerator.Adapters.UseCases;

/// <summary>
/// Result of the ListProjects use case execution.
/// </summary>
public class ListProjectsResult
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<ProjectListItemDto>? Projects { get; private init; }
    public string? ErrorMessage { get; private init; }

    private ListProjectsResult() { }

    public static ListProjectsResult Success(IReadOnlyList<ProjectListItemDto> projects) => new()
    {
        IsSuccess = true,
        Projects = projects
    };

    public static ListProjectsResult Error(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
