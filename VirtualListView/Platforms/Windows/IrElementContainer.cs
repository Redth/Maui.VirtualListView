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
		UseSystemFocusVisuals = true;
		Tapped += ContainerTapped;
		KeyDown += ContainerKeyDown;
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

	internal SelectionMode SelectionMode => handler.IsConnected ? handler.VirtualView.SelectionMode : SelectionMode.None;

	bool CanSelect => !IsRecycled && RowAccessibility.IsSelectable(PositionInfo, SelectionMode);

	void ContainerTapped(object sender, TappedRoutedEventArgs e)
	{
		// Don't select non-item positions
		if (CanSelect)
			SetSelected(!PositionInfo.IsSelected, exclusive: false);
	}

	// Enter/Space select the focused row, as they do in a native list; keys aimed at a control inside
	// the row (a text box, a button) are left to that control.
	void ContainerKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Handled || !ReferenceEquals(e.OriginalSource, this) || !CanSelect
			|| e.Key is not (global::Windows.System.VirtualKey.Enter or global::Windows.System.VirtualKey.Space))
			return;
		e.Handled = true;
		SetSelected(!PositionInfo.IsSelected, exclusive: false);
	}

	// exclusive replaces the whole selection with this row (UI Automation's Select).
	internal void SetSelected(bool selected, bool exclusive)
	{
		if (!CanSelect)
			return;

		var list = PositionalViewSelector.VirtualListView;
		var itemPos = new ItemPosition(PositionInfo.SectionIndex, PositionInfo.ItemIndex);
		PositionInfo.IsSelected = selected;

		if (!selected)
			list.DeselectItem(itemPos);
		else if (exclusive && list.SelectionMode == SelectionMode.Multiple)
			list.SelectedItems = [itemPos];
		else
			list.SelectItem(itemPos);
	}

	// Only selectable rows take keyboard focus; the repeater keeps a single tab stop (see the handler),
	// and arrow keys move between rows.
	internal void UpdateAccessibility()
	{
		IsTabStop = CanSelect;
	}

	internal void RaiseSelectionChanged(bool selected) => automationPeer?.RaiseSelectionChanged(selected);

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
