using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace VirtualListViewSample
{
	public class MauiProgram
	{
		public static MauiApp Create()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseVirtualListView();
			return builder.Build();
		}
	}
}