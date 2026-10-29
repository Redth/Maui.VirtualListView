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
	CvLayout? layout;
	CvDelegate? cvdelegate;
	UIRefreshControl? refreshControl;

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

		var collectionView = new UICollectionView(CGRect.Empty, layout);
		//collectionView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
		collectionView.AllowsMultipleSelection = false;
		collectionView.AllowsSelection = false;


		collectionView.AlwaysBounceVertical = true;

		//collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.ScrollIndicatorInsets = new UIEdgeInsets(0, 0, 0, 0);
		//collectionView.AutomaticallyAdjustsScrollIndicatorInsets = false;

		return collectionView;
	}

	protected override void ConnectHandler(UICollectionView nativeView)
	{
		if (IsConnected)
			return;
		base.ConnectHandler(nativeView);
		layout = nativeView.CollectionViewLayout as CvLayout;
		refreshControl = new UIRefreshControl { Enabled = VirtualView.IsRefreshEnabled };
		refreshControl.ValueChanged += RefreshControlValueChanged;
		if (VirtualView.IsRefreshEnabled)
			nativeView.RefreshControl = refreshControl;

		PositionalViewSelector = new PositionalViewSelector(VirtualView);

		dataSource = new CvDataSource(this);
		
		cvdelegate = new CvDelegate(this, nativeView);

		nativeView.DataSource = dataSource;
		nativeView.Delegate = cvdelegate;
		
		ConnectLifetime();
	}

	void RefreshControlValueChanged(object? sender, EventArgs e)
	{
		if (IsConnected && VirtualView.IsRefreshEnabled && refreshControl is not null)
		{
			BeginRefresh(() => refreshControl.BeginRefreshing(), () => refreshControl?.EndRefreshing());
		}
	}

	protected override void DisconnectHandler(UICollectionView nativeView)
	{
		DisconnectLifetime();
		nativeView.WeakDelegate = null;
		nativeView.WeakDataSource = null;
		nativeView.BackgroundView?.RemoveFromSuperview();
		nativeView.BackgroundView = null;
		nativeView.RefreshControl = null;

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
			refreshControl.ValueChanged -= RefreshControlValueChanged;
			refreshControl.RemoveFromSuperview();
			refreshControl.Dispose();
			refreshControl = null;
		}


		layout = null;
		base.DisconnectHandler(nativeView);
	}

	internal CvCell? GetCell(NSIndexPath indexPath)
		=> dataSource?.GetCell(PlatformView, indexPath) as CvCell;

	internal double ContentCrossSize => layout?.CrossSize(VirtualView.Orientation) ?? 0;

	void PlatformInvalidateContentLayout(PositionInfo position)
	{
		if (layout is null || PositionalViewSelector?.GetInfo(position.Position) is null)
			return;
		using var context = new UICollectionViewFlowLayoutInvalidationContext();
		context.InvalidateItems(new[] { NSIndexPath.FromItemSection(position.Position, 0) });
		layout.InvalidateLayout(context);
	}

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
	}

	public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		=> handler?.InvalidateData();

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;

		if (realIndex < 0)
			return;

		var indexPath = NSIndexPath.FromItemSection(realIndex, 0);

		PlatformView.ScrollToItem(indexPath, UICollectionViewScrollPosition.Top, animated);
	}

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		Publish(() =>
		{
			var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;
			if (realIndex < 0)
				return;
			if (PlatformView.CellForItem(NSIndexPath.FromItemSection(realIndex, 0)) is CvCell cvcell)
				cvcell.UpdateSelected(selected);
		});
	}

	public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.Publish(() =>
		{
			if (handler.layout is not null)
			{
				handler.layout.ScrollDirection = virtualListView.Orientation switch
				{
					ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
					ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
					_ => UICollectionViewScrollDirection.Vertical
				};
				handler.layout.InvalidateLayout();
			}
		});
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
				handler.PlatformView.RefreshControl = handler.refreshControl;
				handler.refreshControl.Enabled = true;
			}
			else
			{
				handler.EndRefresh();
				handler.refreshControl.Enabled = false;
				handler.PlatformView.RefreshControl = null;
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

	void PlatformInvalidateData()
	{
		dataSource?.PruneCells();
		UpdateEmptyViewVisibility();
		PlatformView.ReloadData();
	}

	void PlatformResetTemplates() => dataSource?.ResetTemplates();
	
	public IReadOnlyList<IPositionInfo> FindVisiblePositions()
	{
		var positions = new List<PositionInfo>();
			
		foreach (var cell in PlatformView.VisibleCells)
		{
			if (cell is CvCell cvCell && cvCell.PositionInfo is PositionInfo position)
				positions.Add(position);
		}

		return positions;
	}
}
