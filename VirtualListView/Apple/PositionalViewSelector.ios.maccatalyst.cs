using Microsoft.Maui.Adapters;
using Microsoft.Maui.Animations;

namespace Microsoft.Maui;

internal class PositionalViewSelector : IPositionalViewSelector
{
	public readonly IVirtualListView VirtualListView;
	public IVirtualListViewAdapter Adapter => VirtualListView?.Adapter;
	public IVirtualListViewSelector ViewSelector => VirtualListView?.ViewSelector;

	public bool HasGlobalHeader =>
		(VirtualListView?.IsHeaderVisible ?? false)
			&& (VirtualListView?.Header?.Visibility ?? Visibility.Collapsed) == Visibility.Visible;

	public bool HasGlobalFooter =>
		(VirtualListView?.IsFooterVisible ?? false)
			&& (VirtualListView?.Footer?.Visibility ?? Visibility.Collapsed) == Visibility.Visible;

	public PositionalViewSelector(IVirtualListView virtualListView)
	{
		VirtualListView = virtualListView;
	}

	public int TotalCount
	{
		get
		{
			var total = 0;
			var sections = Adapter.GetNumberOfSections();

			for (var i = 0; i < sections; i++)
			{
				total += Adapter.GetNumberOfItemsInSection(i);
			}

			return total;
		}
	}

	public int GetPosition(int sectionIndex, int itemIndex)
	{
		// calculate position
		if (Adapter == null)
			return -1;

		var position = 0;

		if (HasGlobalHeader)
		{
			position++;
		}

		for (int s = 0; s <= sectionIndex; s++)
		{
			if (ViewSelector.SectionHasHeader(s))
			{
				position++;
			}

			if (s == sectionIndex)
			{
				position += itemIndex;
				break;
			}

			var itemsInSection = Math.Max(Adapter.GetNumberOfItemsInSection(s), 0);

			position += itemsInSection;

			if (ViewSelector.SectionHasFooter(s))
				position++;
		}

		return position;
	}


	public PositionInfo GetInfo(int position)
	{
		if (Adapter == null)
			return null;

		var linear = 0;

		var numberSections = Adapter.GetNumberOfSections();

		if (HasGlobalHeader)
		{
			if (position == 0)
				return PositionInfo.ForHeader(position);

			linear++;
		}

		for (int s = 0; s < numberSections; s++)
		{
			if (ViewSelector.SectionHasHeader(s))
			{
				if (position == linear)
					return PositionInfo.ForSectionHeader(position, s);

				linear++;
			}

			var itemsInSection = Math.Max(Adapter.GetNumberOfItemsInSection(s), 0);

			// It's an item in the section, return it for this item
			if (position < linear + itemsInSection)
			{
				var itemIndex = position - linear;

				return PositionInfo.ForItem(position, s, itemIndex, itemsInSection, numberSections);
			}

			linear += itemsInSection;

			if (ViewSelector.SectionHasFooter(s))
			{
				if (position == linear)
					return PositionInfo.ForSectionFooter(position, s);

				linear++;
			}
		}

		return new PositionInfo
		{
			Position = position,
			Kind = PositionKind.Footer
		};
	}

}
