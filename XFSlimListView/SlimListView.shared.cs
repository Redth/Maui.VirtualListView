using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace XFSlimListView
{
	public class PositionData : Element
	{
		public PositionData()
		{ }

		public static readonly BindableProperty SectionIndexProperty =
			BindableProperty.Create(nameof(SectionIndex), typeof(int), typeof(SlimListView),-1);

		public int SectionIndex
		{
			get => (int)GetValue(SectionIndexProperty);
			set => SetValue(SectionIndexProperty, value);
		}

		public static readonly BindableProperty ItemIndexProperty =
			BindableProperty.Create(nameof(ItemIndex), typeof(int), typeof(SlimListView), -1);

		public int ItemIndex
		{
			get => (int)GetValue(ItemIndexProperty);
			set => SetValue(ItemIndexProperty, value);
		}

		public static readonly BindableProperty IsGlobalHeaderProperty =
			BindableProperty.Create(nameof(IsGlobalHeader), typeof(bool), typeof(SlimListView), false);

		public bool IsGlobalHeader
		{
			get => (bool)GetValue(IsGlobalHeaderProperty);
			set => SetValue(IsGlobalHeaderProperty, value);
		}


		public static readonly BindableProperty IsGlobalFooterProperty =
			BindableProperty.Create(nameof(IsGlobalFooter), typeof(bool), typeof(SlimListView), false);

		public bool IsGlobalFooter
		{
			get => (bool)GetValue(IsGlobalFooterProperty);
			set => SetValue(IsGlobalFooterProperty, value);
		}

		public static readonly BindableProperty IsSectionHeaderProperty =
			BindableProperty.Create(nameof(IsSectionHeader), typeof(bool), typeof(SlimListView), false);

		public bool IsSectionHeader
		{
			get => (bool)GetValue(IsSectionHeaderProperty);
			set => SetValue(IsSectionHeaderProperty, value);
		}


		public static readonly BindableProperty IsSectionFooterProperty =
			BindableProperty.Create(nameof(IsSectionFooter), typeof(bool), typeof(SlimListView), false);

		public bool IsSectionFooter
		{
			get => (bool)GetValue(IsSectionFooterProperty);
			set => SetValue(IsSectionFooterProperty, value);
		}


		public static readonly BindableProperty IsItemProperty =
			BindableProperty.Create(nameof(IsItem), typeof(bool), typeof(SlimListView), false);

		public bool IsItem
		{
			get => (bool)GetValue(IsItemProperty);
			set => SetValue(IsItemProperty, value);
		}


		public static readonly BindableProperty IsLastItemInSectionProperty =
			BindableProperty.Create(nameof(IsLastItemInSection), typeof(bool), typeof(SlimListView), false);

		public bool IsLastItemInSection
		{
			get => (bool)GetValue(IsLastItemInSectionProperty);
			set => SetValue(IsLastItemInSectionProperty, value);
		}

		public static readonly BindableProperty IsNotLastItemInSectionProperty =
			BindableProperty.Create(nameof(IsNotLastItemInSection), typeof(bool), typeof(SlimListView), true);

		public bool IsNotLastItemInSection
		{
			get => (bool)GetValue(IsNotLastItemInSectionProperty);
			set => SetValue(IsNotLastItemInSectionProperty, value);
		}


		public static readonly BindableProperty IsFirstItemInSectionProperty =
			BindableProperty.Create(nameof(IsFirstItemInSection), typeof(bool), typeof(SlimListView), false);

		public bool IsFirstItemInSection
		{
			get => (bool)GetValue(IsFirstItemInSectionProperty);
			set => SetValue(IsFirstItemInSectionProperty, value);
		}


		public static readonly BindableProperty IsNotFirstItemInSectionProperty =
			BindableProperty.Create(nameof(IsNotFirstItemInSection), typeof(bool), typeof(SlimListView), true);

		public bool IsNotFirstItemInSection
		{
			get => (bool)GetValue(IsNotFirstItemInSectionProperty);
			set => SetValue(IsNotFirstItemInSectionProperty, value);
		}


		public static readonly BindableProperty PositionKindProperty =
			BindableProperty.Create(nameof(PositionKind), typeof(PositionKind), typeof(SlimListView), PositionKind.Item);

		public PositionKind PositionKind
		{
			get => (PositionKind)GetValue(PositionKindProperty);
			set => SetValue(PositionKindProperty, value);
		}

		public void Update(PositionInfo info)
		{
			PositionKind = info.Kind;

			if (info.Kind == PositionKind.Item)
			{
				IsLastItemInSection = info.ItemIndex >= info.ItemsInSection - 1;
				IsNotLastItemInSection = !IsLastItemInSection;
				IsFirstItemInSection = info.ItemIndex == 0;
				IsNotFirstItemInSection = !IsFirstItemInSection;
				ItemIndex = info.ItemIndex;
				SectionIndex = info.SectionIndex;
			}
			else
			{
				IsLastItemInSection = false;
				IsNotLastItemInSection = false;
				IsFirstItemInSection = false;
				IsNotFirstItemInSection = false;
				ItemIndex = -1;
				SectionIndex = -1;
			}

			IsItem = info.Kind == PositionKind.Item;
			IsGlobalHeader = info.Kind == PositionKind.Header;
			IsGlobalFooter = info.Kind == PositionKind.Footer;
			IsSectionHeader = info.Kind == PositionKind.SectionHeader;
			IsSectionFooter = info.Kind == PositionKind.SectionFooter;
		}
	}

	public class SlimListView : Xamarin.Forms.View
	{
		public SlimListView() : base()
		{
		}

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

	public enum PositionKind
	{
		Header,
		SectionHeader,
		Item,
		SectionFooter,
		Footer
	}

	public class PositionInfo
	{
		public int Position { get; set; } = -1;

		public PositionKind Kind { get; set; } = PositionKind.Item;

		public object BindingContext { get; set; }

		public int SectionIndex { get; set; } = -1;

		public int NumberOfSections { get; set; } = 0;

		public int ItemIndex { get; set; } = -1;

		public int ItemsInSection { get; set; } = 0;
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
