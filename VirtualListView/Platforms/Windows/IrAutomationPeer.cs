using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.Maui
{
	internal class IrAutomationPeer : FrameworkElementAutomationPeer
	{
		public bool IsRecycled { get; set; } = false;

		readonly IrElementContainer container;

		public IrAutomationPeer(IrElementContainer owner) : base(owner)
		{
			container = owner;
		}

		protected override AutomationHeadingLevel GetHeadingLevelCore()
		{
			return ContainerAutomationPeer()?.GetHeadingLevel() ??
				container.PositionInfo.Kind switch
				{
					PositionKind.Header => AutomationHeadingLevel.Level1,
					PositionKind.Footer => AutomationHeadingLevel.Level1,
					PositionKind.SectionHeader => AutomationHeadingLevel.Level2,
					PositionKind.SectionFooter => AutomationHeadingLevel.Level2,
					PositionKind.Item => AutomationHeadingLevel.Level3,
					_ => base.GetHeadingLevelCore()
				};
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
			=> container.PositionInfo.Kind switch {
					PositionKind.Header => AutomationControlType.Header,
					PositionKind.Footer => AutomationControlType.Header,
					PositionKind.SectionHeader => AutomationControlType.HeaderItem,
					PositionKind.SectionFooter=> AutomationControlType.HeaderItem,
					PositionKind.Item => AutomationControlType.ListItem,
					_ => base.GetAutomationControlTypeCore()
				};

		protected override int GetPositionInSetCore()
		{
			if (container.PositionInfo.Kind == PositionKind.Item)
				return container.PositionInfo.ItemIndex + 1;
			else if (container.PositionInfo.Kind == PositionKind.SectionHeader)
				return container.PositionInfo.SectionIndex + 1;

			return 0;
		}

		protected override int GetSizeOfSetCore()
		{
			if (container.PositionInfo.Kind == PositionKind.Item)
				return container.PositionInfo.ItemsInSection;
			else if (container.PositionInfo.Kind == PositionKind.SectionHeader)
				return container.PositionInfo.NumberOfSections;

			return 0;
		}

		protected override IList<AutomationPeer> GetChildrenCore()
		{
			if (IsRecycled)
				return Enumerable.Empty<AutomationPeer>().ToList();

			return base.GetChildrenCore();
		}

		FrameworkElementAutomationPeer ContainerAutomationPeer()
		{
			if (container.Content is UIElement uiElement)
				return FrameworkElementAutomationPeer.FromElement(uiElement) as FrameworkElementAutomationPeer;

			return null;
		}

		protected override string GetNameCore()
			=> ContainerAutomationPeer()?.GetName() ?? base.GetNameCore();

		protected override string GetFullDescriptionCore()
			=> ContainerAutomationPeer()?.GetFullDescription() ?? base.GetFullDescriptionCore();

		protected override string GetClassNameCore()
			=> ContainerAutomationPeer()?.GetClassName() ?? base.GetClassNameCore();
	}
}
