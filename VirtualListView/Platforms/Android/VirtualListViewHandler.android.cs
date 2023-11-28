using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, FrameLayout>
{
	FrameLayout rootLayout;
	SwipeRefreshLayout swipeRefreshLayout;
	RvAdapter adapter;
	RecyclerView recyclerView;
	LinearLayoutManager layoutManager;
	Android.Views.View emptyView;

	protected override FrameLayout CreatePlatformView()
	{
		rootLayout ??= new FrameLayout(Context);

		recyclerView ??= new RecyclerView(Context);

		if (swipeRefreshLayout is null)
		{
			swipeRefreshLayout = new SwipeRefreshLayout(Context);
			swipeRefreshLayout.AddView(recyclerView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
		}

		rootLayout.AddView(swipeRefreshLayout, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

		return rootLayout;
	}

	protected override void ConnectHandler(FrameLayout nativeView)
	{
		swipeRefreshLayout.SetOnRefreshListener(new SrlRefreshListener(() =>
		{
			VirtualView?.Refresh(() => swipeRefreshLayout.Refreshing = false);
		}));

		layoutManager = new LinearLayoutManager(Context);
		//layoutManager.Orientation = LinearLayoutManager.Horizontal;

		PositionalViewSelector = new PositionalViewSelector(VirtualView);

		adapter = new RvAdapter(Context, this, PositionalViewSelector);
		
		recyclerView.AddOnScrollListener(new RvScrollListener((rv, dx, dy) =>
		{
			var x = Context.FromPixels(dx);
			var y = Context.FromPixels(dy);
			
			VirtualView?.Scrolled(x, y);
		}));

		recyclerView.SetLayoutManager(layoutManager);
		recyclerView.SetAdapter(adapter);
		recyclerView.LayoutParameters = new ViewGroup.LayoutParams(
			ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
	}

	protected override void DisconnectHandler(FrameLayout nativeView)
	{
		recyclerView.ClearOnScrollListeners();
		recyclerView.SetAdapter(null);
		adapter.Dispose();
		adapter = null;
		layoutManager.Dispose();
		layoutManager = null;
	}

	public void InvalidateData()
	{
		UpdateEmptyViewVisibility();
		adapter?.Reset();
		adapter?.NotifyDataSetChanged();
	}

	public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler.InvalidateData();

	public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler.InvalidateData();

	public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler.InvalidateData();

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{ }

	public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		=> handler.InvalidateData();

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		var vh = recyclerView.FindViewHolderForAdapterPosition(position);

		if (vh is RvItemHolder rvh)
		{
			rvh.PositionInfo.IsSelected = selected;

			if (rvh.ViewContainer?.VirtualView is IPositionInfo viewPositionInfo)
				viewPositionInfo.IsSelected = selected;
		}
	}

	public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.layoutManager.Orientation = virtualListView.Orientation switch
		{
			ListOrientation.Vertical => LinearLayoutManager.Vertical,
			ListOrientation.Horizontal => LinearLayoutManager.Horizontal,
			_ => LinearLayoutManager.Vertical
		};
		handler.InvalidateData();
	}

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (virtualListView.RefreshAccentColor is not null)
			handler.swipeRefreshLayout.SetColorSchemeColors(virtualListView.RefreshAccentColor.ToPlatform());
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.swipeRefreshLayout.Enabled = virtualListView.IsRefreshEnabled;
	}

	public static void MapEmptyView(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateEmptyView();
	}

	void UpdateEmptyViewVisibility()
	{
		if (emptyView is not null)
			emptyView.Visibility = ShouldShowEmptyView ? ViewStates.Visible : ViewStates.Gone;
	}

	void UpdateEmptyView()
	{
		if (emptyView is not null)
		{
			emptyView.RemoveFromParent();
			emptyView.Dispose();
		}

		emptyView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

		if (emptyView is not null)
		{
			this.rootLayout.AddView(emptyView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			UpdateEmptyViewVisibility();
		}
	}
}