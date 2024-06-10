using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

#if !IOS && !MACCATALYST
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
		=> GetTotalCount();

	int GetTotalCount()
	{
		if (Adapter is null)
		{
			return 0;
		}
		
		var sum = 0;

		//var hasAtLeastOneItem = false;
		var numberOfSections = Adapter.GetNumberOfSections();

		if (HasGlobalHeader && numberOfSections > 0)
		{
			// Make sure that there's at least one section with at least
			// one item, otherwise it's 'empty'
			// The default adapter may always return 1 for number of sections
			// so it's not enough to check that
			for (int s = 0; s < numberOfSections; s++)
			{
				if (Adapter.GetNumberOfItemsInSection(s) > 0)
				{
					sum += 1;
					// If we found one, we can stop looping
					// since we just care to calculate a spot
					// for the header cell if the adapter isn't empty
					break;
				}
			}
		}

		if (Adapter != null)
		{
			for (int s = 0; s < numberOfSections; s++)
			{
				if (ViewSelector.SectionHasHeader(s))
					sum += 1;

				sum += Adapter.GetNumberOfItemsInSection(s);

				if (ViewSelector.SectionHasFooter(s))
					sum += 1;
			}
		}

		// Only count footer if there is already at least one item
		// otherwise the adapter is empty and we shouldn't count it
		if (HasGlobalFooter && sum > 0)
			sum += 1;

		return sum;
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
#endif