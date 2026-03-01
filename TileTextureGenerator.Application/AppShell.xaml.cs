using TileTextureGenerator.Frontend.UI.Views;

namespace TileTextureGenerator.Application
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute("SingleTileTextureInitializationView", typeof(SingleTileTextureInitializationView));
        }
    }
}
