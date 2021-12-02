using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace VirtualListViewSample
{
	[Register("AppDelegate")]
	public class AppDelegate : MauiUIApplicationDelegate
	{
		protected override MauiApp CreateMauiApp()
			=> MauiProgram.Create();
	}
}