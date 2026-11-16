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
			UpdateEstimate(handler.VirtualView.Orientation, CollectionView.Bounds);
		base.PrepareLayout();
	}

	internal double CrossSize(ListOrientation orientation)
		=> CrossSize(orientation, CollectionView.Bounds);

	double CrossSize(ListOrientation orientation, CGRect bounds)
	{
		var inset = CollectionView.AdjustedContentInset;
		return orientation == ListOrientation.Vertical
			? Math.Max(0, bounds.Width - inset.Left - inset.Right - SectionInset.Left - SectionInset.Right)
			: Math.Max(0, bounds.Height - inset.Top - inset.Bottom - SectionInset.Top - SectionInset.Bottom);
	}

	void UpdateEstimate(ListOrientation orientation, CGRect bounds)
	{
		var cross = Math.Max(1, CrossSize(orientation, bounds));
		var estimate = orientation == ListOrientation.Vertical
			? new CGSize(cross, 44) : new CGSize(44, cross);
		if (EstimatedItemSize != estimate)
			EstimatedItemSize = estimate;
	}

	public override UICollectionViewLayoutInvalidationContext GetInvalidationContextForBoundsChange(CGRect newBounds)
	{
		// UIKit validates estimates against the new viewport before PrepareLayout runs.
		if (Handler.TryGetTarget(out var handler) && handler.IsConnected)
			UpdateEstimate(handler.VirtualView.Orientation, newBounds);
		return base.GetInvalidationContextForBoundsChange(newBounds);
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

	// Items are only self-sized along the scrolling axis; the cross axis is the layout's. UIKit
	// pixel-aligns a cell's preferred size but keeps the flow layout's item sizes at float precision,
	// so at a fractional cross size like a 1174/3 pt pane the two never agree (391.3333333 vs
	// 391.3333435). Invalidating on that difference makes the self-sizing loop run forever, until
	// UIKit asserts. Only a change along the scrolling axis invalidates.
	public override bool ShouldInvalidateLayout(UICollectionViewLayoutAttributes preferredAttributes, UICollectionViewLayoutAttributes originalAttributes)
	{
		if (preferredAttributes.RepresentedElementCategory != UICollectionElementCategory.Cell
			|| !Handler.TryGetTarget(out var handler) || !handler.IsConnected)
			return base.ShouldInvalidateLayout(preferredAttributes, originalAttributes);

		var tolerance = 0.5 / Math.Max(1, CollectionView.TraitCollection.DisplayScale);
		var mainAxisChanged = handler.VirtualView.Orientation == ListOrientation.Vertical
			? Math.Abs(preferredAttributes.Frame.Height - originalAttributes.Frame.Height) >= tolerance
			: Math.Abs(preferredAttributes.Frame.Width - originalAttributes.Frame.Width) >= tolerance;
		if (!mainAxisChanged)
			return false;
		// Before base: flow layout applies the new size inside it, so the anchor must be read first.
		if (anchorItem < 0)
			CaptureScrollAnchor();
		return base.ShouldInvalidateLayout(preferredAttributes, originalAttributes);
	}

	// Flow layout never moves the content offset when an item above the viewport changes size. Rows
	// self-size as they scroll in at the top, so scrolling up pushes the visible rows down by each
	// row's size difference: a few points per row normally, and hundreds after a reload or a jump far
	// down the list, when every row above is still at the estimate (flow layout also re-estimates all
	// unmeasured rows from the measured ones, moving them all at once). So when a row self-sizes, the
	// first item that starts inside the viewport is anchored, and once the collection view has laid out
	// again the content offset follows it. Positions are in content coordinates, so the user's own
	// scrolling in between doesn't count as movement.
	int anchorItem = -1;
	int anchorSection;
	nfloat anchorPosition;

	public override void InvalidateLayout(UICollectionViewLayoutInvalidationContext context)
	{
		// Reloads and batch updates renumber items; the anchor no longer means the same item.
		if (context.InvalidateEverything || context.InvalidateDataSourceCounts)
			ClearScrollAnchor();
		base.InvalidateLayout(context);
	}

	void ClearScrollAnchor() => anchorItem = -1;

	void CaptureScrollAnchor()
	{
		if (CollectionView is not UICollectionView collectionView
			|| !Handler.TryGetTarget(out var handler) || !handler.IsConnected)
			return;

		var vertical = handler.VirtualView.Orientation == ListOrientation.Vertical;
		var inset = collectionView.AdjustedContentInset;
		var bounds = collectionView.Bounds;
		var start = vertical ? bounds.Y + inset.Top : bounds.X + inset.Left;
		var visible = vertical
			? new CGRect(bounds.X, start, bounds.Width, Math.Max(1, bounds.Height - inset.Top - inset.Bottom))
			: new CGRect(start, bounds.Y, Math.Max(1, bounds.Width - inset.Left - inset.Right), bounds.Height);

		UICollectionViewLayoutAttributes anchor = null;
		UICollectionViewLayoutAttributes first = null;
		foreach (var attributes in base.LayoutAttributesForElementsInRect(visible) ?? [])
		{
			if (attributes.RepresentedElementCategory != UICollectionElementCategory.Cell)
				continue;
			var position = vertical ? attributes.Frame.Y : attributes.Frame.X;
			if (first is null || attributes.IndexPath.Item < first.IndexPath.Item)
				first = attributes;
			// An item straddling the leading edge is the one most likely to be self-sizing as it
			// scrolls in, so anchor the first one that starts inside the viewport.
			if (position >= start - 0.5 && (anchor is null || attributes.IndexPath.Item < anchor.IndexPath.Item))
				anchor = attributes;
		}

		anchor ??= first;
		if (anchor is null)
			return;
		anchorItem = (int)anchor.IndexPath.Item;
		anchorSection = (int)anchor.IndexPath.Section;
		anchorPosition = vertical ? anchor.Frame.Y : anchor.Frame.X;
	}

	/// <summary>
	/// Called after the collection view lays out. Moves the content offset by however far
	/// self-sizing moved the anchored item, and returns whether it did.
	/// </summary>
	internal bool FollowScrollAnchor()
	{
		var item = anchorItem;
		if (item < 0)
			return false;
		ClearScrollAnchor();

		if (CollectionView is not UICollectionView collectionView
			|| !Handler.TryGetTarget(out var handler) || !handler.IsConnected
			// The handler is restoring its own anchor (data updates, resizes); it decides the offset.
			|| handler.HasPendingAnchor
			|| anchorSection >= collectionView.NumberOfSections()
			|| item >= collectionView.NumberOfItemsInSection(anchorSection))
			return false;

		using var path = NSIndexPath.FromItemSection(item, anchorSection);
		using var attributes = base.LayoutAttributesForItem(path);
		if (attributes is null)
			return false;

		var vertical = handler.VirtualView.Orientation == ListOrientation.Vertical;
		var delta = (vertical ? attributes.Frame.Y : attributes.Frame.X) - anchorPosition;
		if (Math.Abs(delta) < 0.5 / Math.Max(1, collectionView.TraitCollection.DisplayScale))
			return false;

		var offset = collectionView.ContentOffset;
		collectionView.ContentOffset = vertical
			? new CGPoint(offset.X, offset.Y + delta)
			: new CGPoint(offset.X + delta, offset.Y);
		return true;
	}
}
