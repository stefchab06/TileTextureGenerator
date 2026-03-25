using Microsoft.Extensions.Logging;
using TileTextureGenerator.Configuration;

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

		// Auto-register all services by conventions
		// This scans all TileTextureGenerator assemblies and registers:
		// - Services, Stores, Repositories, Use Cases
		// - ViewModels and Views (when they exist)
		builder.Services.AddAutoRegisteredServices();

		// Initialize Core registries with DI factories
		// This enables polymorphic instantiation for Projects and Transformations
		builder.Services.InitializeCoreRegistries();

		return builder.Build();
	}
}
