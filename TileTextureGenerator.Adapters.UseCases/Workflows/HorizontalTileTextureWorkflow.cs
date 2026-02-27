using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Adapters.UseCases.Registries;
using TileTextureGenerator.Adapters.Persistence.Ports.Output;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Adapters.UseCases.Workflows;

/// <summary>
/// Workflow implementation for horizontal tile texture generation
/// </summary>
[WorkflowFor(typeof(HorizontalTileTextureProjectEntity))]
internal class HorizontalTileTextureWorkflow : IProjectWorkflow
{
    private readonly IImageSelectionService _imageSelectionService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IProjectPersister _projectPersister;

    public HorizontalTileTextureWorkflow(
        IImageSelectionService imageSelectionService,
        IImageProcessingService imageProcessingService,
        IProjectPersister projectPersister)
    {
        _imageSelectionService = imageSelectionService;
        _imageProcessingService = imageProcessingService;
        _projectPersister = projectPersister;
    }

    public async Task InitializeAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

        // Check if already initialized (has source image)
        if (!string.IsNullOrEmpty(horizontalProject.SourceImagePath))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Workflow] Project already initialized, skipping image selection");
#endif
            // Already initialized, just continue
            await ContinueWorkAsync(project);
            return;
        }

        // 1. Let user select an image
        var rawImageData = await _imageSelectionService.PickImageFromFileAsync();

        if (rawImageData == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Workflow] User cancelled image selection");
#endif
            return; // User cancelled
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Image selected, size: {rawImageData.Length} bytes");
#endif

        // 2. Convert to PNG (keeping original resolution)
        var pngImageData = _imageProcessingService.ConvertToPng(rawImageData);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Image converted to PNG, size: {pngImageData.Length} bytes");
#endif

        // 3. Save to Sources/SourceImage.png
        var sourcePath = await _projectPersister.SaveSourceImageAsync(
            horizontalProject.Name, 
            pngImageData, 
            "SourceImage.png");

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Source image saved to: {sourcePath}");
#endif

        // Update project with source image path
        horizontalProject.SourceImagePath = sourcePath;

        // 4. Generate DisplayImage (PNG 256x256)
        horizontalProject.SetDisplayImage(rawImageData, _imageProcessingService);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] DisplayImage generated, size: {horizontalProject.DisplayImage?.Length ?? 0} bytes");
#endif

        // 5. Change status to Pending
        horizontalProject.Status = ProjectStatus.Pending;
        horizontalProject.LastModifiedDate = DateTime.UtcNow;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Initialized horizontal tile project: {project.Name}");
        System.Diagnostics.Debug.WriteLine($"  Source image: {sourcePath}");
        System.Diagnostics.Debug.WriteLine($"  Status: {horizontalProject.Status}");
#endif
    }

    public async Task ContinueWorkAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

        // TODO: Implement continue workflow
        // - Show UI to add/modify/remove image transformations
        // - Apply transformations to generate workspace images

        await Task.CompletedTask;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Continuing horizontal tile project: {project.Name}");
#endif
    }

    public async Task GeneratePdfAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

        // TODO: Implement PDF generation
        // - Compile all transformed textures
        // - Generate PDF in Output folder
        // - Update status to Generated

        await Task.CompletedTask;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Generating PDF for: {project.Name}");
#endif
    }

    public async Task ArchiveAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

        // TODO: Implement archiving
        // - Delete Sources and Workspace folders
        // - Keep only the generated PDF in Output
        // - Update status to Archived

        await Task.CompletedTask;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Archiving project: {project.Name}");
#endif
    }
}
