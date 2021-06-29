using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Maui.Controls
{

	public partial class VirtualListView : View, IVirtualListView, IVirtualListViewSelector
	{
		static VirtualListView()
		{

		}

		public IVirtualListViewAdapter Adapter
		{
			get => (IVirtualListViewAdapter)GetValue(AdapterProperty);
			set => SetValue(AdapterProperty, value);
		}

		public static readonly BindableProperty AdapterProperty =
			BindableProperty.Create(nameof(Adapter), typeof(IVirtualListViewAdapter), typeof(VirtualListView), default);


		public IView GlobalHeader
		{
			get => (IView)GetValue(GlobalHeaderProperty);
			set => SetValue(GlobalHeaderProperty, value);
		}

		public static readonly BindableProperty GlobalHeaderProperty =
			BindableProperty.Create(nameof(GlobalHeader), typeof(IView), typeof(VirtualListView), default);

		public IView GlobalFooter
		{
			get => (IView)GetValue(GlobalFooterProperty);
			set => SetValue(GlobalFooterProperty, value);
		}

		public static readonly BindableProperty GlobalFooterProperty =
			BindableProperty.Create(nameof(GlobalFooter), typeof(IView), typeof(VirtualListView), default);



		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewItemTemplateSelector ItemTemplateSelector
		{
			get => (VirtualListViewItemTemplateSelector)GetValue(ItemTemplateSelectorProperty);
			set => SetValue(ItemTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty ItemTemplateSelectorProperty =
			BindableProperty.Create(nameof(ItemTemplateSelector), typeof(VirtualListViewItemTemplateSelector), typeof(VirtualListView), default);



		public DataTemplate SectionHeaderTemplate
		{
			get => (DataTemplate)GetValue(SectionHeaderTemplateProperty);
			set => SetValue(SectionHeaderTemplateProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewSectionTemplateSelector SectionHeaderTemplateSelector
		{
			get => (VirtualListViewSectionTemplateSelector)GetValue(SectionHeaderTemplateSelectorProperty);
			set => SetValue(SectionHeaderTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(VirtualListViewSectionTemplateSelector), typeof(VirtualListView), default);



		public DataTemplate SectionFooterTemplate
		{
			get => (DataTemplate)GetValue(SectionFooterTemplateProperty);
			set => SetValue(SectionFooterTemplateProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateProperty =
			BindableProperty.Create(nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public VirtualListViewSectionTemplateSelector SectionFooterTemplateSelector
		{
			get => (VirtualListViewSectionTemplateSelector)GetValue(SectionFooterTemplateSelectorProperty);
			set => SetValue(SectionFooterTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionFooterTemplateSelector), typeof(VirtualListViewSectionTemplateSelector), typeof(VirtualListView), default);


		public SelectionMode SelectionMode
		{
			get => (SelectionMode)GetValue(SelectionModeProperty);
			set => SetValue(SelectionModeProperty, value);
		}

		public static readonly BindableProperty SelectionModeProperty =
			BindableProperty.Create(nameof(SelectionMode), typeof(SelectionMode), typeof(VirtualListView), SelectionMode.None);

		public event EventHandler<SelectedItemsChangedEventArgs> SelectedItemsChanged;

		readonly object selectedItemsLocker = new object();
		readonly List<ItemPosition> selectedItems = new List<ItemPosition>();

		public IReadOnlyList<ItemPosition> SelectedItems
		{
			get
			{
				if (SelectionMode == SelectionMode.None)
					return new List<ItemPosition>();

				lock (selectedItemsLocker)
					return selectedItems.AsReadOnly();
			}
		}

		public IVirtualListViewSelector ViewSelector => this;

		public IView Header => GlobalHeader;
		public IView Footer => GlobalFooter;

		public bool IsItemSelected(int sectionIndex, int itemIndex)
		{
			if (SelectionMode == SelectionMode.None)
				return false;

			lock (selectedItemsLocker)
				return selectedItems.Contains(new ItemPosition(sectionIndex, itemIndex));
		}



		public void SetSelected(params ItemPosition[] paths)
		{
			if (SelectionMode == SelectionMode.None)
				return;

			var prev = new List<ItemPosition>(SelectedItems);

			IReadOnlyList<ItemPosition> current;

			lock (selectedItemsLocker)
			{
				foreach (var path in paths)
				{
					if (!selectedItems.Contains(path))
						selectedItems.Add(path);
				}

				current = SelectedItems ?? new List<ItemPosition>();
			}

			// Raise event
			SelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(prev, current));
		}

		public void SetDeselected(params ItemPosition[] paths)
		{
			if (SelectionMode == SelectionMode.None)
				return;

			var prev = new List<ItemPosition>(SelectedItems);

			IReadOnlyList<ItemPosition> current;

			lock (selectedItemsLocker)
			{
				foreach (var path in paths)
				{
					if (selectedItems.Contains(path))
						selectedItems.Remove(path);
				}

				current = SelectedItems ?? new List<ItemPosition>();
			}

			// Raise event
			SelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(prev, current));
		}

		public event EventHandler<ScrolledEventArgs> Scrolled;
		public event EventHandler DataInvalidated;

		internal void RaiseScrolled(ScrolledEventArgs args)
			=> Scrolled?.Invoke(this, args);

		public void InvalidateData()
		{
			DataInvalidated?.Invoke(this, new EventArgs());
		}

		public IView ViewForItem(int sectionIndex, int itemIndex)
			=> ItemTemplateSelector?.SelectTemplate(Adapter, sectionIndex, itemIndex).CreateContent() as IView
				?? ItemTemplate.CreateContent() as IView;

		public IView ViewForSectionHeader(int sectionIndex)
		=> SectionHeaderTemplateSelector?.SelectTemplate(Adapter, sectionIndex)?.CreateContent() as IView
				?? SectionHeaderTemplate?.CreateContent() as IView;

		public bool SectionHasHeader(int sectionIndex)
			=> SectionHeaderTemplateSelector != null || SectionHeaderTemplate != null;

		public IView ViewForSectionFooter(int sectionIndex)
			=> SectionFooterTemplateSelector?.SelectTemplate(Adapter, sectionIndex)?.CreateContent() as IView
				?? SectionFooterTemplate?.CreateContent() as IView;

		public bool SectionHasFooter(int sectionIndex)
			=> SectionFooterTemplateSelector != null || SectionFooterTemplate != null;


		readonly List<DataTemplate> cachedTemplates = new List<DataTemplate>();

		string CacheTemplate(string prefix, DataTemplate template)
		{
			if (template == null)
				throw new ArgumentNullException(nameof(template));

			var index = cachedTemplates.IndexOf(template);

			if (index >= 0)
				return prefix + index.ToString();

			cachedTemplates.Add(template);
			index = cachedTemplates.IndexOf(template);
			return prefix + index.ToString();
		}

		public string ReuseIdIdForItem(int sectionIndex, int itemIndex)
			=> CacheTemplate("INDEX_", ItemTemplateSelector?.SelectTemplate(Adapter, sectionIndex, itemIndex) ?? ItemTemplate);

		public string ReuseIdIdForSectionHeader(int sectionIndex)
			=> CacheTemplate("SECTIONHEADER_", SectionHeaderTemplateSelector?.SelectTemplate(Adapter, sectionIndex) ?? SectionHeaderTemplate);

		public string ReuseIdIdForSectionFooter(int sectionIndex)
			=> CacheTemplate("SECTIONFOOTER_", SectionHeaderTemplateSelector?.SelectTemplate(Adapter, sectionIndex) ?? SectionHeaderTemplate);
	}
}
