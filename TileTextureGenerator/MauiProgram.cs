using Microsoft.Extensions.Logging;
using TileTextureGenerator.Configuration;
using TileTextureGenerator.Core.Ports.Input;
using TileTextureGenerator.Core.Ports.Output;
using TileTextureGenerator.Core.Services;
using TileTextureGenerator.Adapters.Persistence.Ports;
using TileTextureGenerator.Adapters.Persistence.Stores;
using TileTextureGenerator.Adapters.UseCases;
using TileTextureGenerator.Presentation.UI.Services;
using TileTextureGenerator.Infrastructure.FileSystem;

namespace TileTextureGenerator;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Infrastructure services
		builder.Services.AddSingleton<IFileStorage, FileStorage>();

		// Register Core services (Input Ports implementations)
		builder.Services.AddScoped<IProjectsManager, ProjectsManager>();

		// Register Adapters.Persistence (Output Ports implementations)
		builder.Services.AddScoped<IProjectsStore, JsonProjectsStore>();

		// Register Use Cases
		builder.Services.AddScoped<ManageProjectListUseCase>();

		// Register Presentation services (no interface)
		builder.Services.AddSingleton<ProjectTypeLocalizer>();
		builder.Services.AddSingleton<TransformationTypeLocalizer>();
		builder.Services.AddSingleton<TileShapeLocalizer>();

		// Auto-register remaining services by conventions
		// This scans all TileTextureGenerator assemblies and registers:
		// - ViewModels and Views
		builder.Services.AddAutoRegisteredServices();

		// Initialize Core registries with DI factories
		// This enables polymorphic instantiation for Projects and Transformations
		builder.Services.InitializeCoreRegistries();

		return builder.Build();
	}
}
