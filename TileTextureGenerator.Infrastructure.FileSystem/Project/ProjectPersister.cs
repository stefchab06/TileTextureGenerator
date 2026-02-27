using System.Text.Json;
using TileTextureGenerator.Adapters.Persistence.Dto;
using TileTextureGenerator.Adapters.Persistence.Ports.Output;

namespace TileTextureGenerator.Infrastructure.FileSystem.Project;

internal class ProjectPersister : IProjectPersister
{
    private readonly string _basePath;

    public ProjectPersister()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _basePath = Path.Combine(appData, "TileTextureGenerator");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    private string GetProjectFolderName(string projectName)
    {
        return Path.Combine(_basePath, projectName);
    }

    private string GetProjectFilePath(string projectName)
    {
        return Path.Combine(GetProjectFolderName(projectName), $"{projectName}.json");
    }

    private Boolean CreateProjectFolderStructure(ProjectDataDto project)
    {
        var projectFolder = GetProjectFolderName(project.ProjectName);

        // Check if project already exists
        if (Directory.Exists(projectFolder))
        {
            return false; // Project already exists
        }

        // Create main project folder
        Directory.CreateDirectory(projectFolder);

        // Create sub-folders
        var sourcesFolder = Path.Combine(projectFolder, "Sources");
        var workspaceFolder = Path.Combine(projectFolder, "Workspace");
        var outputFolder = Path.Combine(projectFolder, "Output");

        Directory.CreateDirectory(sourcesFolder);
        Directory.CreateDirectory(workspaceFolder);
        Directory.CreateDirectory(outputFolder);

        // Create project JSON file with content from DTO
        var projectFileName = GetProjectFilePath(project.ProjectName);

        // Use the JSON from the DTO if available, otherwise create basic JSON
        string jsonContent;
        if (!string.IsNullOrEmpty(project.ProjectDataJson))
        {
            jsonContent = project.ProjectDataJson;
        }
        else
        {
            var projectJson = new
            {
                Name = project.ProjectName,
                Type = project.ProjectType,
                Status = project.ProjectStatus
            };
            jsonContent = JsonSerializer.Serialize(projectJson, new JsonSerializerOptions { WriteIndented = true });
        }

        File.WriteAllText(projectFileName, jsonContent);

        return true;
    }

    public async Task<Boolean> CreateProjectAsync(ProjectDataDto project)
    {
        return await Task.Run(() => CreateProjectFolderStructure(project));
    }

