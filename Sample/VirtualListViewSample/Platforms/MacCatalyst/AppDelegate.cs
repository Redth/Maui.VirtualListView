using Foundation;
using Microsoft.Maui;

namespace VirtualListViewSample
{
	[Register("AppDelegate")]
	public class AppDelegate : MauiUIApplicationDelegate
	{
		protected override MauiApp CreateMauiApp()
			=> MauiProgram.Create();
	}
}