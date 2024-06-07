using System.Windows.Input;
using Microsoft.Maui.Adapters;

namespace Microsoft.Maui.Controls;

public partial class VirtualListView : View, IVirtualListView, IVirtualListViewSelector
{
	public static readonly BindableProperty PositionInfoProperty = BindableProperty.CreateAttached(
		nameof(PositionInfo),
		typeof(PositionInfo),
		typeof(View),
		default);

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

	public bool IsHeaderVisible
	{
		get => (bool)GetValue(IsHeaderVisibleProperty);
		set => SetValue(IsHeaderVisibleProperty, value);
	}

	public static readonly BindableProperty IsHeaderVisibleProperty =
		BindableProperty.Create(nameof(IsHeaderVisible), typeof(bool), typeof(VirtualListView), true);

	public IView GlobalFooter
	{
		get => (IView)GetValue(GlobalFooterProperty);
		set => SetValue(GlobalFooterProperty, value);
	}

	public static readonly BindableProperty GlobalFooterProperty =
		BindableProperty.Create(nameof(GlobalFooter), typeof(IView), typeof(VirtualListView), default);


	public bool IsFooterVisible
	{
		get => (bool)GetValue(IsFooterVisibleProperty);
		set => SetValue(IsFooterVisibleProperty, value);
	}

	public static readonly BindableProperty IsFooterVisibleProperty =
		BindableProperty.Create(nameof(IsFooterVisible), typeof(bool), typeof(VirtualListView), true);


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

	
	public ScrollBarVisibility VerticalScrollbarVisibility
	{
		get => (ScrollBarVisibility)GetValue(VerticalScrollbarVisibilityProperty);
		set => SetValue(VerticalScrollbarVisibilityProperty, value);
	}

	public static readonly BindableProperty VerticalScrollbarVisibilityProperty =
		BindableProperty.Create(nameof(VerticalScrollbarVisibility), typeof(ScrollBarVisibility), typeof(VirtualListView), ScrollBarVisibility.Default);


	public ScrollBarVisibility HorizontalScrollbarVisibility
	{
		get => (ScrollBarVisibility)GetValue(HorizontalScrollbarVisibilityProperty);
		set => SetValue(HorizontalScrollbarVisibilityProperty, value);
	}

	public static readonly BindableProperty HorizontalScrollbarVisibilityProperty =
		BindableProperty.Create(nameof(HorizontalScrollbarVisibility), typeof(ScrollBarVisibility), typeof(VirtualListView), ScrollBarVisibility.Default);

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


	public Maui.SelectionMode SelectionMode
	{
		get => (Maui.SelectionMode)GetValue(SelectionModeProperty);
		set => SetValue(SelectionModeProperty, value);
	}

	public static readonly BindableProperty SelectionModeProperty =
		BindableProperty.Create(nameof(SelectionMode), typeof(Maui.SelectionMode), typeof(VirtualListView), Maui.SelectionMode.None);

	public event EventHandler<SelectedItemsChangedEventArgs> OnSelectedItemsChanged;

	public event EventHandler<RefreshEventArgs> OnRefresh;

	void IVirtualListView.Refresh(Action completionCallback)
	{
		if (RefreshCommand != null && RefreshCommand.CanExecute(null))
		{
			RefreshCommand.Execute(completionCallback);
		}

		OnRefresh?.Invoke(this, new RefreshEventArgs(completionCallback));
	}

	public ICommand RefreshCommand
	{
		get => (ICommand)GetValue(RefreshCommandProperty);
		set => SetValue(RefreshCommandProperty, value);
	}

