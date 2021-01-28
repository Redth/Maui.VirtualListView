using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace XFSlimListView
{
	public class SlimListView : Xamarin.Forms.View
	{
		public SlimListView() : base()
		{

		}

		public Color SeparatorColor
		{
			get => (Color)GetValue(SeparatorColorProperty);
			set => SetValue(SeparatorColorProperty, value);
		}

		public static readonly BindableProperty SeparatorColorProperty =
			BindableProperty.Create(nameof(SeparatorColor), typeof(Color), typeof(SlimListView), Color.Transparent);

		public double SeparatorSize
		{
			get => (double)GetValue(SeparatorSizeProperty);
			set => SetValue(SeparatorSizeProperty, value);
		}

		public static readonly BindableProperty SeparatorSizeProperty =
			BindableProperty.Create(nameof(SeparatorSize), typeof(double), typeof(SlimListView), 0d);



		public ISlimListViewAdapter Adapter
		{
			get => (ISlimListViewAdapter)GetValue(AdapterProperty);
			set => SetValue(AdapterProperty, value);
		}

		public static readonly BindableProperty AdapterProperty =
			BindableProperty.Create(nameof(Adapter), typeof(ISlimListViewAdapter), typeof(SlimListView), default);


		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static readonly BindableProperty HeaderTemplateProperty =
			BindableProperty.Create(nameof(HeaderTemplate), typeof(DataTemplate), typeof(SlimListView), default);

		public DataTemplate FooterTemplate
		{
			get => (DataTemplate)GetValue(FooterTemplateProperty);
			set => SetValue(FooterTemplateProperty, value);
		}

		public static readonly BindableProperty FooterTemplateProperty =
			BindableProperty.Create(nameof(FooterTemplate), typeof(DataTemplate), typeof(SlimListView), default);


		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(SlimListView), default);

		public DataTemplate SectionHeaderTemplate
		{
			get => (DataTemplate)GetValue(SectionHeaderTemplateProperty);
			set => SetValue(SectionHeaderTemplateProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(SlimListView), default);

		public DataTemplate SectionFooterTemplate
		{
			get => (DataTemplate)GetValue(SectionFooterTemplateProperty);
			set => SetValue(SectionFooterTemplateProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateProperty =
			BindableProperty.Create(nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(SlimListView), default);


		public AdapterItemDataTemplateSelector ItemTemplateSelector
		{
			get => (AdapterItemDataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
			set => SetValue(ItemTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty ItemTemplateSelectorProperty =
			BindableProperty.Create(nameof(ItemTemplateSelector), typeof(AdapterItemDataTemplateSelector), typeof(SlimListView), default);

		public AdapterSectionDataTemplateSelector SectionHeaderTemplateSelector
		{
			get => (AdapterSectionDataTemplateSelector)GetValue(SectionHeaderTemplateSelectorProperty);
			set => SetValue(SectionHeaderTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(AdapterSectionDataTemplateSelector), typeof(SlimListView), default);

		public AdapterSectionDataTemplateSelector SectionFooterTemplateSelector
		{
			get => (AdapterSectionDataTemplateSelector)GetValue(SectionFooterTemplateSelectorProperty);
			set => SetValue(SectionFooterTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(AdapterSectionDataTemplateSelector), typeof(SlimListView), default);
	}

	public class PositionInfo
	{
		public int Position { get; set; }

		public PositionType Type { get; set; } = PositionType.Item;

		public object BindingContext { get; set; }

		public enum PositionType
		{
			Header,
			SectionHeader,
			Item,
			SectionFooter,
			Footer
		}
	}

	public class PositionTemplateSelector
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

		public PositionInfo GetInfo(ISlimListViewAdapter adapter, int position)
		{
			var linear = 0;

			if (HeaderTemplate != null)
			{
				if (position == 0)
					return new PositionInfo
					{
						Position = position,
						Type = PositionInfo.PositionType.Header
					};

				linear++;
			}

			for (int s = 0; s < adapter.Sections; s++)
			{
				if (SectionHeaderTemplate != null || SectionHeaderTemplateSelector != null)
				{
					if (position == linear)
						return new PositionInfo
						{
							Position = position,
							BindingContext = adapter.Section(s),
							Type = PositionInfo.PositionType.SectionHeader
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
						Position = position,
						BindingContext = adapter.Item(s, itemIndex),
						Type = PositionInfo.PositionType.Item
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
							Type = PositionInfo.PositionType.SectionFooter
						};

					linear++;
				}
			}

			return new PositionInfo
			{
				Position = position,
				Type = PositionInfo.PositionType.Footer
			};
		}

		public DataTemplate GetTemplate(ISlimListViewAdapter adapter, int position)
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
	}
}
