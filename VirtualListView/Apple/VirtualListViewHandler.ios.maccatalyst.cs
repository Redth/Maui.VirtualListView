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
		
		cvdelegate = new CvDelegate(this, nativeView);
		cvdelegate.ScrollHandler = new WeakReference<Action<System.Runtime.InteropServices.NFloat, System.Runtime.InteropServices.NFloat>>((x, y) =>
			VirtualView?.Scrolled(x, y));

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

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;

		if (realIndex < 0)
			return;

		var indexPath = NSIndexPath.FromItemSection(realIndex, 0);

		PlatformView.ScrollToItem(indexPath, UICollectionViewScrollPosition.Top, animated);
	}
	
	public void PlatformDeleteItems(ItemPosition[] itemPositions)
	{
		var realIndexes = itemPositions.Select(p => PositionalViewSelector.GetPosition(p.SectionIndex, p.ItemIndex));

		PlatformView.InvokeOnMainThread(() =>
		{
			try
			{
				var ristr = string.Join(", ", realIndexes);
				
				Console.WriteLine("ReloadData");
				PlatformView.ReloadData();

				var nsindexes = realIndexes.Select(r => NSIndexPath.FromItemSection(new nint(r), new nint(0))).ToArray();
				
				Console.WriteLine($"DeleteItems: {ristr}");
				PlatformView.DeleteItems(nsindexes);
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex);
			}
		});
	}
	
	public void PlatformDeleteSection(int sectionIndex)
	{
	}
	
	public void PlatformInsertItems(ItemPosition[] itemPositions)
	{
		var realIndexes = itemPositions.Select(p => PositionalViewSelector.GetPosition(p.SectionIndex, p.ItemIndex));

		PlatformView.InvokeOnMainThread(() =>
		{
			try
			{
				var ristr = string.Join(", ", realIndexes);
				
				Console.WriteLine("ReloadData");
				PlatformView.ReloadData();

				var nsindexes = realIndexes.Select(r => NSIndexPath.FromItemSection(new nint(r), new nint(0))).ToArray();
				
				Console.WriteLine($"InsertItems: {ristr}");
				PlatformView.InsertItems(nsindexes);
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR: " + ex);
			}
		});
	}
	
	public void PlatformInsertSection(int sectionIndex)
	{
	}

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var realIndex = PositionalViewSelector?.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex) ?? -1;

		if (realIndex < 0)
			return;

		var cell = PlatformView.CellForItem(NSIndexPath.FromItemSection(realIndex, 0));

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
		if (handler.layout is not null)
		{
			handler.layout.ScrollDirection = virtualListView.Orientation switch
			{
				ListOrientation.Vertical => UICollectionViewScrollDirection.Vertical,
				ListOrientation.Horizontal => UICollectionViewScrollDirection.Horizontal,
				_ => UICollectionViewScrollDirection.Vertical
			};
		}

		handler.PlatformInvalidateData();
	}

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (virtualListView.RefreshAccentColor is not null && handler.refreshControl is not null)
			handler.refreshControl.TintColor = virtualListView.RefreshAccentColor.ToPlatform();
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler.refreshControl is not null)
			handler.refreshControl.Enabled = virtualListView.IsRefreshEnabled;
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

	void PlatformInvalidateData()
	{
		this.PlatformView.InvokeOnMainThread(() => { 
			layout?.InvalidateLayout();

			UpdateEmptyViewVisibility();

			PlatformView?.SetNeedsLayout();
			PlatformView?.ReloadData();
			PlatformView?.LayoutIfNeeded();
		});
		
	}
}
