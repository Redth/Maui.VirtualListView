using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui;

internal class CvLayout : UICollectionViewFlowLayout
{
	public CvLayout(VirtualListViewHandler handler) : base()
	{
		Handler = handler;
		isiOS11 = UIDevice.CurrentDevice.CheckSystemVersion(11, 0);
	}

	readonly VirtualListViewHandler Handler;

	readonly bool isiOS11;

	NFloat GetLayoutFullWidth(UIEdgeInsets sectionInset)
	{
		if (isiOS11)
			return CollectionView.SafeAreaLayoutGuide.LayoutFrame.Width - sectionInset.Left - sectionInset.Right;
		else
			return CollectionView.Bounds.Width - sectionInset.Left - sectionInset.Right;
	}

	NFloat GetLayoutFullHeight(UIEdgeInsets sectionInset)
	{
		if (isiOS11)
			return CollectionView.SafeAreaLayoutGuide.LayoutFrame.Height - sectionInset.Top - sectionInset.Bottom;
		else
			return CollectionView.Bounds.Height - sectionInset.Top - sectionInset.Bottom;
	}

	public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath path)
	{
		var layoutAttributes = base.LayoutAttributesForItem(path);

		PositionInfo? info = null;

		var columns = Handler.VirtualView.Columns;

		if (columns > 1)
		{
			info = Handler?.PositionalViewSelector?.GetInfo(path.Item.ToInt32());
		}

		var sectionInset = SectionInset;

		if (Handler.VirtualView.Orientation == ListOrientation.Vertical)
		{
			var width = GetLayoutFullWidth(sectionInset);
			NFloat gridItemInset = 0f;

			if (columns > 1 && info is not null && info.Kind == PositionKind.Item)
			{
				width = width / columns;

				// Index 0 is first item in grid's row, so 0 additional inset
				// for every next item, we need to add width of previous item
				gridItemInset = width * (info.ItemIndex % columns);
			}

			layoutAttributes.Frame = new CGRect(sectionInset.Left + gridItemInset, layoutAttributes.Frame.Y, width, layoutAttributes.Frame.Height);
		}
		else
		{
			var height = GetLayoutFullHeight(sectionInset);
			NFloat gridItemInset = 0f;

			if (columns > 1 && info is not null && info.Kind == PositionKind.Item)
			{
				height = height / columns;

				// Index 0 is first item in grid's column, so 0 additional inset
				// for every next item, we need to add height of previous item
				gridItemInset = height * info.ItemIndex;
			}

			layoutAttributes.Frame = new CGRect(layoutAttributes.Frame.X, sectionInset.Top + gridItemInset, layoutAttributes.Frame.Width, height);
		}

		return layoutAttributes;
	}

	// public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
	// {
	// 	var layoutAttributesObjects = base.LayoutAttributesForElementsInRect(rect);
	//
	// 	foreach (var layoutAttributes in layoutAttributesObjects)
	// 	{
	// 		if (layoutAttributes.RepresentedElementCategory == UICollectionElementCategory.Cell)
	// 		{
	// 			var newFrame = LayoutAttributesForItem(layoutAttributes.IndexPath).Frame;
	// 			layoutAttributes.Frame = newFrame;
	// 		}
	// 	}
	//
	// 	return layoutAttributesObjects;
	// }
	
	public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
	{
		var layoutAttributesObjects = base.LayoutAttributesForElementsInRect(rect);
		
		foreach (var layoutAttributes in layoutAttributesObjects)
		{
			if (layoutAttributes.RepresentedElementCategory == UICollectionElementCategory.Cell)
			{
				var newFrame = LayoutAttributesForItem(layoutAttributes.IndexPath).Frame;
				layoutAttributes.Frame = newFrame;
			}
		}

		return layoutAttributesObjects;
	}
}
