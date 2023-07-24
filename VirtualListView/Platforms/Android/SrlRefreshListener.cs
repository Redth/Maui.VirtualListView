using AndroidX.SwipeRefreshLayout.Widget;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
    class SrlRefreshListener : Java.Lang.Object, SwipeRefreshLayout.IOnRefreshListener
	{
		public SrlRefreshListener(Action refreshHandler)
		{
			RefreshHandler = refreshHandler;
		}
		Action RefreshHandler { get; }

		public void OnRefresh()
			=> RefreshHandler?.Invoke();
	}
}