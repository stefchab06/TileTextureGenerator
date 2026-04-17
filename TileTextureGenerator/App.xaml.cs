using Microsoft.Extensions.DependencyInjection;

namespace TileTextureGenerator;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

#if WINDOWS
		// Initialize Windows close handler to intercept X button
		window.Created += (s, e) =>
		{
			Platforms.Windows.WindowsCloseHandler.Initialize(window);
		};
#endif
		return window;
	}

}
