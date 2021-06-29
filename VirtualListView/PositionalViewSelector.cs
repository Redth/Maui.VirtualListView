using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	internal class PositionalViewSelector
	{
		public readonly IVirtualListViewAdapter Adapter;
		public readonly IVirtualListViewSelector ViewSelector;
		public readonly Func<bool> HasGlobalHeader;
		public readonly Func<bool> HasGlobalFooter;

		public PositionalViewSelector(IVirtualListViewAdapter adapter, IVirtualListViewSelector viewSelector, Func<bool> hasGlobalHeader, Func<bool> hasGlobalFooter)
		{
			Adapter = adapter;
			ViewSelector = viewSelector;
			HasGlobalFooter = hasGlobalFooter;
			HasGlobalHeader = hasGlobalHeader;
		}

		const string GlobalHeaderReuseId = "GLOBAL_HEADER";
		const string GlobalFooterReuseId = "GLOBAL_FOOTER";

		readonly List<string> ReuseIdentifiers = new () { GlobalHeaderReuseId, GlobalFooterReuseId };

		readonly Dictionary<int, int> CachedItemsInSection = new ();

		int CachedItemsForSection(int sectionIndex)
		{
			if (CachedItemsInSection.TryGetValue(sectionIndex, out var n))
				return n;

			n = Adapter.ItemsForSection(sectionIndex);
			CachedItemsInSection.TryAdd(sectionIndex, n);
			return n;
		}

		public int GetReuseId(PositionKind kind, int sectionIndex, int itemIndex)
		{
			var reuseIdStr = kind switch
			{
				PositionKind.Item => ViewSelector.ReuseIdIdForItem(sectionIndex, itemIndex),
				PositionKind.SectionHeader => ViewSelector.ReuseIdIdForSectionHeader(sectionIndex),
				PositionKind.SectionFooter => ViewSelector.ReuseIdIdForSectionFooter(sectionIndex),
				PositionKind.Header => GlobalHeaderReuseId,
				PositionKind.Footer => GlobalFooterReuseId,
				_ => default
			};

			var id = ReuseIdentifiers.IndexOf(reuseIdStr);

			if (id < 0)
			{
				ReuseIdentifiers.Add(reuseIdStr);
				id = ReuseIdentifiers.IndexOf(reuseIdStr);
			}

			return id;
		}

		public void Reset()
		{
			ReuseIdentifiers.Clear();
			ReuseIdentifiers.AddRange(new[] { GlobalHeaderReuseId, GlobalFooterReuseId });
			CachedItemsInSection.Clear();
		}

		public PositionInfo GetInfo(int position)
		{
			if (Adapter == null)
				return null;

			var linear = 0;

			var numberSections = Adapter.Sections;

			if (HasGlobalHeader())
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

				var itemsInSection = CachedItemsForSection(s);

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

		public (int realSectionIndex, int realItemIndex) GetRealIndexPath(int sectionIndex, int itemIndex)
		{
			var realSectionIndex = sectionIndex;

			if (HasGlobalHeader())
			{
				if (sectionIndex == 0)
					return (-1, -1);

				// Global header takes up a section, real adapter is 1 less
				realSectionIndex--;
			}

			var realNumberOfSections = Adapter.Sections;

			if (HasGlobalFooter())
			{
				if (realSectionIndex >= realNumberOfSections)
					return (-1, -1);
			}

			var realItemsInSection = CachedItemsForSection(realSectionIndex);

			var realItemIndex = itemIndex;

			if (ViewSelector.SectionHasHeader(sectionIndex))
			{
				realItemIndex--;

				if (itemIndex == 0)
					return (-1, -1);
			}

			if (ViewSelector.SectionHasFooter(sectionIndex))
			{
				if (realItemIndex >= realItemsInSection)
					return (-1, -1);
			}

			return (realSectionIndex, realItemIndex);
		}

		public int GetTotalCount()
		{
			if (Adapter == null)
				return 0;

			var sum = 0;

			if (HasGlobalHeader())
				sum += 1;

			if (Adapter != null)
			{
				for (int s = 0; s < Adapter.Sections; s++)
				{
					if (ViewSelector.SectionHasHeader(s))
						sum += 1;

					sum += CachedItemsForSection(s);

					if (ViewSelector.SectionHasFooter(s))
						sum += 1;
				}
			}

			if (HasGlobalFooter())
				sum += 1;

			return sum;
		}
	}
}
