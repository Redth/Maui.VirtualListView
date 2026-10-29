using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui;

internal class CvLayout : UICollectionViewFlowLayout
{
	public CvLayout(VirtualListViewHandler handler) : base()
	{
		Handler = new WeakReference<VirtualListViewHandler>(handler);
	}

	readonly WeakReference<VirtualListViewHandler> Handler;

	public override void PrepareLayout()
	{
		if (Handler.TryGetTarget(out var handler) && handler.IsConnected)
		{
			var cross = Math.Max(1, CrossSize(handler.VirtualView.Orientation));
			var estimate = handler.VirtualView.Orientation == ListOrientation.Vertical
				? new CGSize(cross, 44) : new CGSize(44, cross);
			if (EstimatedItemSize != estimate)
				EstimatedItemSize = estimate;
		}
		base.PrepareLayout();
	}

	internal double CrossSize(ListOrientation orientation)
	{
		var bounds = CollectionView.Bounds;
		var inset = CollectionView.AdjustedContentInset;
		return orientation == ListOrientation.Vertical
			? Math.Max(0, bounds.Width - inset.Left - inset.Right - SectionInset.Left - SectionInset.Right)
			: Math.Max(0, bounds.Height - inset.Top - inset.Bottom - SectionInset.Top - SectionInset.Bottom);
	}

	public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath path)
	{
		var layoutAttributes = base.LayoutAttributesForItem(path);
		if (layoutAttributes is null || !Handler.TryGetTarget(out var handler) || !handler.IsConnected)
			return layoutAttributes;

		layoutAttributes = (UICollectionViewLayoutAttributes)layoutAttributes.Copy();
		if (handler.VirtualView.Orientation == ListOrientation.Vertical)
		{
			var x = SectionInset.Left;
			var width = CrossSize(ListOrientation.Vertical);
			layoutAttributes.Frame = new CGRect(x, layoutAttributes.Frame.Y, width, layoutAttributes.Frame.Height);
		}
		else
		{
			var y = SectionInset.Top;

			var height = CrossSize(ListOrientation.Horizontal);
			layoutAttributes.Frame = new CGRect(layoutAttributes.Frame.X, y, layoutAttributes.Frame.Width, height);
		}

		return layoutAttributes;
	}

	public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
	{
		var layoutAttributesObjects = base.LayoutAttributesForElementsInRect(rect);
		if (layoutAttributesObjects is null)
			return layoutAttributesObjects;

		return layoutAttributesObjects.Select(attributes =>
		{
			if (attributes.RepresentedElementCategory == UICollectionElementCategory.Cell)
				return LayoutAttributesForItem(attributes.IndexPath) ?? (UICollectionViewLayoutAttributes)attributes.Copy();
			return (UICollectionViewLayoutAttributes)attributes.Copy();
		}).ToArray();
	}

	public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
	{
		if (!Handler.TryGetTarget(out var handler) || !handler.IsConnected)
			return base.ShouldInvalidateLayoutForBoundsChange(newBounds);
		var crossChanged = handler.VirtualView.Orientation == ListOrientation.Vertical
			? newBounds.Width != CollectionView.Bounds.Width
			: newBounds.Height != CollectionView.Bounds.Height;
		return crossChanged || base.ShouldInvalidateLayoutForBoundsChange(newBounds);
	}
}
