using System.Runtime.InteropServices;
using Foundation;
using UIKit;

namespace Microsoft.Maui;

internal class CvDelegate : UICollectionViewDelegateFlowLayout
{
	public CvDelegate(VirtualListViewHandler handler, UICollectionView collectionView)
		: base()
	{
		Handler = handler;
		NativeCollectionView = new WeakReference<UICollectionView>(collectionView);
	}

	internal readonly WeakReference<UICollectionView> NativeCollectionView;
	internal readonly VirtualListViewHandler Handler;

	public WeakReference<Action<NFloat, NFloat>> ScrollHandler { get; set; }

	public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
		=> HandleSelection(collectionView, indexPath, true);

	public override void ItemDeselected(UICollectionView collectionView, NSIndexPath indexPath)
		=> HandleSelection(collectionView, indexPath, false);

	void HandleSelection(UICollectionView collectionView, NSIndexPath indexPath, bool selected)
	{
		//UIView.AnimationsEnabled = false;
		var selectedCell = collectionView.CellForItem(indexPath) as CvCell;

		if ((selectedCell?.PositionInfo?.Kind ?? PositionKind.Header) == PositionKind.Item)
		{
			selectedCell.UpdateSelected(selected);

			var itemPos = new ItemPosition(
				selectedCell.PositionInfo.SectionIndex,
				selectedCell.PositionInfo.ItemIndex);

			if (selected)
				Handler?.VirtualView?.SelectItem(itemPos);
			else
				Handler?.VirtualView?.DeselectItem(itemPos);
		}
	}

	public override void Scrolled(UIScrollView scrollView)
	{
		if (ScrollHandler?.TryGetTarget(out var handler) ?? false)
			handler?.Invoke(scrollView.ContentOffset.X, scrollView.ContentOffset.Y);
	}

	public override bool ShouldSelectItem(UICollectionView collectionView, NSIndexPath indexPath)
		=> IsRealItem(indexPath);

	public override bool ShouldDeselectItem(UICollectionView collectionView, NSIndexPath indexPath)
		=> IsRealItem(indexPath);

	bool IsRealItem(NSIndexPath indexPath)
	{
		var info = Handler?.PositionalViewSelector?.GetInfo(indexPath.Item.ToInt32());
		return (info?.Kind ?? PositionKind.Header) == PositionKind.Item;
	}
}
