#nullable enable
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, UICollectionView>
{
	CvDataSource? dataSource;
	UICollectionViewLayout? layout;
	CvDelegate? cvdelegate;
	UIRefreshControl? refreshControl;

	protected override UICollectionView CreatePlatformView()
	{
		// layout = new (this);
		// layout.ScrollDirection = VirtualView.Orientation switch
		// {
		// 	ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
		// 	ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
		// 	_ => UICollectionViewScrollDirection.Vertical
		// };
		// layout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;
		// layout.ItemSize = UICollectionViewFlowLayout.AutomaticSize;
		// layout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
		// layout.MinimumInteritemSpacing = 0f;
		// layout.MinimumLineSpacing = 0f;
		
		CreateLayout(this, VirtualView);

		var collectionView = new UICollectionView(CGRect.Empty, layout);
		//collectionView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
		collectionView.AllowsMultipleSelection = false;
		collectionView.AllowsSelection = false;


		refreshControl = new UIRefreshControl();
		refreshControl.Enabled = VirtualView?.IsRefreshEnabled ?? false;
		refreshControl.AddTarget(new EventHandler((s, a) =>
		{
			refreshControl.BeginRefreshing();
			try
			{
				VirtualView?.Refresh(() => refreshControl.EndRefreshing());
			}
			catch
			{
				refreshControl.EndRefreshing();
			}
		}), UIControlEvent.ValueChanged);

		//collectionView.AddSubview(refreshControl);

		collectionView.AlwaysBounceVertical = true;

		//collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.ScrollIndicatorInsets = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.AutomaticallyAdjustsScrollIndicatorInsets = false;

		return collectionView;
	}

	protected override void ConnectHandler(UICollectionView nativeView)
	{
		base.ConnectHandler(nativeView);

		//PositionalViewSelector = new PositionalViewSelector(VirtualView);

		dataSource = new CvDataSource(this);
		
		cvdelegate = new CvDelegate(this, nativeView);
		cvdelegate.ScrollHandler = (x, y) => VirtualView?.Scrolled(x, y);

		nativeView.DataSource = dataSource;
		nativeView.Delegate = cvdelegate;
		
		nativeView.ReloadData();
	}

	protected override void DisconnectHandler(UICollectionView nativeView)
	{
		if (dataSource is not null)
		{
			dataSource.Dispose();
			dataSource = null;
		}

		if (cvdelegate is not null)
		{
			cvdelegate.Dispose();
			cvdelegate = null;
		}

		if (refreshControl is not null)
		{
			refreshControl.RemoveFromSuperview();
			refreshControl.Dispose();
			refreshControl = null;
		}


		nativeView.Dispose();

		if (layout is not null)
		{
			layout.Dispose();
			layout = null;
		}

		base.DisconnectHandler(nativeView);
	}

	internal CvCell? GetCell(NSIndexPath indexPath)
		=> dataSource?.GetCell(PlatformView, indexPath) as CvCell;

	public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
	}

	public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		=> handler?.InvalidateData();

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		// var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;
		//
		// if (realIndex < 0)
		// 	return;

		var indexPath = NSIndexPath.FromItemSection(itemPosition.ItemIndex, itemPosition.SectionIndex);

		PlatformView.ScrollToItem(indexPath, UICollectionViewScrollPosition.Top, animated);
	}

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		// var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;
		//
		// if (realIndex < 0)
		// 	return;

		var cell = PlatformView.CellForItem(NSIndexPath.FromItemSection(itemPosition.ItemIndex, itemPosition.SectionIndex));

		if (cell is CvCell cvcell)
		{
			PlatformView.InvokeOnMainThread(() =>
			{
				cvcell.UpdateSelected(selected);
			});
		}
	}

	public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		CreateLayout(handler, virtualListView);
		// if (handler.layout is not null)
		// {
		// 	handler.layout.ScrollDirection = virtualListView.Orientation switch
		// 	{
		// 		ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
		// 		ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
		// 		_ => UICollectionViewScrollDirection.Vertical
		// 	};
		// }

		handler.InvalidateData();
	}

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (virtualListView.RefreshAccentColor is not null && handler.refreshControl is not null)
			handler.refreshControl.TintColor = virtualListView.RefreshAccentColor.ToPlatform();
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		var isRefreshEnabled = virtualListView?.IsRefreshEnabled ?? false;
		if (handler.refreshControl is not null)
		{
			if (isRefreshEnabled)
			{
				handler.PlatformView.AddSubview(handler.refreshControl);
				handler.refreshControl.Enabled = true;
			}
			else
			{
				handler.refreshControl.Enabled = false;
				handler.refreshControl.RemoveFromSuperview();
			}
		}
	}


	public static void MapEmptyView(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateEmptyView();
	}

	void UpdateEmptyViewVisibility()
	{
		if (PlatformView is not null && PlatformView.BackgroundView is not null)
		{
			var visibility = ShouldShowEmptyView ? Visibility.Visible : Visibility.Collapsed;

			PlatformView.BackgroundView?.UpdateVisibility(visibility);
		}
	}

	void UpdateEmptyView()
	{
		if (PlatformView is not null)
		{
			if (PlatformView.BackgroundView is not null)
			{
				PlatformView.BackgroundView.RemoveFromSuperview();
				PlatformView.BackgroundView.Dispose();
			}

			if (MauiContext is not null)
				PlatformView.BackgroundView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

			UpdateEmptyViewVisibility();
		}
	}

	void UpdateVerticalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		PlatformView.ShowsVerticalScrollIndicator = scrollBarVisibility == ScrollBarVisibility.Always || scrollBarVisibility == ScrollBarVisibility.Default;
	}
	
	void UpdateHorizontalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		PlatformView.ShowsHorizontalScrollIndicator = scrollBarVisibility == ScrollBarVisibility.Always || scrollBarVisibility == ScrollBarVisibility.Default;
	}

	public void InvalidateData()
	{
		this.PlatformView.InvokeOnMainThread(() => {
			//layout?.InvalidateLayout();

			UpdateEmptyViewVisibility();

			//PlatformView?.SetNeedsLayout();
			PlatformView?.ReloadData();
			//PlatformView?.LayoutIfNeeded();
		});
		
	}

	static void CreateLayout(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (virtualListView.Columns > 1)
		{
			handler.layout = LayoutFactory.CreateGrid(virtualListView);
		}
		else
		{
			handler.layout = LayoutFactory.CreateList(virtualListView);
		}

		handler.PlatformView.CollectionViewLayout = handler.layout;
	}

	public static void MapColumns(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		CreateLayout(handler, virtualListView);
		// if (virtualListView.Columns > 1)
		// {
		// 	
		// }
	
	public IReadOnlyList<IPositionInfo> FindVisiblePositions()
	{
		var positions = new List<PositionInfo>();
			
		foreach (var cell in PlatformView.VisibleCells)
		{
			if (cell is CvCell cvCell)
				positions.Add(cvCell.PositionInfo);
		}

		return positions;
	}
}
