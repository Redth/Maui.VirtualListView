using Android.Views;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, SwipeRefreshLayout>
	{
		SwipeRefreshLayout swipeRefreshLayout;
		RvAdapter adapter;
		RecyclerView recyclerView;
		LinearLayoutManager layoutManager;
		PositionalViewSelector positionalViewSelector;

		protected override SwipeRefreshLayout CreatePlatformView()
		{
			recyclerView ??= new RecyclerView(Context);

			if (swipeRefreshLayout is null)
			{
				swipeRefreshLayout = new SwipeRefreshLayout(Context);
				swipeRefreshLayout.AddView(recyclerView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			}

			return swipeRefreshLayout;
		}

		protected override void ConnectHandler(SwipeRefreshLayout nativeView)
		{
			swipeRefreshLayout.SetOnRefreshListener(new SrlRefreshListener(() =>
			{
				VirtualView?.Refresh();
				swipeRefreshLayout.Refreshing = false;
			}));

			layoutManager = new LinearLayoutManager(Context);
			//layoutManager.Orientation = LinearLayoutManager.Horizontal;

			positionalViewSelector = new PositionalViewSelector(VirtualView);

			adapter = new RvAdapter(Context, this, positionalViewSelector);
			
			recyclerView.AddOnScrollListener(new RvScrollListener((rv, dx, dy) =>
			{
				var x = Context.FromPixels(dx);
				var y = Context.FromPixels(dy);
				
				VirtualView?.Scrolled(new ScrolledEventArgs(x, y));
			}));

			recyclerView.SetLayoutManager(layoutManager);
			recyclerView.SetAdapter(adapter);
			recyclerView.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
		}

		protected override void DisconnectHandler(SwipeRefreshLayout nativeView)
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
			positionalViewSelector.Reset();
			adapter?.Reset();
			adapter?.NotifyDataSetChanged();
		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.InvalidateData();

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

		public static void MapSetSelected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] itemPositions)
				UpdateSelection(handler, itemPositions, true);
		}

		public static void MapSetDeselected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] itemPositions)
				UpdateSelection(handler, itemPositions, false);
		}

		static void UpdateSelection(VirtualListViewHandler handler, ItemPosition[] itemPositions, bool selected)
		{
			foreach (var itemPosition in itemPositions)
			{
				var position = handler.positionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

				var vh = handler.recyclerView.FindViewHolderForAdapterPosition(position);

				if (vh is RvItemHolder rvh)
				{
					rvh.PositionInfo.IsSelected = selected;

					if (rvh.ViewContainer?.VirtualView is IPositionInfo viewPositionInfo)
						viewPositionInfo.IsSelected = selected;
				}
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
			handler.adapter.NotifyDataSetChanged();
		}

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
}