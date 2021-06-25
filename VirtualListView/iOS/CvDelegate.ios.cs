using System;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvDelegate : UICollectionViewDelegateFlowLayout
	{

		public CvDelegate(VirtualListViewRenderer renderer)
			: base()
		{
			Renderer = renderer;
		}

		internal VirtualListViewRenderer Renderer { get; }

		public Action<nfloat, nfloat> ScrollHandler { get; set; }

		public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
			=> HandleSelection(collectionView, indexPath, true);

		public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
			=> HandleSelection(collectionView, indexPath, false);

		void HandleSelection(UICollectionView collectionView, NSIndexPath indexPath, bool selected)
		{
			var real = Renderer?.TemplateSelector?.GetRealIndexPath(Renderer?.Adapter, indexPath.Section, (int)indexPath.Item);

			var realSectionIndex = real?.realSectionIndex ?? -1;
			var realItemIndex = real?.realItemIndex ?? -1;

			if (realItemIndex < 0 || realSectionIndex < 0)
				return;

			if (selected)
				Renderer.Element.SetSelected(new ItemPosition(realSectionIndex, realItemIndex));
			else
				Renderer.Element.SetDeselected(new ItemPosition(realSectionIndex, realItemIndex));

			if (Renderer.DataSource.GetCell(collectionView, indexPath) is CvCell cell
				&& cell?.ViewCell != null)
				cell.ViewCell.IsSelected = selected;
		}

		public override void Scrolled(UIScrollView scrollView)
			=> ScrollHandler?.Invoke(scrollView.ContentOffset.X, scrollView.ContentOffset.Y);

		public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
			=> IsRealItem(indexPath);

		public override bool ShouldDeselectItem(UICollectionView collectionView, NSIndexPath indexPath)
			=> IsRealItem(indexPath);

		bool IsRealItem(NSIndexPath indexPath)
		{
			var real = Renderer?.TemplateSelector?.GetRealIndexPath(Renderer?.Adapter, indexPath.Section, (int)indexPath.Item);

			if ((real?.realItemIndex ?? -1) < 0 || (real?.realSectionIndex ?? -1) < 0)
				return false;

			return true;
		}
	}
}
