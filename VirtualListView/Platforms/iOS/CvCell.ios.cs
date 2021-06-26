using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvCell : UICollectionViewCell
	{
		public VirtualViewCell ViewCell { get; private set; }

		public NSIndexPath IndexPath { get; set; }

		UIContainerView containerView = null;

		[Export("initWithFrame:")]
		public CvCell(CGRect frame) : base(frame)
		{
		}

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			var attr = base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);

			if (ViewCell?.View == null)
				return attr;

			ViewCell.View.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);

			var formsSize = ViewCell.View.Measure(attr.Frame.Width, double.MaxValue - 100, MeasureFlags.IncludeMargins);

			var h = formsSize.Request.Height;

			attr.Frame = new CGRect(0, attr.Frame.Y, attr.Frame.Width, h);

			return attr;
		}

		public void EnsureFormsTemplate(DataTemplate template, PositionInfo positionInfo)
		{
			if (ViewCell?.View == null)
			{
				var templateContent = template.CreateContent();

				if (templateContent is VirtualViewCell vc)
					ViewCell = vc;
				else
					VirtualViewCell.ThrowInvalidDataTemplateException();
			}

			ViewCell.Update(positionInfo);

			ViewCell.BindingContext = positionInfo.BindingContext;

			if (containerView == null)
			{
				containerView = new UIContainerView(ViewCell.View)
				{
					Frame = ContentView.Frame,
					AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth
				};

				ContentView.AddSubview(containerView);
			}
		}
	}
}