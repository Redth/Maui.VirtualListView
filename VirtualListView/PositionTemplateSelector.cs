using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	internal static class PositionTemplateSelector
	{
		public static PositionInfo GetInfo(
			IVirtualListViewAdapter adapter,
			int position,
			bool hasGlobalHeader,
			bool hasGlobalFooter,
			bool hasSectionHeader,
			bool hasSectionFooter)
		{
			if (adapter == null)
				return null;

			var linear = 0;

			var numberSections = adapter.Sections;

			if (hasGlobalHeader)
			{
				if (position == 0)
					return PositionInfo.ForHeader(position);

				linear++;
			}

			for (int s = 0; s < numberSections; s++)
			{
				if (hasSectionHeader)
				{
					if (position == linear)
						return PositionInfo.ForSectionHeader(position, s);

					linear++;
				}

				var itemsInSection = adapter.ItemsForSection(s);

				// It's an item in the section, return it for this item
				if (position < linear + itemsInSection)
				{
					var itemIndex = position - linear;

					return PositionInfo.ForItem(position, s, itemIndex, itemsInSection, numberSections);
				}

				linear += itemsInSection;

				if (hasSectionFooter)
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

		public static (int realSectionIndex, int realItemIndex) GetRealIndexPath(
			IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex,
			bool hasGlobalHeader,
			bool hasGlobalFooter,
			bool hasSectionHeader,
			bool hasSectionFooter)
		{
			var realSectionIndex = sectionIndex;

			if (hasGlobalHeader)
			{
				if (sectionIndex == 0)
					return (-1, -1);

				// Global header takes up a section, real adapter is 1 less
				realSectionIndex--;
			}

			var realNumberOfSections = adapter.Sections;

			if (hasGlobalFooter)
			{
				if (realSectionIndex >= realNumberOfSections)
					return (-1, -1);
			}

			var realItemsInSection = adapter.ItemsForSection(realSectionIndex);

			var realItemIndex = itemIndex;

			if (hasSectionHeader)
			{
				realItemIndex--;

				if (itemIndex == 0)
					return (-1, -1);
			}

			if (hasSectionFooter)
			{
				if (realItemIndex >= realItemsInSection)
					return (-1, -1);
			}

			return (realSectionIndex, realItemIndex);
		}

		public static (IViewTemplate template, PositionInfo info) GetTemplateAndInfo(
			IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex,
			IViewTemplate header,
			IViewTemplate footer,
			IViewTemplate sectionHeaderTemplate,
			IViewTemplate sectionFooterTemplate,
			IViewTemplate itemTemplate)
		{
			var realSectionIndex = sectionIndex;

			if (header != null)
			{
				if (sectionIndex == 0)
				{
					return (header, new PositionInfo
					{
						Kind = PositionKind.Header
					});
				}

				// Global header takes up a section, real adapter is 1 less
				realSectionIndex--;
			}

			var realNumberOfSections = adapter.Sections;

			if (footer != null)
			{
				if (realSectionIndex >= realNumberOfSections)
				{
					return (footer, new PositionInfo
					{
						Kind = PositionKind.Footer
					});
				}
			}


			var realItemsInSection = adapter.ItemsForSection(realSectionIndex);

			var realItemIndex = itemIndex;

			var itemsAdded = 0;

			if (sectionHeaderTemplate != null)
			{
				itemsAdded++;
				realItemIndex--;

				if (itemIndex == 0)
				{
					return ((sectionHeaderTemplate as SectionTemplateSelector)?.SelectGroupTemplate(adapter, realSectionIndex) ?? sectionHeaderTemplate,
						new PositionInfo
						{
							Kind = PositionKind.SectionHeader,
							ItemsInSection = realItemsInSection,
							SectionIndex = realSectionIndex,
							NumberOfSections = realNumberOfSections
						});
				}
			}

			if (sectionFooterTemplate != null)
			{
				itemsAdded++;

				if (itemIndex >= realItemsInSection + itemsAdded - 1)
				{
					return ((sectionFooterTemplate as SectionTemplateSelector)?.SelectGroupTemplate(adapter, realSectionIndex) ?? sectionFooterTemplate,
						new PositionInfo
						{
							Kind = PositionKind.SectionFooter,
							ItemsInSection = realItemsInSection,
							SectionIndex = realSectionIndex,
							NumberOfSections = realNumberOfSections
						});
				}
			}

			return ((itemTemplate as ItemTemplateSelector)?.SelectItemTemplate(adapter, realSectionIndex, realItemIndex) ?? itemTemplate,
				new PositionInfo
				{
					Kind = PositionKind.Item,
					ItemsInSection = realItemsInSection,
					SectionIndex = realSectionIndex,
					ItemIndex = realItemIndex,
					NumberOfSections = realNumberOfSections,
				});
		}

		public static IViewTemplate GetTemplate(
			IVirtualListViewAdapter adapter, int position,
			IViewTemplate header,
			IViewTemplate footer,
			IViewTemplate sectionHeaderTemplate,
			IViewTemplate sectionFooterTemplate,
			IViewTemplate itemTemplate)
		{
			if (position == 0)
			{
				if (header != null)
					return header;

				if (sectionHeaderTemplate is SectionTemplateSelector headerTemplateSelector)
					return headerTemplateSelector.SelectGroupTemplate(adapter, 0);

				if (sectionHeaderTemplate != null)
					return sectionHeaderTemplate;
			}

			var linear = 0;

			if (header != null)
				linear++;

			for (int s = 0; s < adapter.Sections; s++)
			{
				if (sectionHeaderTemplate != null)
				{
					if (position == linear)
					{
						return (sectionHeaderTemplate as SectionTemplateSelector)?.SelectGroupTemplate(adapter, s)
							?? sectionHeaderTemplate;
					}
					linear++;
				}

				var itemsInSection = adapter.ItemsForSection(s);

				// It's an item in the section, return it for this item
				if (position < linear + itemsInSection)
				{
					var itemIndex = position - linear;
					return (itemTemplate as ItemTemplateSelector)?.SelectItemTemplate(adapter, s, itemIndex)
						?? itemTemplate;
				}

				linear += itemsInSection;

				if (sectionFooterTemplate != null)
				{
					if (position == linear)
						return (sectionFooterTemplate as SectionTemplateSelector)?.SelectGroupTemplate(adapter, s)
							?? sectionFooterTemplate;
					linear++;
				}
			}

			return footer;
		}

		public static int GetTotalCount(
			IVirtualListViewAdapter adapter,
			bool hasGlobalHeader,
			bool hasGlobalFooter,
			bool hasSectionHeader,
			bool hasSectionFooter)
		{
			if (adapter == null)
				return 0;

			var sum = 0;

			if (hasGlobalHeader)
				sum += 1;

			if (adapter != null)
			{
				for (int i = 0; i < adapter.Sections; i++)
				{
					if (hasSectionHeader)
						sum += 1;

					sum += adapter.ItemsForSection(i);

					if (hasSectionFooter)
						sum += 1;
				}
			}

			if (hasGlobalFooter)
				sum += 1;

			return sum;
		}
	}
}