    public async Task<Boolean> DeleteProjectAsync(string projectName)
    {
        var folderPath = GetProjectFolderName(projectName);

        return await Task.Run(() =>
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, recursive: true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<ProjectDataDto> LoadProjectAsync(string projectName)
    {
        var projectFilePath = GetProjectFilePath(projectName);

        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(projectFilePath);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        var name = root.GetProperty("Name").GetString() ?? projectName;
        var type = root.GetProperty("Type").GetString() ?? "Unknown";
        var status = root.GetProperty("Status").GetString() ?? "Unexisting";

        // Load DisplayImageFile if present (persistence detail)
        byte[] imageBytes = null;
        string ? displayImageFile = null;
        if (root.TryGetProperty("DisplayImageFile", out var imageElement))
        {
            displayImageFile = imageElement.GetString();
            if (displayImageFile != null)
            {     var absoluteImagePath = Path.Combine(GetProjectFolderName(projectName), displayImageFile);
                if (File.Exists(absoluteImagePath))
                {
                    imageBytes = await File.ReadAllBytesAsync(absoluteImagePath);

                }
            }
        }

        return new ProjectDataDto(name, type, status, jsonContent)
        {
            DisplayImage = imageBytes
        };
    }

    public async Task<bool> SaveProjectAsync(ProjectDataDto project)
    {
        var projectFilePath = GetProjectFilePath(project.ProjectName);
        var projectFolder = GetProjectFolderName(project.ProjectName);

        return await Task.Run(() =>
        {
            try
            {
                // Ensure project folder exists
                if (!Directory.Exists(projectFolder))
                {
                    return false;
                }

                // Parse existing JSON and add/update DisplayImageFile
                string jsonContent;

                if (!string.IsNullOrEmpty(project.ProjectDataJson))
                {
                    var jsonDoc = JsonDocument.Parse(project.ProjectDataJson);
                    var jsonObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(project.ProjectDataJson);

                    if (project.DisplayImage != null)
                    {
                        // Save the image and get the relative path
                        var relativeImagePath = Path.Combine("Workspace", "DisplayImage.png");
                        var absoluteImagePath = Path.Combine(GetProjectFolderName(project.ProjectName), relativeImagePath);
                        File.WriteAllBytesAsync(absoluteImagePath, project.DisplayImage).Wait();
                        jsonObject["DisplayImageFile"] = JsonSerializer.SerializeToElement(relativeImagePath);
                    }

                    jsonContent = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    // Create basic JSON with image file if present
                    var projectData = new Dictionary<string, object>
                    {
                        ["Name"] = project.ProjectName,
                        ["Type"] = project.ProjectType,
                        ["Status"] = project.ProjectStatus
                    };

                    jsonContent = JsonSerializer.Serialize(projectData, new JsonSerializerOptions { WriteIndented = true });
                }


                File.WriteAllText(projectFilePath, jsonContent);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
    public async Task<IList<ProjectDataDto>> GetProjectList()
    {
        return await Task.Run(async () =>
        {
            var projectInfos = new List<ProjectDataDto>();

            if (!Directory.Exists(_basePath))
            {
                return projectInfos;
            }

            // Look for project directories (not just JSON files)
            var projectDirectories = Directory.GetDirectories(_basePath);

            foreach (var projectDir in projectDirectories)
            {
                var projectName = Path.GetFileName(projectDir);
                var projectFile = Path.Combine(projectDir, $"{projectName}.json");

                if (!File.Exists(projectFile))
                {
                    continue; // Skip directories without project file
                }

                try
                {
                    var content = await File.ReadAllTextAsync(projectFile);
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    var name = root.GetProperty("Name").GetString() ?? projectName;
                    var typeValue = root.GetProperty("Type").GetString() ?? "Unknown";
                    var statusValue = root.GetProperty("Status").GetString() ?? "Unexisting";

                    // Load DisplayImageFile if present
                    byte[]? displayImage= null;
                    if (root.TryGetProperty("DisplayImageFile", out var imageElement))
                    {
                        var displayImageFile = imageElement.GetString();
                        if (displayImageFile != null)
                        {
                            var absoluteImagePath = Path.Combine(projectDir, displayImageFile);
                            if (File.Exists(absoluteImagePath))
                            {
                                displayImage = await File.ReadAllBytesAsync(absoluteImagePath);
                            }
                        }
                    }

                    projectInfos.Add(new ProjectDataDto(name, typeValue, statusValue, content)
                    {
                        DisplayImage = displayImage
                    });
                }
                catch
                {
                    // Skip corrupted files
                    continue;
                }
            }

            return projectInfos;
        });
    }

    public async Task<IList<ProjectSummaryDto>> GetProjectSummariesAsync()
    {
        return await Task.Run(async () =>
        {
            var summaries = new List<ProjectSummaryDto>();

            if (!Directory.Exists(_basePath))
            {
                return summaries;
            }

            var projectDirectories = Directory.GetDirectories(_basePath);

            foreach (var projectDir in projectDirectories)
            {
                var projectName = Path.GetFileName(projectDir);
                var projectFile = Path.Combine(projectDir, $"{projectName}.json");

                if (!File.Exists(projectFile))
                {
                    continue;
                }

                try
                {
                    var content = await File.ReadAllTextAsync(projectFile);
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    var name = root.GetProperty("Name").GetString() ?? projectName;
                    var typeValue = root.GetProperty("Type").GetString() ?? "Unknown";
                    var statusValue = root.GetProperty("Status").GetString() ?? "Unexisting";

                    // Load LastModifiedDate
                    var lastModifiedDate = DateTime.UtcNow;
                    if (root.TryGetProperty("LastModifiedDate", out var dateElement))
                    {
                        if (DateTime.TryParse(dateElement.GetString(), out var date))
                        {
                            lastModifiedDate = date;
                        }
                    }

                    // Load DisplayImage if available
                    byte[]? displayImage = null;
                    if (root.TryGetProperty("DisplayImageFile", out var displayImageFileElement))
                    {
                        var displayImageFile = displayImageFileElement.GetString();
                        if (displayImageFile != null)
                        {
                            var absoluteImagePath = Path.Combine(projectDir, displayImageFile);
                            if (File.Exists(absoluteImagePath))
                            {
                                displayImage = await File.ReadAllBytesAsync(absoluteImagePath);
                            }
                        }
                    }

                    summaries.Add(new ProjectSummaryDto(name, typeValue, statusValue, lastModifiedDate)
                    {
                        DisplayImage = displayImage
                    });
                }
                catch
                {
                    // Skip corrupted files
                    continue;
                }
            }

            return summaries;
        });
    }

    public async Task<string> SaveSourceImageAsync(string projectName, byte[] imageData, string filename = "SourceImage.png")
    {
        return await Task.Run(() =>
        {
            var projectFolder = GetProjectFolderName(projectName);
            var sourcesFolder = Path.Combine(projectFolder, "Sources");

            // Ensure Sources folder exists
            if (!Directory.Exists(sourcesFolder))
            {
                Directory.CreateDirectory(sourcesFolder);
            }

            var targetPath = Path.Combine(sourcesFolder, filename);

            // Save the image
            File.WriteAllBytes(targetPath, imageData);

            // Return relative path from project root
            return Path.Combine("Sources", filename);
        });
    }
}
