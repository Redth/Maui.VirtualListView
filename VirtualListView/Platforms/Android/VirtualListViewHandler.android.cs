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
	SrlRefreshListener refreshListener;
	RvScrollListener scrollListener;
	RvRecycledViewPool recycledViewPool;

	protected override FrameLayout CreatePlatformView()
	{
		rootLayout = new FrameLayout(Context);
		recyclerView = new RecyclerView(Context);
		swipeRefreshLayout = new SwipeRefreshLayout(Context);
		swipeRefreshLayout.AddView(recyclerView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
		rootLayout.AddView(swipeRefreshLayout, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

		return rootLayout;
	}

	protected override void ConnectHandler(FrameLayout nativeView)
	{
		if (IsConnected && VirtualView.IsRefreshEnabled)
			return;
		base.ConnectHandler(nativeView);
		refreshListener = new SrlRefreshListener(() =>
		{
			if (IsConnected)
				BeginRefresh(() => swipeRefreshLayout.Refreshing = true, () => swipeRefreshLayout.Refreshing = false);
		});
		swipeRefreshLayout.SetOnRefreshListener(refreshListener);

		layoutManager = new LinearLayoutManager(Context);
		//layoutManager.Orientation = LinearLayoutManager.Horizontal;

		PositionalViewSelector = new PositionalViewSelector(VirtualView);

		adapter = new RvAdapter(Context, this, PositionalViewSelector);
		recycledViewPool = new RvRecycledViewPool(() => recyclerView.ChildCount);
		recyclerView.SetRecycledViewPool(recycledViewPool);
		
		scrollListener = new RvScrollListener((rv, dx, dy) =>
		{
			var x = Context.FromPixels(dx);
			var y = Context.FromPixels(dy);
			
			if (IsConnected)
				VirtualView?.Scrolled(x, y);
		});
		recyclerView.AddOnScrollListener(scrollListener);

		recyclerView.SetLayoutManager(layoutManager);
		recyclerView.SetAdapter(adapter);
		recyclerView.LayoutParameters = new ViewGroup.LayoutParams(
			ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
		ConnectLifetime();
	}

	protected override void DisconnectHandler(FrameLayout nativeView)
	{
		DisconnectLifetime();
		swipeRefreshLayout.SetOnRefreshListener(null);
		refreshListener?.Dispose();
		refreshListener = null;
		if (scrollListener is not null)
			recyclerView.RemoveOnScrollListener(scrollListener);
		scrollListener?.Dispose();
		scrollListener = null;
		recyclerView.SetAdapter(null);
		recycledViewPool?.Clear();
		recyclerView.SetRecycledViewPool(null);
		recycledViewPool?.Dispose();
		recycledViewPool = null;
		adapter?.Dispose();
		adapter = null;
		recyclerView.SetLayoutManager(null);
		layoutManager?.Dispose();
		layoutManager = null;
		emptyView?.RemoveFromParent();
		emptyView = null;
		base.DisconnectHandler(nativeView);
	}

	void PlatformInvalidateData()
	{
		UpdateEmptyViewVisibility();
		adapter?.Reset();
		adapter?.NotifyDataSetChanged();
	}

	void PlatformResetTemplates() => adapter?.ResetTemplates();

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		if (position < 0)
			return;

		recyclerView.ScrollToPosition(position);
	}

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{ }

	public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object parameter)
		=> handler.InvalidateData();

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		if (position < 0)
			return;

		var vh = recyclerView.FindViewHolderForAdapterPosition(position);

		if (vh is RvItemHolder rvh && rvh.PositionInfo is not null)
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
		if (!virtualListView.IsRefreshEnabled)
			handler.EndRefresh();
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
		}

		emptyView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

		if (emptyView is not null)
		{
			this.rootLayout.AddView(emptyView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			UpdateEmptyViewVisibility();
		}
	}
	
	ScrollBarVisibility _defaultHorizontalScrollVisibility = ScrollBarVisibility.Default;
	ScrollBarVisibility _defaultVerticalScrollVisibility = ScrollBarVisibility.Default;

	void UpdateVerticalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		if (_defaultVerticalScrollVisibility == ScrollBarVisibility.Default)
			_defaultVerticalScrollVisibility =
				recyclerView.VerticalScrollBarEnabled ? ScrollBarVisibility.Always : ScrollBarVisibility.Never;

		var newVerticalScrollVisiblility = scrollBarVisibility;

		if (newVerticalScrollVisiblility == ScrollBarVisibility.Default)
			newVerticalScrollVisiblility = _defaultVerticalScrollVisibility;

		recyclerView.VerticalScrollBarEnabled = newVerticalScrollVisiblility == ScrollBarVisibility.Always;
	}
	
	void UpdateHorizontalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		if (_defaultHorizontalScrollVisibility == ScrollBarVisibility.Default)
			_defaultHorizontalScrollVisibility =
				recyclerView.HorizontalScrollBarEnabled ? ScrollBarVisibility.Always : ScrollBarVisibility.Never;

		var newHorizontalScrollVisiblility = scrollBarVisibility;

		if (newHorizontalScrollVisiblility == ScrollBarVisibility.Default)
			newHorizontalScrollVisiblility = _defaultHorizontalScrollVisibility;

		recyclerView.HorizontalScrollBarEnabled = newHorizontalScrollVisiblility == ScrollBarVisibility.Always;
	}
	
	public IReadOnlyList<IPositionInfo> FindVisiblePositions()
	{
		var positions = new List<IPositionInfo>();
		
		var firstVisibleItemPosition = layoutManager.FindFirstVisibleItemPosition();
		var lastVisibleItemPosition = layoutManager.FindLastVisibleItemPosition();

		for (var p = firstVisibleItemPosition; p <= lastVisibleItemPosition; p++)
		{
			if (PositionalViewSelector.GetInfo(p) is PositionInfo info)
				positions.Add(info);
		}

		return positions;
	}
}