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
	internal RetainedView Content => hosted?.Retained;
	// What this cell currently shows. It can move to another cell (see CvDataSource.ReclaimContent).
	CellContent hosted;
	[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "The callback holds only a weak cell reference; the cell removes and disposes its recognizer.")]
	readonly UITapGestureRecognizer tapRecognizer;
	bool disposed;
	RetainedContentLayout contentLayout => hosted?.Layout;

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
		if (Handler?.IsConnected == true && Handler.VirtualView.SelectionMode != SelectionMode.None
			&& PositionInfo?.Kind == PositionKind.Item)
		{
			TapHandler.Invoke(this);
		}
	}

	SelectionMode SelectionMode => Handler?.IsConnected == true ? Handler.VirtualView.SelectionMode : SelectionMode.None;

	// A row reads as one VoiceOver element (like a UITableViewCell) with its content's labels joined,
	// so selection and heading state have somewhere to live. Rows hosting their own controls (buttons,
	// switches, tap gestures, nested scrolling, ...) keep their children reachable instead, because a
	// single element would hide those controls from VoiceOver. Everything is read on demand: positions
	// move under a cell during keyed updates without the cell being rebound.
	public override bool IsAccessibilityElement
	{
		get => RowAccessibility.IsRow(PositionInfo) && NativeView is UIView content
			&& !AccessibilityContent.Inspect(content).Interactive;
		set => base.IsAccessibilityElement = value;
	}

	public override string AccessibilityLabel
	{
		get => NativeView is UIView content ? AccessibilityContent.Inspect(content).Label : base.AccessibilityLabel;
		set => base.AccessibilityLabel = value;
	}

	public override string AccessibilityHint
	{
		get => NativeView?.AccessibilityHint is { Length: > 0 } hint ? hint : base.AccessibilityHint;
		set => base.AccessibilityHint = value;
	}

	public override UIAccessibilityTrait AccessibilityTraits
	{
		get
		{
			var traits = base.AccessibilityTraits;
			// A heading set inside the template (SemanticProperties.HeadingLevel) survives the grouping.
			if (RowAccessibility.IsHeading(PositionInfo)
				|| (NativeView is UIView content && AccessibilityContent.Inspect(content).Heading))
				traits |= UIAccessibilityTrait.Header;
			if (RowAccessibility.IsSelected(PositionInfo, SelectionMode))
				traits |= UIAccessibilityTrait.Selected;
			return traits;
		}
		set => base.AccessibilityTraits = value;
	}

	public override bool AccessibilityActivate()
	{
		if (!RowAccessibility.IsSelectable(PositionInfo, SelectionMode))
			return base.AccessibilityActivate();
		InvokeTap();
		return true;
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
		var result = (UICollectionViewLayoutAttributes)layoutAttributes.Copy();
		if (contentLayout is null || Handler?.IsConnected != true)
			return result;
		var axis = Handler.VirtualView.Orientation;
		var frame = result.Frame;
		var cross = Handler.ContentCrossSize;
		var size = contentLayout.Measure(axis, cross, force: true);
		// Only the scrolling axis is self-sized; the cross axis keeps the layout's own value. The
		// layout's frame goes through float precision, so at a fractional cross size such as a
		// 1174/3 pt pane (391.3333435 vs 391.3333333) returning the measured cross size reads as a
		// changed preferred size on every pass, and UIKit's feedback loop never settles.
		result.Frame = axis == ListOrientation.Vertical
			? new CGRect(frame.X, frame.Y, frame.Width, size.Height)
			: new CGRect(frame.X, frame.Y, size.Width, frame.Height);
		return result;
	}

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		if (Handler?.IsConnected == true && contentLayout is not null)
			contentLayout.Arrange(Handler.VirtualView.Orientation, new Size(ContentView.Bounds.Width, ContentView.Bounds.Height));
	}

	public IView VirtualView => Content?.View;

	public UIView NativeView => hosted?.NativeView;

	public void Bind(VirtualListViewHandler handler, PositionInfo position, object data, PresentationKey? key)
	{
		Handler = handler;
		tapRecognizer.Enabled = position.Kind == PositionKind.Item;
		if (hosted is null)
		{
			var slot = new CellContent(this);
			slot.Retained = new RetainedView(handler.VirtualView,
				view =>
				{
					if (!slot.Host.TryGetTarget(out var cell))
						throw new InvalidOperationException("The Apple content host is no longer available.");
					cell.SetupView(slot, view);
				},
				slot.ClearView);
			hosted = slot;
		}
		handler.BindRetained(Content, position, data, key);
		SetNeedsLayout();
	}

	public override void PrepareForReuse()
	{
		base.PrepareForReuse();

		// Keep the previous BindingContext (as RecyclerView does on Android): clearing it here pushes defaults
		// through every binding, and makes rows that build content on BindingContextChanged rebuild, only for
		// the next Bind to set an item moments later. Release still clears it.
		Content?.Unbind(clearContext: false);
	}

	void SetupView(CellContent slot, IView view)
	{
		var native = view.ToPlatform(Handler.MauiContext);
		native.RemoveFromSuperview();
		native.AutoresizingMask = UIViewAutoresizing.None;
		ContentView.AddSubview(native);
		slot.NativeView = native;
		// Through the slot's host, not this cell: the content may move to another cell.
		slot.Layout = new RetainedContentLayout(view, Handler.DispatchContentLayout, changed =>
		{
			if (slot.Host.TryGetTarget(out var cell) && cell.Handler?.IsConnected == true && cell.PositionInfo is not null)
			{
				cell.SetNeedsLayout();
				if (changed)
					cell.Handler.InvalidateContentLayout(cell.PositionInfo);
			}
		});
	}

	// Swaps what two cells show: the retained MAUI trees, their native views and their measurements.
	// Only for cells UIKit isn't displaying (a cell being dequeued and one waiting in the reuse pool).
	internal static void ExchangeContent(CvCell a, CvCell b)
	{
		(a.hosted, b.hosted) = (b.hosted, a.hosted);
		a.AdoptHostedContent();
		b.AdoptHostedContent();
	}

	void AdoptHostedContent()
	{
		if (hosted is null)
			return;
		hosted.Host.SetTarget(this);
		if (hosted.NativeView is UIView native && !ReferenceEquals(native.Superview, ContentView))
		{
			native.RemoveFromSuperview();
			ContentView.AddSubview(native);
		}
		SetNeedsLayout();
	}

	internal void Release()
	{
		hosted?.Retained?.Release();
		hosted = null;
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
	
	// A cell's retained content. Its callbacks reach the cell through Host, which ExchangeContent re-points.
	[UnconditionalSuppressMessage("Memory", "MEM0002", Justification = "The slot owns retained content; ClearView removes it and Release disconnects owned MAUI handlers.")]
	sealed class CellContent
	{
		public CellContent(CvCell host) => Host = new WeakReference<CvCell>(host);

		public WeakReference<CvCell> Host { get; }
		public RetainedView Retained { get; set; }
		public UIView NativeView { get; set; }
		public RetainedContentLayout Layout { get; set; }

		public void ClearView()
		{
			Layout?.Dispose();
			Layout = null;
			NativeView?.RemoveFromSuperview();
			NativeView = null;
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