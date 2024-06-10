using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui;

internal class CvCell : UICollectionViewCell
{
	internal const string ReuseIdUnknown = "UNKNOWN";
	
	public VirtualListViewHandler Handler { get; set; }

	public WeakReference<NSIndexPath> IndexPath { get; set; }

	public PositionInfo PositionInfo { get; private set; }

	public WeakReference<Action<IView>> ReuseCallback { get; set; }

	[Export("initWithFrame:")]
	public CvCell(CGRect frame) : base(frame)
	{
		this.ContentView.AddGestureRecognizer(new UITapGestureRecognizer(() => InvokeTap()));
	}

	private TapHandlerCallback TapHandler;

	public void SetTapHandlerCallback(Action<CvCell> callback)
	{
		TapHandler = new TapHandlerCallback(callback);
	}

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
			TapHandler.Invoke(this);
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
		// if ((NativeView is not null && NativeView.TryGetTarget(out var _))
		// 	&& (VirtualView is not null && VirtualView.TryGetTarget(out var virtualView)))
		// {
		// 	var measure = virtualView.Measure(layoutAttributes.Size.Width, double.PositiveInfinity);
		//
		// 	layoutAttributes.Frame = new CGRect(0, layoutAttributes.Frame.Y, layoutAttributes.Frame.Width, measure.Height);
		//
		// 	return layoutAttributes;
		// }
		//
		// return layoutAttributes;
		
		var preferredAttributes = base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);
		
		if (VirtualView.TryGetTarget(out var virtualView))
		{
			if (Handler.VirtualView.Orientation == ListOrientation.Vertical)
			{
				var measure =
					virtualView.Measure(preferredAttributes.Size.Width, double.PositiveInfinity);
		
				preferredAttributes.Frame =
					new CGRect(preferredAttributes.Frame.X, preferredAttributes.Frame.Y,
						preferredAttributes.Frame.Width, measure.Height);
			}
			else
			{
				var measure =
					virtualView.Measure(double.PositiveInfinity, preferredAttributes.Size.Height);
		
				preferredAttributes.Frame =
					new CGRect(preferredAttributes.Frame.X, preferredAttributes.Frame.Y,
						measure.Width, preferredAttributes.Frame.Height);
			}
		
			preferredAttributes.ZIndex = 2;
		}
			
		return preferredAttributes;
	}

	public bool NeedsView
		=> NativeView == null 
			|| VirtualView is null
			|| !NativeView.TryGetTarget(out var _) 
			|| !VirtualView.TryGetTarget(out var _);

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
        if (!(NativeView?.TryGetTarget(out var _) ?? false))
        {
			var nativeView = view.ToPlatform(this.Handler.MauiContext);
            nativeView.Frame = this.ContentView.Frame;
            nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
            
			this.ContentView.AddSubview(nativeView);
            
			NativeView = new WeakReference<UIView>(nativeView);
        }

        if (!(VirtualView?.TryGetTarget(out var virtualView) ?? false) || (virtualView?.Handler is null))
        {
            VirtualView = new WeakReference<IView>(view);
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
	
	class TapHandlerCallback
	{
		public TapHandlerCallback(Action<CvCell> callback)
		{
			Callback = callback;
		}
		
		public readonly Action<CvCell> Callback;

		public void Invoke(CvCell cell)
			=> Callback?.Invoke(cell);
	}
}