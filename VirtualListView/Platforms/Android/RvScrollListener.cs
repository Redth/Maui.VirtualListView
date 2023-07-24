using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
    class RvScrollListener : RecyclerView.OnScrollListener
	{
		public RvScrollListener(Action<RecyclerView, int, int> scrollHandler)
		{
			ScrollHandler = scrollHandler;
		}

		Action<RecyclerView, int, int> ScrollHandler { get; }

		public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
		{
			base.OnScrolled(recyclerView, dx, dy);

			ScrollHandler?.Invoke(recyclerView, dx, dy);
		}
	}
}