using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.Maui;

internal class IrElementContainer : Panel
{
	readonly VirtualListViewHandler handler;
	RetainedContentLayout contentLayout;

	public IrElementContainer(IMauiContext context, string reuseId, PositionalViewSelector positionalViewSelector, VirtualListViewHandler handler)
		: base()
	{
		MauiContext = context;
		ReuseId = reuseId;
		PositionalViewSelector = positionalViewSelector;
		this.handler = handler;
		ContentOwner = new RetainedView(positionalViewSelector.VirtualListView, SetupView, ClearView);
		UpdateOrientation();
		Tapped += ContainerTapped;
	}

	internal void UpdateOrientation()
	{
		var vertical = PositionalViewSelector.VirtualListView.Orientation == ListOrientation.Vertical;
		HorizontalAlignment = vertical ? UI.Xaml.HorizontalAlignment.Stretch : UI.Xaml.HorizontalAlignment.Left;
		VerticalAlignment = vertical ? UI.Xaml.VerticalAlignment.Top : UI.Xaml.VerticalAlignment.Stretch;
		InvalidateMeasure();
	}

	protected override global::Windows.Foundation.Size MeasureOverride(global::Windows.Foundation.Size availableSize)
	{
		if (contentLayout is null)
			return new global::Windows.Foundation.Size();
		var axis = PositionalViewSelector.VirtualListView.Orientation;
		var cross = axis == ListOrientation.Vertical ? availableSize.Width : availableSize.Height;
		if (double.IsPositiveInfinity(cross))
			cross = handler.ContentCrossSize;
		var desired = contentLayout.Measure(axis, cross, force: true);
		return new global::Windows.Foundation.Size(desired.Width, desired.Height);
	}

	protected override global::Windows.Foundation.Size ArrangeOverride(global::Windows.Foundation.Size finalSize)
	{
		contentLayout?.Arrange(PositionalViewSelector.VirtualListView.Orientation, new Size(finalSize.Width, finalSize.Height));
		return finalSize;
	}

	public readonly string ReuseId;
	public readonly IMauiContext MauiContext;
	public readonly PositionalViewSelector PositionalViewSelector;

	public PositionInfo PositionInfo => ContentOwner.Position;
	internal RetainedView ContentOwner { get; }

	bool isRecycled = false;

	internal bool IsRecycled
	{
		get => isRecycled;
		set
		{
			isRecycled = value;

			if (automationPeer != null)
				automationPeer.IsRecycled = value;
		}
	}

	public bool NeedsView
		=> VirtualView is null || VirtualView.Handler is null;

	public IView VirtualView => ContentOwner.View;
	internal UIElement Content => Children.FirstOrDefault();

	public void SetupView(IView view)
	{
		Children.Add(view.ToPlatform(MauiContext));
		var weakSelf = new WeakReference<IrElementContainer>(this);
		contentLayout = new RetainedContentLayout(view, handler.DispatchContentLayout, _ =>
		{
			if (weakSelf.TryGetTarget(out var container) && container.handler.IsConnected && !container.IsRecycled)
				container.handler.Publish(() => container.handler.PreserveAnchorDuring(container.InvalidateMeasure));
		});
	}

	void ClearView()
	{
		contentLayout?.Dispose();
		contentLayout = null;
		Children.Clear();
	}

	public void UpdatePosition(PositionInfo positionInfo)
	{
        if (VirtualView is IPositionInfo viewWithPositionInfo)
            viewWithPositionInfo.Update(PositionInfo);
    }

	void ContainerTapped(object sender, TappedRoutedEventArgs e)
	{
		// Don't select non-item positions
		if (IsRecycled || !handler.IsConnected || handler.VirtualView.SelectionMode == SelectionMode.None
			|| (PositionInfo?.Kind ?? PositionKind.Header) != PositionKind.Item)
			return;

		if (PositionInfo != null)
			PositionInfo.IsSelected = !PositionInfo.IsSelected;

		var itemPos = new ItemPosition(PositionInfo?.SectionIndex ?? 0, PositionInfo?.ItemIndex ?? 0);

		if (PositionInfo?.IsSelected ?? false)
			PositionalViewSelector?.VirtualListView?.SelectItem(itemPos);
		else
			PositionalViewSelector?.VirtualListView?.DeselectItem(itemPos);
	}

	protected override IEnumerable<DependencyObject> GetChildrenInTabFocusOrder()
	{
		if (IsRecycled)
			return Enumerable.Empty<DependencyObject>();
		else
			return base.GetChildrenInTabFocusOrder();
	}

	IrAutomationPeer automationPeer;

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return automationPeer ??= new IrAutomationPeer(this);
	}
}
