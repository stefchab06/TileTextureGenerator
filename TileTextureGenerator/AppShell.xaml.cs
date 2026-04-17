using TileTextureGenerator.Presentation.UI.Pages;

namespace TileTextureGenerator;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes for navigation
		Routing.RegisterRoute("ImageCroppingPage", typeof(ImageCroppingPage));
	}
}
