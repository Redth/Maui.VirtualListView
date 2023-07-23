namespace VirtualListViewSample;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App() : base()
	{
#if WINDOWS
		System.Diagnostics.Debugger.Launch();
#endif

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState activationState)
	{
		return new Microsoft.Maui.Controls.Window(new NavigationPage(new MainPage()));
	}
}
