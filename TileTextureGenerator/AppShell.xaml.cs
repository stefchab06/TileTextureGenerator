using TileTextureGenerator.Presentation.UI.Pages;

namespace TileTextureGenerator;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register custom routes for navigation (non-Shell elements)
		Routing.RegisterRoute("EditProjectPage", typeof(EditProjectPage));
		Routing.RegisterRoute("ImageCroppingPage", typeof(ImageCroppingPage));
	}
}
