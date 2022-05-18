using System;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvCell : UICollectionViewCell
	{
		public VirtualListViewHandler Handler { get; set; }

		public NSIndexPath IndexPath { get; set; }

		public PositionInfo PositionInfo { get; private set; }

		public Action<IView> ReuseCallback { get; set; }

		[Export("initWithFrame:")]
		public CvCell(CGRect frame) : base(frame)
		{
		}

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			if (NativeView == null || VirtualView == null)
				return layoutAttributes;

			var measure = VirtualView.Measure(layoutAttributes.Size.Width, double.PositiveInfinity);

			layoutAttributes.Frame = new CGRect(0, layoutAttributes.Frame.Y, layoutAttributes.Frame.Width, measure.Height);

			return layoutAttributes;
		}

		public void Update(PositionInfo info)
		{
			PositionInfo = info;
			if (VirtualView is IPositionInfo positionInfoView)
				positionInfoView.SetPositionInfo(info);
		}

		public bool NeedsView
			=> NativeView == null;

		public IView VirtualView { get; private set; }

		public UIView NativeView { get; private set; }

		public override void PrepareForReuse()
		{
			base.PrepareForReuse();

			// TODO: Recycle
			if (VirtualView != null)
				ReuseCallback?.Invoke(VirtualView);
		}

		public void SwapView(IView newView)
		{
			if (VirtualView == null || VirtualView.Handler == null || NativeView == null)
			{
				NativeView = newView.ToPlatform(this.Handler.MauiContext);
				VirtualView = newView;
				NativeView.Frame = this.ContentView.Frame;
				NativeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
				this.ContentView.AddSubview(NativeView);
			}
			else
			{
				var handler = VirtualView.Handler;
				VirtualView.Handler = null;
				newView.Handler = handler;
				handler.SetVirtualView(newView);
				VirtualView = newView;
			}
		}
	}
}