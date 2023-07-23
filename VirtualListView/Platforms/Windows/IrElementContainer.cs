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

	public IView VirtualView { get; private set; }

	public void Update(PositionInfo positionInfo, IView newView)
	{
		PositionInfo = positionInfo;

		if (newView is IPositionInfo viewWithPositionInfo)
			viewWithPositionInfo.Update(PositionInfo);

		SwapView(newView);
	}

	void SwapView(IView newView)
	{
		if (VirtualView == null || VirtualView.Handler == null || Content == null)
		{
			Content = newView.ToPlatform(MauiContext);
			VirtualView = newView;
		}
		else
		{
			var handler = VirtualView.Handler;
			newView.Handler = handler;
			handler.SetVirtualView(newView);
			VirtualView = newView;
		}
	}

	protected override void OnTapped(TappedRoutedEventArgs e)
	{
		base.OnTapped(e);

		if (PositionInfo != null)
			PositionInfo.IsSelected = !PositionInfo.IsSelected;

		var itemPos = new ItemPosition(PositionInfo?.SectionIndex ?? 0, PositionInfo?.ItemIndex ?? 0);

		if (PositionInfo?.IsSelected ?? false)
			PositionalViewSelector?.VirtualListView?.SetSelected(itemPos);
		else
			PositionalViewSelector?.VirtualListView?.SetDeselected(itemPos);
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