	public static readonly BindableProperty RefreshCommandProperty =
		BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(VirtualListView), default);

	public Color RefreshAccentColor
	{
		get => (Color)GetValue(RefreshAccentColorProperty);
		set => SetValue(RefreshAccentColorProperty, value);
	}

	public static readonly BindableProperty RefreshAccentColorProperty =
		BindableProperty.Create(nameof(RefreshAccentColor), typeof(Color), typeof(VirtualListView), null);

	public bool IsRefreshEnabled
	{
		get => (bool)GetValue(IsRefreshEnabledProperty);
		set => SetValue(IsRefreshEnabledProperty, value);
	}

	public static readonly BindableProperty IsRefreshEnabledProperty =
		BindableProperty.Create(nameof(IsRefreshEnabled), typeof(bool), typeof(VirtualListView), false);

	public ListOrientation Orientation
	{
		get => (ListOrientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	public static readonly BindableProperty OrientationProperty =
		BindableProperty.Create(nameof(Orientation), typeof(ListOrientation), typeof(VirtualListView), ListOrientation.Vertical);


	public View EmptyView
	{
		get => (View)GetValue(EmptyViewProperty);
		set => SetValue(EmptyViewProperty, value);
	}

	public static readonly BindableProperty EmptyViewProperty =
		BindableProperty.Create(nameof(EmptyView), typeof(View), typeof(VirtualListView), null,
			propertyChanged: (bobj, oldValue, newValue) =>
			{
				if (bobj is VirtualListView virtualListView)
				{
					if (oldValue is IView oldView)
						virtualListView.RemoveLogicalChild(oldView);

					if (newValue is IView newView)
						virtualListView.AddLogicalChild(newView);
				}
			});

	IView IVirtualListView.EmptyView => EmptyView;


	public IVirtualListViewSelector ViewSelector => this;

	public IView Header => GlobalHeader;
	public IView Footer => GlobalFooter;

	public event EventHandler<ScrolledEventArgs> OnScrolled;

	public void Scrolled(double x, double y)
	{
		var args = new ScrolledEventArgs(x, y);

		if (ScrolledCommand != null && ScrolledCommand.CanExecute(args))
			ScrolledCommand.Execute(args);

		OnScrolled?.Invoke(this, args);
	}

	public static readonly BindableProperty ScrolledCommandProperty =
		BindableProperty.Create(nameof(ScrolledCommand), typeof(ICommand), typeof(VirtualListView), default);

	public ICommand ScrolledCommand
	{
		get => (ICommand)GetValue(ScrolledCommandProperty);
		set => SetValue(ScrolledCommandProperty, value);
	}

	public static readonly BindableProperty SelectedItemsProperty =
		BindableProperty.Create(nameof(SelectedItems), typeof(IList<ItemPosition>), typeof(VirtualListView), Array.Empty<ItemPosition>(),
			propertyChanged: (bindableObj, oldValue, newValue) =>
			{
				if (bindableObj is VirtualListView vlv
					&& oldValue is IList<ItemPosition> oldSelection
					&& newValue is IList<ItemPosition> newSelection)
				{
					vlv.RaiseSelectedItemsChanged(oldSelection.ToArray(), newSelection.ToArray());
				}
			});
	public IList<ItemPosition> SelectedItems
	{
		get => (IList<ItemPosition>)GetValue(SelectedItemsProperty);
		set => SetValue(SelectedItemsProperty, value ?? Array.Empty<ItemPosition>());
	}

	public static readonly BindableProperty SelectedItemProperty =
		BindableProperty.Create(nameof(SelectedItem), typeof(ItemPosition?), typeof(VirtualListView), default,
			propertyChanged: (bindableObj, oldValue, newValue) =>
			{
				if (bindableObj is VirtualListView vlv)
				{
					if (newValue is null || newValue is not ItemPosition)
						vlv.SelectedItems = null;
					else if (newValue is ItemPosition p)
						vlv.SelectedItems = new[] { p };
				}
			});

	public ItemPosition? SelectedItem
	{
		get => (ItemPosition?)GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}


	public void DeselectItem(ItemPosition itemPosition)
	{
		if (SelectionMode == Maui.SelectionMode.Single)
		{
			if (SelectedItem.HasValue && SelectedItem.Value.Equals(itemPosition))
			{
				SelectedItem = null;
			}
		}
		else if (SelectionMode == Maui.SelectionMode.Multiple)
		{
			var current = SelectedItems.ToList();
			if (current.Contains(itemPosition))
			{
				current.Remove(itemPosition);
				SelectedItems = current.ToArray();
			}
		}
	}

	public void SelectItem(ItemPosition itemPosition)
	{
		if (SelectionMode == Maui.SelectionMode.Single)
		{
			if (!SelectedItem.HasValue || !SelectedItem.Value.Equals(itemPosition))
			{
				SelectedItem = itemPosition;
			}
		}
		else if (SelectionMode == Maui.SelectionMode.Multiple)
		{
			var current = SelectedItems;
			if (!current.Contains(itemPosition))
			{
				SelectedItems = current.Append(itemPosition).ToArray();
			}
		}
	}

	public void ClearSelectedItems()
	{
		if (SelectionMode == Maui.SelectionMode.Multiple)
			SelectedItems = null;
		else
			SelectedItem = null;
	}

	public void ScrollToItem(ItemPosition itemPosition, bool animated)
		=> Handler?.Invoke(nameof(ScrollToItem), new object[] { itemPosition, animated });

	public bool SectionHasHeader(int sectionIndex)
		=> SectionHeaderTemplateSelector != null || SectionHeaderTemplate != null;

	public bool SectionHasFooter(int sectionIndex)
		=> SectionFooterTemplateSelector != null || SectionFooterTemplate != null;

	public IView CreateView(PositionInfo position, object data)
		=> position.Kind switch
		{
			PositionKind.Item =>
				ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)?.CreateContent() as View
					?? ItemTemplate?.CreateContent() as View,
			PositionKind.SectionHeader =>
				SectionHeaderTemplateSelector?.SelectTemplate(data, position.SectionIndex)?.CreateContent() as View
					?? SectionHeaderTemplate?.CreateContent() as View,
			PositionKind.SectionFooter =>
				SectionFooterTemplateSelector?.SelectTemplate(data, position.SectionIndex)?.CreateContent() as View
					?? SectionFooterTemplate?.CreateContent() as View,
			PositionKind.Header =>
				GlobalHeader,
			PositionKind.Footer =>
				GlobalFooter,
			_ => default
		};

	public void RecycleView(PositionInfo position, object data, IView view)
	{
		if (view is View controlsView)
		{
			// Use the listview's binding context if
			// there's no data
			// this may happen for global header/footer for instance
			controlsView.SetValue(
				View.BindingContextProperty,
				data ?? this.BindingContext);
		}
	}

	public string GetReuseId(PositionInfo position, object data)
		=> position.Kind switch
		{
			PositionKind.Item =>
				"ITEM_" +
					(ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)
						?? ItemTemplate)?.GetDataTemplateId() ?? "0",
			PositionKind.SectionHeader =>
				"SECTION_HEADER_" +
					(SectionHeaderTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionHeaderTemplate)?.GetDataTemplateId() ?? "0",
			PositionKind.SectionFooter =>
				"SECTION_FOOTER_" +
					(SectionFooterTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionFooterTemplate)?.GetDataTemplateId() ?? "0",
			PositionKind.Header =>
				"GLOBAL_HEADER_" + (Header?.GetType()?.FullName ?? "NIL"),
			PositionKind.Footer =>
				"GLOBAL_FOOTER_" + (Footer?.GetType()?.FullName ?? "NIL"),
			_ => "UNKNOWN"
		};

	public void ViewDetached(PositionInfo position, IView view)
		=> this.RemoveLogicalChild(view);

	public void ViewAttached(PositionInfo position, IView view)
		=> this.AddLogicalChild(view);

	void RaiseSelectedItemsChanged(ItemPosition[] previousSelection, ItemPosition[] newSelection)
		=> this.OnSelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(previousSelection, newSelection));

}
