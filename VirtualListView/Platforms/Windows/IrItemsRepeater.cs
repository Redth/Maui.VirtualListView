using Microsoft.UI.Xaml.Automation;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Maui;

// ItemsRepeater presents itself as an unnamed group; this one is the list its rows belong to.
internal sealed class IrItemsRepeater : ItemsRepeater
{
	readonly WeakReference<VirtualListViewHandler> handler;

	public IrItemsRepeater(VirtualListViewHandler handler) => this.handler = new(handler);

	internal VirtualListViewHandler Handler
		=> handler.TryGetTarget(out var target) && target.IsConnected ? target : null;

	protected override AutomationPeer OnCreateAutomationPeer() => new IrRepeaterAutomationPeer(this);
}

// Keeps RepeaterAutomationPeer's ordering of realized rows, and adds the list role and its selection.
internal sealed class IrRepeaterAutomationPeer : RepeaterAutomationPeer, ISelectionProvider
{
	readonly IrItemsRepeater repeater;

	public IrRepeaterAutomationPeer(IrItemsRepeater owner) : base(owner) => repeater = owner;

	SelectionMode SelectionMode => repeater.Handler?.VirtualView.SelectionMode ?? SelectionMode.None;

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

	// The list is named by the VirtualListView's own SemanticProperties.Description.
	protected override string GetNameCore()
	{
		var name = base.GetNameCore();
		if (string.IsNullOrEmpty(name) && repeater.Handler?.PlatformView is { } platformView)
			name = AutomationProperties.GetName(platformView);
		return name ?? string.Empty;
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.Selection && SelectionMode != SelectionMode.None
			? this : base.GetPatternCore(patternInterface);

	public bool CanSelectMultiple => SelectionMode == SelectionMode.Multiple;

	public bool IsSelectionRequired => false;

	// UI Automation only sees realized rows, like any virtualized list.
	public IRawElementProviderSimple[] GetSelection()
		=> (repeater.Handler?.RealizedContainers ?? [])
			.Where(container => !container.IsRecycled && RowAccessibility.IsSelected(container.PositionInfo, SelectionMode))
			.OrderBy(container => container.PositionInfo.Position)
			.Select(container => CreatePeerForElement(container))
			.Where(peer => peer is not null)
			.Select(ProviderFromPeer)
			.ToArray();
}
