using Microsoft.Extensions.Logging;
using TileTextureGenerator.Core.Registries;
using TileTextureGenerator.Adapters.UseCases.Registries;
using TileTextureGenerator.Application.DependencyInjection;
using System.Reflection;

namespace TileTextureGenerator.Application
{
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

            // Automatic dependency registration
            ConfigureDependencyInjection(builder.Services);

            // Build the app first to get IServiceProvider
            var app = builder.Build();

            // Initialize registries with service provider
            TextureProjectRegistry.ForceAutoRegistrationFromCore();
            WorkflowRegistry.ForceAutoRegistration(
                Assembly.Load("TileTextureGenerator.Adapters.UseCases"),
                app.Services);

            return app;
        }

        private static void ConfigureDependencyInjection(IServiceCollection services)
        {
            // Load required assemblies
            var assemblies = new[]
            {
                Assembly.Load("TileTextureGenerator.Core"),
                Assembly.Load("TileTextureGenerator.Adapters.UseCases"),
                Assembly.Load("TileTextureGenerator.Adapters.Persistence"),
                Assembly.Load("TileTextureGenerator.Infrastructure.FileSystem"),
                Assembly.Load("TileTextureGenerator.Frontend.UI")
            };

            // Automatic registration based on assemblies
#if DEBUG
            services.RegisterDependenciesFromAssemblies(enableLogging: true, assemblies);
#else
            services.RegisterDependenciesFromAssemblies(assemblies);
#endif

            // Manual registration for Views and ViewModels (MAUI best practice)
            RegisterViewsAndViewModels(services);

            // Alternative: registration by prefix
            // services.RegisterDependenciesFromPrefix("TileTextureGenerator", enableLogging: true);
        }

        private static void RegisterViewsAndViewModels(IServiceCollection services)
        {
            // ViewModels
            services.AddTransient<TileTextureGenerator.Frontend.UI.ViewModels.ProjectSelectorViewModel>();
            services.AddTransient<TileTextureGenerator.Frontend.UI.ViewModels.SingleTileTextureInitializationViewModel>();

            // Views
            services.AddTransient<TileTextureGenerator.Frontend.UI.Views.ProjectSelectorView>();
            services.AddTransient<TileTextureGenerator.Frontend.UI.Views.SingleTileTextureInitializationView>();
        }
    }
}
