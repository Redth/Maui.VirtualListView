using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvLayout : UICollectionViewFlowLayout
	{
		public CvLayout() : base()
		{
			isiOS11 = UIDevice.CurrentDevice.CheckSystemVersion(11, 0);
		}

		readonly bool isiOS11;

		public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath path)
		{
			var layoutAttributes = base.LayoutAttributesForItem(path);

			var x = SectionInset.Left;

			nfloat width;

			if (isiOS11)
				width = CollectionView.SafeAreaLayoutGuide.LayoutFrame.Width - SectionInset.Left - SectionInset.Right;
			else
				width = CollectionView.Bounds.Width - SectionInset.Left - SectionInset.Right;

			layoutAttributes.Frame = new CGRect(x, layoutAttributes.Frame.Y, width, layoutAttributes.Frame.Height);

			return layoutAttributes;
		}

		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
		{
			var layoutAttributesObjects = base.LayoutAttributesForElementsInRect(rect);

			foreach (var layoutAttributes in layoutAttributesObjects)
			{
				var indexPath = layoutAttributes.IndexPath;

				if (layoutAttributes.RepresentedElementCategory == UICollectionElementCategory.Cell)
				{
					var newFrame = LayoutAttributesForItem(indexPath).Frame;

					layoutAttributes.Frame = newFrame;
				}
			}

			return layoutAttributesObjects;
		}
	}
}
