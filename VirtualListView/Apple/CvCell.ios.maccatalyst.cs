using CoreGraphics;
using System.Diagnostics.CodeAnalysis;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Microsoft.Maui;

internal class CvCell : UICollectionViewCell
{
	internal const string ReuseIdUnknown = "UNKNOWN";
	
	public VirtualListViewHandler Handler { get; set; }

	public PositionInfo PositionInfo => Content?.Position;
	internal RetainedView Content { get; private set; }
	[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "The callback holds only a weak cell reference; the cell removes and disposes its recognizer.")]
	readonly UITapGestureRecognizer tapRecognizer;
	bool disposed;

	[Export("initWithFrame:")]
	public CvCell(CGRect frame) : base(frame)
	{
		var weakSelf = new WeakReference<CvCell>(this);
		tapRecognizer = new UITapGestureRecognizer(() =>
		{
			if (weakSelf.TryGetTarget(out var cell))
				cell.InvokeTap();
		});
		ContentView.AddGestureRecognizer(tapRecognizer);
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
		if (Handler?.IsConnected == true && PositionInfo?.Kind == PositionKind.Item)
		{
			TapHandler.Invoke(this);
		}
	}

	public void UpdateSelected(bool selected)
	{
		if (PositionInfo is null)
			return;
		PositionInfo.IsSelected = selected;

		if (VirtualView is IView virtualView)
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
		if (NativeView is not null && VirtualView is IView virtualView)
		{
			var measure = virtualView.Measure(layoutAttributes.Size.Width, double.PositiveInfinity);

			layoutAttributes.Frame = new CGRect(0, layoutAttributes.Frame.Y, layoutAttributes.Frame.Width, measure.Height);

			return layoutAttributes;
		}

		return layoutAttributes;
	}

	public IView VirtualView => Content?.View;

	[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "The slot owns retained content; ClearView removes it and Release disconnects owned MAUI handlers.")]
	public UIView NativeView { get; private set; }

	public void Bind(VirtualListViewHandler handler, PositionInfo position, object data)
	{
		Handler = handler;
		if (Content is null)
		{
			var weakSelf = new WeakReference<CvCell>(this);
			Content = new RetainedView(handler.VirtualView,
				view =>
				{
					if (!weakSelf.TryGetTarget(out var cell))
						throw new InvalidOperationException("The Apple content host is no longer available.");
					cell.SetupView(view);
				},
				() =>
				{
					if (weakSelf.TryGetTarget(out var cell))
						cell.ClearView();
				});
		}
		Content.Bind(position, data);
	}

	public override void PrepareForReuse()
	{
		base.PrepareForReuse();

		Content?.Unbind();
	}

	public void SetupView(IView view)
	{
		NativeView = view.ToPlatform(Handler.MauiContext);
		NativeView.RemoveFromSuperview();
		NativeView.Frame = ContentView.Frame;
		NativeView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		ContentView.AddSubview(NativeView);
    }

	void ClearView()
	{
		NativeView?.RemoveFromSuperview();
		NativeView = null;
	}

	internal void Release()
	{
		Content?.Release();
		Content = null;
		TapHandler = null;
		Handler = null;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !disposed)
		{
			disposed = true;
			Release();
			ContentView.RemoveGestureRecognizer(tapRecognizer);
			tapRecognizer.Dispose();
		}
		base.Dispose(disposing);
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