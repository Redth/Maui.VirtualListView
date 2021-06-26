using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Maui.Controls
{

    public partial class VirtualListView : View, IVirtualListView
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


        public IViewTemplate HeaderTemplate
        {
            get => (IViewTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        public static readonly BindableProperty HeaderTemplateProperty =
            BindableProperty.Create(nameof(HeaderTemplate), typeof(IViewTemplate), typeof(VirtualListView), default);

        public IViewTemplate FooterTemplate
        {
            get => (IViewTemplate)GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }

        public static readonly BindableProperty FooterTemplateProperty =
            BindableProperty.Create(nameof(FooterTemplate), typeof(IViewTemplate), typeof(VirtualListView), default);


        public IViewTemplate ItemTemplate
        {
            get => (IViewTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public static readonly BindableProperty ItemTemplateProperty =
            BindableProperty.Create(nameof(ItemTemplate), typeof(IViewTemplate), typeof(VirtualListView), default);

        public IViewTemplate SectionHeaderTemplate
        {
            get => (IViewTemplate)GetValue(SectionHeaderTemplateProperty);
            set => SetValue(SectionHeaderTemplateProperty, value);
        }

        public static readonly BindableProperty SectionHeaderTemplateProperty =
            BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(IViewTemplate), typeof(VirtualListView), default);

        public IViewTemplate SectionFooterTemplate
        {
            get => (IViewTemplate)GetValue(SectionFooterTemplateProperty);
            set => SetValue(SectionFooterTemplateProperty, value);
        }

        public static readonly BindableProperty SectionFooterTemplateProperty =
            BindableProperty.Create(nameof(SectionFooterTemplate), typeof(IViewTemplate), typeof(VirtualListView), default);


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

        public event EventHandler<ScrolledEventArgs> Scrolled;

        internal void RaiseScrolled(ScrolledEventArgs args)
            => Scrolled?.Invoke(this, args);
    }
}
