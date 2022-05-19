using System;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvDelegate : UICollectionViewDelegateFlowLayout
	{

		public CvDelegate(VirtualListViewHandler handler, UICollectionView collectionView)
			: base()
		{
			Handler = handler;
			NativeCollectionView = collectionView;
		}

		internal readonly UICollectionView NativeCollectionView;
		internal readonly CvDataSource DataSource;
		internal readonly VirtualListViewHandler Handler;

		public Action<NFloat, NFloat> ScrollHandler { get; set; }

		public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
			=> HandleSelection(collectionView, indexPath, true);

		public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
			=> HandleSelection(collectionView, indexPath, false);

		void HandleSelection(UICollectionView collectionView, NSIndexPath indexPath, bool selected)
		{
			UIView.AnimationsEnabled = false;
			var selectedCell = collectionView.CellForItem(indexPath) as CvCell;

			if ((selectedCell?.PositionInfo?.Kind ?? PositionKind.Header) == PositionKind.Item)
			{
				selectedCell.PositionInfo.IsSelected = selected;

				var itemPos = new ItemPosition(
					selectedCell.PositionInfo.SectionIndex,
					selectedCell.PositionInfo.ItemIndex);

				if (selected)
					Handler?.VirtualView?.SetSelected(itemPos);
				else
					Handler?.VirtualView?.SetDeselected(itemPos);
			}

			//var updatedVisibleRect = collectionView.ConvertRectToView(collectionView.Bounds, selectedCell);

			//var contentOffset = collectionView.ContentOffset;
			//contentOffset.X = contentOffset.X + (visibleRect.X - updatedVisibleRect.X);
			//collectionView.ContentOffset = contentOffset;

			//UIView.AnimationsEnabled = true;
		}

		public override void Scrolled(UIScrollView scrollView)
			=> ScrollHandler?.Invoke(scrollView.ContentOffset.X, scrollView.ContentOffset.Y);

		public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
			=> IsRealItem(indexPath);

		public override bool ShouldDeselectItem(UICollectionView collectionView, NSIndexPath indexPath)
			=> IsRealItem(indexPath);

		bool IsRealItem(NSIndexPath indexPath)
		{
			var info = Handler?.PositionalViewSelector?.GetInfo(indexPath.Section, (int)indexPath.Item);
			return (info?.Kind ?? PositionKind.Header) == PositionKind.Item;
		}
	}
}
