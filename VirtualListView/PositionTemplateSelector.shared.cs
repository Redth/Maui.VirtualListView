using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Xamarin.CommunityToolkit.UI.Views
{
	internal class PositionTemplateSelector
	{
		public DataTemplate HeaderTemplate { get; set; }
		public DataTemplate FooterTemplate { get; set; }

		public DataTemplate SectionHeaderTemplate { get; set; }
		public DataTemplate SectionFooterTemplate { get; set; }
		public DataTemplate ItemTemplate { get; set; }

		public AdapterItemDataTemplateSelector ItemTemplateSelector { get; set; }
		public AdapterSectionDataTemplateSelector SectionHeaderTemplateSelector { get; set; }
		public AdapterSectionDataTemplateSelector SectionFooterTemplateSelector { get; set; }

		public bool HasSectionHeader
			=> SectionHeaderTemplate != null || SectionHeaderTemplateSelector != null;

		public bool HasSectionFooter
			=> SectionFooterTemplate != null || SectionFooterTemplateSelector != null;

		public bool HasGlobalHeader
			=> HeaderTemplate != null;

		public bool HasGlobalFooter
			=> FooterTemplate != null;

		public PositionInfo GetInfo(IVirtualListViewAdapter adapter, int position)
		{
			if (adapter == null)
				return null;

			var linear = 0;

			var numberSections = adapter.Sections;

			if (HeaderTemplate != null)
			{
				if (position == 0)
					return new PositionInfo
					{
						Position = position,
						Kind = PositionKind.Header
					};

				linear++;
			}

			for (int s = 0; s < numberSections; s++)
			{
				if (SectionHeaderTemplate != null || SectionHeaderTemplateSelector != null)
				{
					if (position == linear)
						return new PositionInfo
						{
							SectionIndex = s,
							Position = position,
							BindingContext = adapter.Section(s),
							Kind = PositionKind.SectionHeader
						};

					linear++;
				}

				var itemsInSection = adapter.ItemsForSection(s);

				// It's an item in the section, return it for this item
				if (position < linear + itemsInSection)
				{
					var itemIndex = position - linear;

					return new PositionInfo
					{
						SectionIndex = s,
						NumberOfSections = numberSections,
						ItemIndex = itemIndex,
						ItemsInSection = itemsInSection,
						Position = position,
						BindingContext = adapter.Item(s, itemIndex),
						Kind = PositionKind.Item
					};
				}

				linear += itemsInSection;

				if (SectionFooterTemplate != null || SectionFooterTemplateSelector != null)
				{
					if (position == linear)
						return new PositionInfo
						{
							SectionIndex = s,
							Position = position,
							BindingContext = adapter.Section(s),
							Kind = PositionKind.SectionFooter
						};

					linear++;
				}
			}

			return new PositionInfo
			{
				Position = position,
				Kind = PositionKind.Footer
			};
		}

		public (int realSectionIndex, int realItemIndex) GetRealIndexPath(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
		{
			var realSectionIndex = sectionIndex;

			if (HasGlobalHeader)
			{
				if (sectionIndex == 0)
					return (-1, -1);

				// Global header takes up a section, real adapter is 1 less
				realSectionIndex--;
			}

			var realNumberOfSections = adapter.Sections;

			if (HasGlobalFooter)
			{
				if (realSectionIndex >= realNumberOfSections)
					return (-1, -1);
			}

			var realItemsInSection = adapter.ItemsForSection(realSectionIndex);

			var realItemIndex = itemIndex;

			if (HasSectionHeader)
			{
				realItemIndex--;

				if (itemIndex == 0)
					return (-1, -1);
			}

			if (HasSectionFooter)
			{
				if (realItemIndex >= realItemsInSection)
					return (-1, -1);
			}

			return (realSectionIndex, realItemIndex);
		}

		public (DataTemplate template, PositionInfo info) GetTemplateAndInfo(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
		{
			var realSectionIndex = sectionIndex;

			if (HasGlobalHeader)
			{
				if (sectionIndex == 0)
				{
					return (HeaderTemplate, new PositionInfo
					{
						Kind = PositionKind.Header
					});
				}

				// Global header takes up a section, real adapter is 1 less
				realSectionIndex--;
			}

			var realNumberOfSections = adapter.Sections;

			if (HasGlobalFooter)
			{
				if (realSectionIndex >= realNumberOfSections)
				{
					return (FooterTemplate, new PositionInfo
					{
						Kind = PositionKind.Footer
					});
				}
			}


			var realItemsInSection = adapter.ItemsForSection(realSectionIndex);

			var realItemIndex = itemIndex;

			var itemsAdded = 0;

			if (HasSectionHeader)
			{
				itemsAdded++;
				realItemIndex--;

				if (itemIndex == 0)
				{
					return (SectionHeaderTemplateSelector?.SelectGroupTemplate(adapter, realSectionIndex) ?? SectionHeaderTemplate,
						new PositionInfo
						{
							Kind = PositionKind.SectionHeader,
							ItemsInSection = realItemsInSection,
							SectionIndex = realSectionIndex,
							BindingContext = adapter.Section(realSectionIndex),
							NumberOfSections = realNumberOfSections
						});
				}
			}

			if (HasSectionFooter)
			{
				itemsAdded++;

				if (itemIndex >= realItemsInSection + itemsAdded - 1)
				{
					return (SectionFooterTemplateSelector?.SelectGroupTemplate(adapter, realSectionIndex) ?? SectionFooterTemplate,
						new PositionInfo
						{
							Kind = PositionKind.SectionFooter,
							ItemsInSection = realItemsInSection,
							SectionIndex = realSectionIndex,
							BindingContext = adapter.Section(realSectionIndex),
							NumberOfSections = realNumberOfSections
						});
				}
			}

			return (ItemTemplateSelector?.SelectItemTemplate(adapter, realSectionIndex, realItemIndex) ?? ItemTemplate,
				new PositionInfo
				{
					Kind = PositionKind.Item,
					ItemsInSection = realItemsInSection,
					SectionIndex = realSectionIndex,
					ItemIndex = realItemIndex,
					BindingContext = adapter.Item(realSectionIndex, realItemIndex),
					NumberOfSections = realNumberOfSections,
				});
		}

		public DataTemplate GetTemplate(IVirtualListViewAdapter adapter, int position)
		{
			if (position == 0)
			{
				if (HeaderTemplate != null)
					return HeaderTemplate;

				if (SectionHeaderTemplateSelector != null)
					return SectionHeaderTemplateSelector.SelectGroupTemplate(adapter, 0);

				if (SectionHeaderTemplate != null)
					return SectionHeaderTemplate;
			}

			var linear = 0;

			if (HeaderTemplate != null)
				linear++;

			for (int s = 0; s < adapter.Sections; s++)
			{
				if (HasSectionHeader)
				{
					if (position == linear)
						return SectionHeaderTemplateSelector?.SelectGroupTemplate(adapter, s)
							?? SectionHeaderTemplate;
					linear++;
				}

				var itemsInSection = adapter.ItemsForSection(s);

				// It's an item in the section, return it for this item
				if (position < linear + itemsInSection)
				{
					var itemIndex = position - linear;
					return ItemTemplateSelector?.SelectItemTemplate(adapter, s, itemIndex) ?? ItemTemplate;
				}

				linear += itemsInSection;

				if (HasSectionFooter)
				{
					if (position == linear)
						return SectionFooterTemplateSelector?.SelectGroupTemplate(adapter, s)
							?? SectionFooterTemplate;
					linear++;
				}
			}

			return FooterTemplate;
		}

		public int GetTotalCount(IVirtualListViewAdapter adapter)
		{
			if (adapter == null)
				return 0;

			var sum = 0;

			if (HeaderTemplate != null)
				sum += 1;

			if (adapter != null)
			{
				for (int i = 0; i < adapter.Sections; i++)
				{
					if (SectionHeaderTemplate != null || SectionHeaderTemplateSelector != null)
						sum += 1;

					sum += adapter.ItemsForSection(i);

					if (SectionFooterTemplate != null || SectionFooterTemplateSelector != null)
						sum += 1;
				}
			}

			if (FooterTemplate != null)
				sum += 1;

			return sum;
		}
	}
}
