using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
    class RvScrollListener : RecyclerView.OnScrollListener
	{
		public RvScrollListener(Action<RecyclerView, int, int> scrollHandler, Action userScroll)
		{
			ScrollHandler = scrollHandler;
			UserScroll = userScroll;
		}

		Action<RecyclerView, int, int> ScrollHandler { get; }
		Action UserScroll { get; }

		public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
		{
			base.OnScrollStateChanged(recyclerView, newState);
			if (newState == RecyclerView.ScrollStateDragging)
				UserScroll();
		}

		public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
		{
			base.OnScrolled(recyclerView, dx, dy);

			ScrollHandler?.Invoke(recyclerView, dx, dy);
		}
	}
}