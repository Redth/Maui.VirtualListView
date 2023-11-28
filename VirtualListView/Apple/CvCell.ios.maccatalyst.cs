using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using Microsoft.VisualBasic;
using System.Diagnostics.CodeAnalysis;
using UIKit;

namespace Microsoft.Maui;

internal class CvCell : UICollectionViewCell
{
	public VirtualListViewHandler Handler { get; set; }

	public WeakReference<NSIndexPath> IndexPath { get; set; }

	public PositionInfo PositionInfo { get; private set; }

	public WeakReference<Action<IView>> ReuseCallback { get; set; }

	[Export("initWithFrame:")]
	public CvCell(CGRect frame) : base(frame)
	{
		this.ContentView.AddGestureRecognizer(new UITapGestureRecognizer(() => InvokeTap()));
	}

	public WeakReference<Action<CvCell>> TapHandler { get; set; }

	WeakReference<UIKeyCommand[]> keyCommands;

	public override UIKeyCommand[] KeyCommands
	{
		get
		{
			if (keyCommands?.TryGetTarget(out var commands) ?? false)
                return commands;

			var v = new[]
			{
				UIKeyCommand.Create(new NSString("\r"), 0, new ObjCRuntime.Selector("keyCommandSelect")),
				UIKeyCommand.Create(new NSString(" "), 0, new ObjCRuntime.Selector("keyCommandSelect")),
			};

            keyCommands = new WeakReference<UIKeyCommand[]>(v);

			return v;
		}

	}

	[Export("keyCommandSelect")]
	public void KeyCommandSelect()
	{
		InvokeTap();
	}

	void InvokeTap()
	{
		if (PositionInfo.Kind == PositionKind.Item)
		{
			if (TapHandler?.TryGetTarget(out var handler) ?? false)
				handler?.Invoke(this);
		}
	}

	public void UpdateSelected(bool selected)
	{
		PositionInfo.IsSelected = selected;

		if (VirtualView?.TryGetTarget(out var virtualView) ?? false)
		{
			if (virtualView is IPositionInfo positionInfo)
			{
				positionInfo.IsSelected = selected;
				virtualView.Handler?.UpdateValue(nameof(PositionInfo.IsSelected));
			}
		}
	}

	public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
	{
		if ((NativeView is not null && NativeView.TryGetTarget(out var _))
			&& (VirtualView is not null && VirtualView.TryGetTarget(out var virtualView)))
		{
			var measure = virtualView.Measure(layoutAttributes.Size.Width, double.PositiveInfinity);

			layoutAttributes.Frame = new CGRect(0, layoutAttributes.Frame.Y, layoutAttributes.Frame.Width, measure.Height);

			return layoutAttributes;
		}

		return layoutAttributes;
	}

	public bool NeedsView
		=> NativeView == null || !NativeView.TryGetTarget(out var _);

	public WeakReference<IView> VirtualView { get; set; }

	public WeakReference<UIView> NativeView { get; set; }

	public override void PrepareForReuse()
	{
		base.PrepareForReuse();

		// TODO: Recycle
		if ((VirtualView?.TryGetTarget(out var virtualView) ?? false)
			&& (ReuseCallback?.TryGetTarget(out var reuseCallback) ?? false))
		{
			reuseCallback?.Invoke(virtualView);
		}
	}

	public void SetupView(IView view)
	{
        // Create a new platform native view if we don't have one yet
        if (!(NativeView?.TryGetTarget(out var nativeView) ?? false))
        {
            nativeView = view.ToPlatform(this.Handler.MauiContext);
            nativeView.Frame = this.ContentView.Frame;
            nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
            this.ContentView.AddSubview(nativeView);
            NativeView = new WeakReference<UIView>(nativeView);
        }

        if (!(VirtualView?.TryGetTarget(out var virtualView) ?? false) || (virtualView?.Handler is null))
        {
            virtualView = view;
            VirtualView = new WeakReference<IView>(virtualView);
        }
    }

	public void UpdatePosition(PositionInfo positionInfo)
	{
        PositionInfo = positionInfo;
        if (VirtualView?.TryGetTarget(out var virtualView) ?? false)
		{
            if (virtualView is IPositionInfo viewPositionInfo)
                viewPositionInfo.Update(positionInfo);
        }
    }
}