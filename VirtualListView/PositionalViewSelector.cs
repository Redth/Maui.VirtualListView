using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal class PositionalViewSelector
{
	public readonly IVirtualListView VirtualListView;
	public IVirtualListViewAdapter Adapter => VirtualListView?.Adapter;
	public IVirtualListViewSelector ViewSelector => VirtualListView?.ViewSelector;
	public bool HasGlobalHeader => VirtualListView?.Header != null;
	public bool HasGlobalFooter => VirtualListView?.Footer != null;

	public PositionalViewSelector(IVirtualListView virtualListView)
	{
		VirtualListView = virtualListView;
	}

	//int? cachedTotalCount;
	public int TotalCount
		=> GetTotalCount();

	int GetTotalCount()
	{
		var sum = 0;

		if (HasGlobalHeader)
			sum += 1;

		if (Adapter != null)
		{
			for (int s = 0; s < Adapter.GetNumberOfSections(); s++)
			{
				if (ViewSelector.SectionHasHeader(s))
					sum += 1;

				sum += Adapter.GetNumberOfItemsInSection(s);

				if (ViewSelector.SectionHasFooter(s))
					sum += 1;
			}
		}

		if (HasGlobalFooter)
			sum += 1;

		return sum;
	}

#if IOS || MACCATALYST

	internal int GetNumberOfSections()
	{
		var sections = Math.Max(Adapter.GetNumberOfSections(), 0);
		if (HasGlobalHeader)
			sections++;
		if (HasGlobalFooter)
			sections++;

		return sections;
	}

	internal int GetNumberOfItemsForSection(int sectionIndex)
	{
		var realSection = sectionIndex;

		if (HasGlobalHeader)
		{
			if (sectionIndex == 0)
				return 1;

			realSection--;
		}

		if (HasGlobalFooter)
		{
			if (sectionIndex >= GetNumberOfSections() - 1)
				return 1;
		}

		var itemsCount = Adapter?.GetNumberOfItemsInSection((int)realSection) ?? 0;

		if (ViewSelector?.SectionHasHeader((int)realSection) ?? false)
			itemsCount++;

		if (ViewSelector?.SectionHasFooter((int)realSection) ?? false)
			itemsCount++;

		return itemsCount;
	}

	public PositionInfo GetInfo(int sectionIndex, int itemIndex)
	{
		var realSectionIndex = sectionIndex;

		if (HasGlobalHeader)
		{
			if (sectionIndex == 0)
				return PositionInfo.ForHeader(0);

			// Global header takes up a section, real adapter is 1 less
			realSectionIndex--;
		}

		var realNumberOfSections = Adapter?.GetNumberOfSections() ?? 0;

		if (HasGlobalFooter)
		{
			if (realSectionIndex >= realNumberOfSections)
				return PositionInfo.ForFooter(-1);
		}


		var realItemsInSection = Adapter?.GetNumberOfItemsInSection(realSectionIndex) ?? 0;

		var realItemIndex = itemIndex;

		var itemsAdded = 0;

		if (ViewSelector?.SectionHasHeader(realSectionIndex) ?? false)
		{
			itemsAdded++;
			realItemIndex--;

			if (itemIndex == 0)
				return PositionInfo.ForSectionHeader(-1, realSectionIndex, realItemsInSection);
		}

		if (ViewSelector.SectionHasFooter(realSectionIndex))
		{
			itemsAdded++;

			if (itemIndex >= realItemsInSection + itemsAdded - 1)
				return PositionInfo.ForSectionFooter(-1, realSectionIndex, realItemsInSection);
		}

		return PositionInfo.ForItem(-1, realSectionIndex, realItemIndex, Adapter.GetNumberOfItemsInSection(realSectionIndex), realNumberOfSections);
	}

	public Foundation.NSIndexPath GetIndexPath(int positionSectionIndex, int positionItemIndex)
	{
		var realSectionIndex = positionSectionIndex;
		var realItemIndex = positionItemIndex;

		// Global header takes up one section
		if (HasGlobalHeader)
			realSectionIndex++;

		// If the section has a header, the real item index is +1
		if (ViewSelector?.SectionHasHeader(positionSectionIndex) ?? false)
			realItemIndex++;

		return Foundation.NSIndexPath.FromItemSection(realItemIndex, realSectionIndex);
	}

#else

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
#endif

}
