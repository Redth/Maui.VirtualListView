using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, UICollectionView>
	{
		CvDataSource dataSource;
		CvLayout layout;
		CvDelegate cvdelegate;
		UICollectionView collectionView;
		UIRefreshControl refreshControl;

		internal PositionalViewSelector PositionalViewSelector { get; private set; }
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
				VirtualView?.Refresh();
				refreshControl.EndRefreshing();

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
			dataSource.IsSelectedHandler = (realSection, realIndex) =>
				VirtualView?.IsItemSelected(realSection, realIndex) ?? false;

			cvdelegate = new CvDelegate(this, collectionView);
			cvdelegate.ScrollHandler = (x, y) =>
				VirtualView?.Scrolled(new ScrolledEventArgs(x, y));

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

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

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

		public static void MapSetSelected(VirtualListViewHandler handler, IVirtualListView virtualListView, object parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				UpdateSelected(handler, items, true);
			}
		}

		public static void MapSetDeselected(VirtualListViewHandler handler, IVirtualListView virtualListView, object parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				UpdateSelected(handler, items, false);
			}
		}

		static void UpdateSelected(VirtualListViewHandler handler, ItemPosition[] itemPositions, bool selected)
		{
			
			foreach (var itemPosition in itemPositions)
			{
				var realIndex = handler.PositionalViewSelector.GetIndexPath(itemPosition.SectionIndex, itemPosition.ItemIndex);

				var cell = handler.collectionView.CellForItem(realIndex);

				if (cell is CvCell cvcell)
                {
					cvcell.PositionInfo.IsSelected = selected;

					if (cvcell.VirtualView is IPositionInfo positionInfo)
					{	
						handler.collectionView.InvokeOnMainThread(() =>
						{
							positionInfo.IsSelected = selected;
						});
					}
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

		public void InvalidateData()
		{
			PositionalViewSelector?.Reset();
			dataSource?.Reset(collectionView);
			collectionView?.ReloadData();
			layout?.InvalidateLayout();
		}
	}
}
