using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, UICollectionView>
	{
		CvDataSource dataSource;
		UICollectionViewLayout layout;
		CvDelegate cvdelegate;
		UICollectionView collectionView;

		internal PositionalViewSelector PositionalViewSelector { get; private set; }

		protected override UICollectionView CreateNativeView()
		{
			layout = (VirtualView.Layout ?? new VirtualListViewStackLayout(ListOrientation.Vertical)).CreateNativeLayout();

			collectionView = new UICollectionView(CGRect.Empty, layout);

			return collectionView;
		}
		protected override void ConnectHandler(UICollectionView nativeView)
		{
			base.ConnectHandler(nativeView);

			PositionalViewSelector = new PositionalViewSelector(VirtualView);

			dataSource = new CvDataSource(this);
			dataSource.IsSelectedHandler = (realSection, realIndex) =>
				VirtualView?.IsItemSelected(realSection, realIndex) ?? false;

			cvdelegate = new CvDelegate(this);
			//cvdelegate.ScrollHandler = (x, y) =>
			//	VirtualView?.RaiseScrolled(new ScrolledEventArgs(x, y));

			collectionView.AllowsSelection = VirtualView.SelectionMode != SelectionMode.None;
			collectionView.AllowsMultipleSelection = VirtualView.SelectionMode == SelectionMode.Multiple;
			collectionView.DataSource = dataSource;
			collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
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
			if (handler?.NativeView != null)
			{
				handler.NativeView.AllowsSelection = virtualListView.SelectionMode != SelectionMode.None;
				handler.NativeView.AllowsMultipleSelection = virtualListView.SelectionMode == SelectionMode.Multiple;
			}
		}

		public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapSetSelected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				handler?.collectionView?.ReloadItems(
					items.Select(i => NSIndexPath.FromItemSection(i.ItemIndex, i.SectionIndex)).ToArray());
			}
		}

		public static void MapSetDeselected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items)
			{
				handler?.collectionView?.ReloadItems(
					items.Select(i => NSIndexPath.FromItemSection(i.ItemIndex, i.SectionIndex)).ToArray());
			}
		}

		public static void MapLayout(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{
			handler.layout = (handler.VirtualView.Layout ?? new VirtualListViewStackLayout(ListOrientation.Vertical)).CreateNativeLayout();
			handler.collectionView.CollectionViewLayout = handler.layout;
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
