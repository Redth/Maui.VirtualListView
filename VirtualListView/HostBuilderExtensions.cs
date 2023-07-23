namespace Microsoft.Maui;

public static class VirtualListViewHostBuilderExtensions
{
	public static MauiAppBuilder UseVirtualListView(this MauiAppBuilder builder)
		=> builder.ConfigureMauiHandlers(handlers =>
			handlers.AddHandler(typeof(IVirtualListView), typeof(VirtualListViewHandler)));
}
