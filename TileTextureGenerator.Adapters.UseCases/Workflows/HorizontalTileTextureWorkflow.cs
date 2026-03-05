using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Adapters.UseCases.Registries;
using TileTextureGenerator.Core.Entities;
using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Services;

namespace TileTextureGenerator.Adapters.UseCases.Workflows;

/// <summary>
/// Workflow implementation for horizontal tile texture generation
/// </summary>
[WorkflowFor(typeof(HorizontalTileTextureProjectEntity))]
internal class HorizontalTileTextureWorkflow : IProjectWorkflow
{
    private readonly IImageInitializationService _imageInitializationService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly INavigationService _navigationService;

    public HorizontalTileTextureWorkflow(
        IImageInitializationService imageInitializationService,
        IImageProcessingService imageProcessingService,
        INavigationService navigationService)
    {
        _imageInitializationService = imageInitializationService;
        _imageProcessingService = imageProcessingService;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

        // Check if already initialized (has source image)
        if (horizontalProject.SourceImage != null && horizontalProject.SourceImage.Length > 0)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Workflow] Project already initialized, skipping image selection");
#endif
            // Already initialized, just continue
            await ContinueWorkAsync(project);
            return;
        }

        // Open the initialization view
        var result = await _imageInitializationService.InitializeImageAsync();

        if (result.WasCancelled || result.ImageData == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Workflow] User cancelled initialization");
#endif
            return;
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Image initialized, size: {result.ImageData.Length} bytes, shape: {result.TileShape}");
#endif

        // Convert to PNG (keeping original resolution)
        var pngImageData = _imageProcessingService.ConvertToPng(result.ImageData);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Image converted to PNG, size: {pngImageData.Length} bytes");
#endif

        // Store the source image in the domain entity (as byte[])
        horizontalProject.SourceImage = pngImageData;
        horizontalProject.TileShape = result.TileShape;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Source image and tile shape stored in domain entity");
#endif

        // Generate DisplayImage (PNG 256x256)
        horizontalProject.SetDisplayImage(result.ImageData, _imageProcessingService);

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] DisplayImage generated, size: {horizontalProject.DisplayImage?.Length ?? 0} bytes");
#endif

        // Change status to Pending
        horizontalProject.Status = ProjectStatus.Pending;
        horizontalProject.LastModifiedDate = DateTime.UtcNow;

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Initialized horizontal tile project: {project.Name}");
        System.Diagnostics.Debug.WriteLine($"  Tile Shape: {horizontalProject.TileShape}");
        System.Diagnostics.Debug.WriteLine($"  Status: {horizontalProject.Status}");
#endif
    }

    public async Task ContinueWorkAsync(TileTextureProjectBase project)
    {
        if (project is not HorizontalTileTextureProjectEntity horizontalProject)
            throw new ArgumentException($"Expected HorizontalTileTextureProjectEntity, got {project.GetType().Name}");

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[Workflow] Continuing horizontal tile project: {project.Name}");
#endif

        // Navigate to transformations management view
        await _navigationService.NavigateToTransformationsManagementAsync(horizontalProject);
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
