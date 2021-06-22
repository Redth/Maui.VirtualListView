using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Maui.Controls
{

	public partial class VirtualListView : View, IView
	{
		public IVirtualListViewAdapter Adapter
		{
			get => (IVirtualListViewAdapter)GetValue(AdapterProperty);
			set => SetValue(AdapterProperty, value);
		}

		public static readonly BindableProperty AdapterProperty =
			BindableProperty.Create(nameof(Adapter), typeof(IVirtualListViewAdapter), typeof(VirtualListView), default);


		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static readonly BindableProperty HeaderTemplateProperty =
			BindableProperty.Create(nameof(HeaderTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public DataTemplate FooterTemplate
		{
			get => (DataTemplate)GetValue(FooterTemplateProperty);
			set => SetValue(FooterTemplateProperty, value);
		}

		public static readonly BindableProperty FooterTemplateProperty =
			BindableProperty.Create(nameof(FooterTemplate), typeof(DataTemplate), typeof(VirtualListView), default);


		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static readonly BindableProperty ItemTemplateProperty =
			BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public DataTemplate SectionHeaderTemplate
		{
			get => (DataTemplate)GetValue(SectionHeaderTemplateProperty);
			set => SetValue(SectionHeaderTemplateProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

		public DataTemplate SectionFooterTemplate
		{
			get => (DataTemplate)GetValue(SectionFooterTemplateProperty);
			set => SetValue(SectionFooterTemplateProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateProperty =
			BindableProperty.Create(nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(VirtualListView), default);


		public AdapterItemDataTemplateSelector ItemTemplateSelector
		{
			get => (AdapterItemDataTemplateSelector)GetValue(ItemTemplateSelectorProperty);
			set => SetValue(ItemTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty ItemTemplateSelectorProperty =
			BindableProperty.Create(nameof(ItemTemplateSelector), typeof(AdapterItemDataTemplateSelector), typeof(VirtualListView), default);

		public AdapterSectionDataTemplateSelector SectionHeaderTemplateSelector
		{
			get => (AdapterSectionDataTemplateSelector)GetValue(SectionHeaderTemplateSelectorProperty);
			set => SetValue(SectionHeaderTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionHeaderTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(AdapterSectionDataTemplateSelector), typeof(VirtualListView), default);

		public AdapterSectionDataTemplateSelector SectionFooterTemplateSelector
		{
			get => (AdapterSectionDataTemplateSelector)GetValue(SectionFooterTemplateSelectorProperty);
			set => SetValue(SectionFooterTemplateSelectorProperty, value);
		}

		public static readonly BindableProperty SectionFooterTemplateSelectorProperty =
			BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(AdapterSectionDataTemplateSelector), typeof(VirtualListView), default);


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

		// public event EventHandler<ScrolledEventArgs> Scrolled;

		// internal void RaiseScrolled(ScrolledEventArgs args)
		// 	=> Scrolled?.Invoke(this, args);
	}
}
