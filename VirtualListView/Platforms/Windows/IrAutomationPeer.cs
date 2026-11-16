using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using AutomationProperties = Microsoft.UI.Xaml.Automation.AutomationProperties;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.Maui
{
	// UI Automation for one realized row. Items are list items that carry the list's selection;
	// section headers and footers are text (headers are headings); the global header, footer and
	// empty view stay out of the control view so their own controls read as page content. Everything
	// is read from the row's current position on demand, because keyed updates move positions under
	// a realized row without rebinding it.
	internal sealed class IrAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IScrollItemProvider
	{
		public bool IsRecycled { get; set; } = false;

		readonly IrElementContainer container;

		public IrAutomationPeer(IrElementContainer owner) : base(owner)
		{
			container = owner;
		}

		PositionInfo Position => IsRecycled ? null : container.PositionInfo;

		SelectionMode SelectionMode => container.SelectionMode;

		// Section headers and footers read as one piece of text unless they host their own controls.
		bool IsLeafRow => Position?.Kind is PositionKind.SectionHeader or PositionKind.SectionFooter && !Content().Interactive;

		protected override AutomationControlType GetAutomationControlTypeCore()
			=> Position?.Kind switch
			{
				PositionKind.Item => AutomationControlType.ListItem,
				PositionKind.SectionHeader or PositionKind.SectionFooter => AutomationControlType.Text,
				_ => AutomationControlType.Group,
			};

		protected override string GetClassNameCore() => nameof(IrElementContainer);

		protected override bool IsControlElementCore() => RowAccessibility.IsRow(Position);

		protected override bool IsContentElementCore() => RowAccessibility.IsRow(Position);

		protected override string GetNameCore()
		{
			if (!RowAccessibility.IsRow(Position))
				return base.GetNameCore();
			var explicitName = base.GetNameCore();
			if (!string.IsNullOrEmpty(explicitName))
				return explicitName;
			return Content().Label ?? string.Empty;
		}

		protected override string GetFullDescriptionCore()
		{
			var description = base.GetFullDescriptionCore();
			return !string.IsNullOrEmpty(description) || container.Content is not UIElement content
				? description : AutomationProperties.GetFullDescription(content);
		}

		protected override AutomationHeadingLevel GetHeadingLevelCore()
		{
			if (!RowAccessibility.IsRow(Position))
				return AutomationHeadingLevel.None;
			if (container.Content is UIElement content && AutomationProperties.GetHeadingLevel(content) is var own
				&& own != AutomationHeadingLevel.None)
				return own;
			// A leaf row hides its children, so a heading set inside the template moves up to the row.
			if (IsLeafRow && Content().Heading is var inner && inner != AutomationHeadingLevel.None)
				return inner;
			return RowAccessibility.IsHeading(Position) ? AutomationHeadingLevel.Level2 : AutomationHeadingLevel.None;
		}

		// Items count within their section ("3 of 10"), matching what the template's IPositionInfo sees.
		protected override int GetPositionInSetCore()
			=> Position is { Kind: PositionKind.Item } position ? position.ItemIndex + 1 : base.GetPositionInSetCore();

		protected override int GetSizeOfSetCore()
			=> Position is { Kind: PositionKind.Item } position ? position.ItemsInSection : base.GetSizeOfSetCore();

		protected override IList<AutomationPeer> GetChildrenCore()
		{
			if (IsRecycled || IsLeafRow)
				return new List<AutomationPeer>();

			return base.GetChildrenCore();
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.SelectionItem && RowAccessibility.IsSelectable(Position, SelectionMode))
				return this;
			if (patternInterface == PatternInterface.ScrollItem && Position is not null)
				return this;
			return base.GetPatternCore(patternInterface);
		}

		public bool IsSelected => RowAccessibility.IsSelected(Position, SelectionMode);

		public IRawElementProviderSimple SelectionContainer
			=> VisualTreeHelper.GetParent(container) is UIElement parent
				&& FrameworkElementAutomationPeer.CreatePeerForElement(parent) is AutomationPeer peer
				? ProviderFromPeer(peer) : null;

		// UIA's Select replaces the selection; AddToSelection extends it where the list allows.
		public void Select() => container.SetSelected(true, exclusive: true);

		public void AddToSelection() => container.SetSelected(true, exclusive: false);

		public void RemoveFromSelection() => container.SetSelected(false, exclusive: false);

		public void ScrollIntoView() => container.StartBringIntoView();

		internal void RaiseSelectionChanged(bool selected)
		{
			if (ListenerExists(AutomationEvents.PropertyChanged))
				RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, !selected, selected);
			var selectionEvent = !selected ? AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection
				: SelectionMode == SelectionMode.Multiple ? AutomationEvents.SelectionItemPatternOnElementAddedToSelection
				: AutomationEvents.SelectionItemPatternOnElementSelected;
			if (ListenerExists(selectionEvent))
				RaiseAutomationEvent(selectionEvent);
		}

		// What a screen reader would read inside the row: names of non-interactive descendants, in order,
		// and whether the row hosts controls of its own.
		readonly record struct Summary(string Label, bool Interactive, AutomationHeadingLevel Heading);

		Summary Content()
		{
			var parts = new List<string>();
			var interactive = false;
			var heading = AutomationHeadingLevel.None;
			if (!IsRecycled)
				Visit(base.GetChildrenCore());
			return new(RowAccessibility.ComposeLabel(parts), interactive, heading);

			void Visit(IList<AutomationPeer> peers)
			{
				if (peers is null)
					return;
				foreach (var peer in peers)
				{
					// Controls stay separately reachable, so they aren't part of the row's name.
					if (InteractiveTypes.Contains(peer.GetAutomationControlType()))
					{
						interactive = true;
						continue;
					}
					if (peer.GetName() is { Length: > 0 } name)
					{
						parts.Add(name);
						if (heading == AutomationHeadingLevel.None)
							heading = peer.GetHeadingLevel();
					}
					else
						Visit(peer.GetChildren());
				}
			}
		}

		static readonly HashSet<AutomationControlType> InteractiveTypes =
		[
			AutomationControlType.Button, AutomationControlType.CheckBox, AutomationControlType.ComboBox,
			AutomationControlType.Edit, AutomationControlType.Hyperlink, AutomationControlType.List,
			AutomationControlType.Menu, AutomationControlType.MenuItem, AutomationControlType.RadioButton,
			AutomationControlType.ScrollBar, AutomationControlType.Slider, AutomationControlType.Spinner,
			AutomationControlType.SplitButton, AutomationControlType.Tab, AutomationControlType.Tree,
			AutomationControlType.DataGrid, AutomationControlType.Document, AutomationControlType.Pane,
		];
	}
}
