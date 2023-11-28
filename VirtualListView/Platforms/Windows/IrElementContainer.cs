using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.Maui;

internal class IrElementContainer : ContentControl
{
	public IrElementContainer(IMauiContext context, string reuseId, PositionalViewSelector positionalViewSelector)
		: base()
	{
		MauiContext = context;
		ReuseId = reuseId;
		PositionalViewSelector = positionalViewSelector;

		if (positionalViewSelector.VirtualListView.Orientation == ListOrientation.Vertical)
		{
			HorizontalContentAlignment = UI.Xaml.HorizontalAlignment.Stretch;
			HorizontalAlignment = UI.Xaml.HorizontalAlignment.Stretch;
		}
		else
		{
			VerticalContentAlignment = UI.Xaml.VerticalAlignment.Stretch;
			VerticalAlignment = UI.Xaml.VerticalAlignment.Stretch;
		}
	}

	public readonly string ReuseId;
	public readonly IMauiContext MauiContext;
	public readonly PositionalViewSelector PositionalViewSelector;

	public PositionInfo PositionInfo { get; private set; }

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

	public IView VirtualView { get; private set; }

	public void SetupView(IView view)
	{
		if (VirtualView is null || VirtualView.Handler is null)
		{
            Content = view.ToPlatform(MauiContext);
            VirtualView = view;
        }
	}

	public void UpdatePosition(PositionInfo positionInfo)
	{
        PositionInfo = positionInfo;

        if (VirtualView is IPositionInfo viewWithPositionInfo)
            viewWithPositionInfo.Update(PositionInfo);
    }

	protected override void OnTapped(TappedRoutedEventArgs e)
	{
		base.OnTapped(e);

		// Don't select non-item positions
		if ((PositionInfo?.Kind ?? PositionKind.Header) != PositionKind.Item)
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
