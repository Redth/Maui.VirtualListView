using AndroidX.SwipeRefreshLayout.Widget;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
    class SrlRefreshListener : Java.Lang.Object, SwipeRefreshLayout.IOnRefreshListener, SwipeRefreshLayout.IOnChildScrollUpCallback
	{
		public SrlRefreshListener(Action refreshHandler, Func<bool> canScrollUp)
		{
			RefreshHandler = refreshHandler;
			CanScrollUp = canScrollUp;
		}
		Action RefreshHandler { get; }
		Func<bool> CanScrollUp { get; }

		public void OnRefresh()
			=> RefreshHandler?.Invoke();

		public bool CanChildScrollUp(SwipeRefreshLayout parent, global::Android.Views.View child)
			=> CanScrollUp();
	}
}