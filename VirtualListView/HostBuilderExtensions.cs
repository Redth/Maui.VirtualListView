namespace Microsoft.Maui;

#if ANDROID || MACCATALYST || IOS || WINDOWS
public static class VirtualListViewHostBuilderExtensions
{
	public static MauiAppBuilder UseVirtualListView(this MauiAppBuilder builder)
		=> builder.ConfigureMauiHandlers(handlers =>
			handlers.AddHandler(typeof(IVirtualListView), typeof(VirtualListViewHandler)));
}
#endif
