using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, UICollectionView>
{
	CvDataSource dataSource;
	CvLayout layout;
	CvDelegate cvdelegate;
	UICollectionView collectionView;
	UIRefreshControl refreshControl;

	protected override UICollectionView CreatePlatformView()
	{
		layout = new (this);
		layout.ScrollDirection = VirtualView.Orientation switch
		{
			ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
			ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
			_ => UICollectionViewScrollDirection.Vertical
		};
		layout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;
		layout.ItemSize = UICollectionViewFlowLayout.AutomaticSize;
		layout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
		layout.MinimumInteritemSpacing = 0f;
		layout.MinimumLineSpacing = 0f;

		collectionView = new UICollectionView(CGRect.Empty, layout);
		//collectionView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
		collectionView.AllowsMultipleSelection = false;
		collectionView.AllowsSelection = false;


		refreshControl = new UIRefreshControl();
		refreshControl.AddTarget(new EventHandler((s, a) =>
		{
			refreshControl.BeginRefreshing();
			VirtualView?.Refresh(() => refreshControl.EndRefreshing());
		}), UIControlEvent.ValueChanged);

		collectionView.AddSubview(refreshControl);

		collectionView.AlwaysBounceVertical = true;

		//collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.ScrollIndicatorInsets = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.AutomaticallyAdjustsScrollIndicatorInsets = false;

		return collectionView;
	}

	protected override void ConnectHandler(UICollectionView nativeView)
	{
		base.ConnectHandler(nativeView);

		PositionalViewSelector = new PositionalViewSelector(VirtualView);

		dataSource = new CvDataSource(this);
		
		cvdelegate = new CvDelegate(this, collectionView);
		cvdelegate.ScrollHandler = (x, y) =>
			VirtualView?.Scrolled(x, y);

		collectionView.DataSource = dataSource;
		collectionView.Delegate = cvdelegate;
		
		collectionView.ReloadData();
	}

	protected override void DisconnectHandler(UICollectionView nativeView)
	{
		collectionView.DataSource = null;
		dataSource.Dispose();
		dataSource = null;

		collectionView.Delegate = null;
		cvdelegate.Dispose();
		cvdelegate = null;

		refreshControl.RemoveFromSuperview();
		refreshControl.Dispose();
		refreshControl = null;

		collectionView.Dispose();
		collectionView = null;
		
		layout.Dispose();
		layout = null;

		base.DisconnectHandler(nativeView);
	}

	internal CvCell GetCell(NSIndexPath indexPath)
		=> dataSource?.GetCell(collectionView, indexPath) as CvCell;

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

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;

		if (realIndex < 0)
			return;

		var cell = collectionView.CellForItem(NSIndexPath.FromItemSection(realIndex, 0));

		if (cell is CvCell cvcell)
		{
			cvcell.PositionInfo.IsSelected = selected;

			if (cvcell.VirtualView is IPositionInfo positionInfo)
			{
				collectionView.InvokeOnMainThread(() =>
				{
					positionInfo.IsSelected = selected;
				});
			}
		}
	}

	public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.layout.ScrollDirection = virtualListView.Orientation switch
		{
			ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
			ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
			_ => UICollectionViewScrollDirection.Vertical
		};

		handler?.InvalidateData();
	}

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (virtualListView.RefreshAccentColor is not null)
			handler.refreshControl.TintColor = virtualListView.RefreshAccentColor.ToPlatform();
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.refreshControl.Enabled = virtualListView.IsRefreshEnabled;
	}


	public static void MapEmptyView(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateEmptyView();
	}

	void UpdateEmptyViewVisibility()
	{
		if (collectionView is not null && collectionView.BackgroundView is not null)
		{
			var visibility = ShouldShowEmptyView ? Visibility.Visible : Visibility.Collapsed;

			collectionView.BackgroundView?.UpdateVisibility(visibility);
		}
	}

	void UpdateEmptyView()
	{
		if (collectionView is not null)
		{
			if (collectionView.BackgroundView is not null)
			{
				collectionView.BackgroundView.RemoveFromSuperview();
				collectionView.BackgroundView.Dispose();
			}

			collectionView.BackgroundView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

			UpdateEmptyViewVisibility();
		}
	}

	public void InvalidateData()
	{
		this.PlatformView.InvokeOnMainThread(() => {
			layout?.InvalidateLayout();

			UpdateEmptyViewVisibility();

			collectionView?.SetNeedsLayout();
			collectionView?.ReloadData();
			collectionView?.LayoutIfNeeded();
		});
		
	}
}
