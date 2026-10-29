#nullable enable
using CoreGraphics;
using UIKit;

namespace Microsoft.Maui;

internal sealed class HorizontalRefreshHost : UIScrollView
{
	readonly WeakReference<UICollectionView> content;

	public HorizontalRefreshHost(UICollectionView collectionView)
	{
		content = new(collectionView);
		ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
		AlwaysBounceVertical = true;
		ShowsVerticalScrollIndicator = false;
		ShowsHorizontalScrollIndicator = false;
		AddSubview(collectionView);
	}

	public override UIView? HitTest(CGPoint point, UIEvent? uievent)
		=> content.TryGetTarget(out var collectionView) && collectionView.UserInteractionEnabled
			? base.HitTest(point, uievent) : null;

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		// Refresh scrolls this host without changing the horizontal collection's viewport.
		if (content.TryGetTarget(out var collectionView))
		{
			var frame = new CGRect(CGPoint.Empty, Bounds.Size);
			if (collectionView.Frame != frame)
				collectionView.Frame = frame;
		}
		if (ContentSize != Bounds.Size)
			ContentSize = Bounds.Size;
	}
}
