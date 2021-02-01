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
						SectionIndex = 0,
						NumberOfSections = -1,
						ItemIndex = 0,
						ItemsInSection = 1,
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
					return ItemTemplateSelector?.SelectItemTemplate(adapter, s, itemIndex);
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
