#nullable enable
using System.Windows.Input;
using Microsoft.Maui.Adapters;

namespace Microsoft.Maui.Controls;

public partial class VirtualListView : View, IVirtualListView, IVirtualListViewSelector
{
	static readonly DataTemplate defaultItemTemplate = new(() =>
	{
		var label = new Label();
		label.SetBinding(Label.TextProperty, ".");
		return label;
	});

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

	/// <summary>The snapshot currently accepted by this control; null for legacy or disconnected sources.</summary>
	public VirtualListViewSnapshot? AcceptedSnapshot
	{
		get
		{
#if ANDROID || IOS || MACCATALYST || WINDOWS
			return (Handler as VirtualListViewHandler)?.AcceptedSnapshot;
#else
			return null;
#endif
		}
	}


	public IView? GlobalHeader
	{
		get => (IView)GetValue(GlobalHeaderProperty);
		set => SetValue(GlobalHeaderProperty, value);
	}

	public static readonly BindableProperty GlobalHeaderProperty =
		BindableProperty.Create(nameof(GlobalHeader), typeof(IView), typeof(VirtualListView), default,
			propertyChanged: (view, oldValue, _) => SuppliedViewChanged(view, oldValue, PositionKind.Header));

	public bool IsHeaderVisible
	{
		get => (bool)GetValue(IsHeaderVisibleProperty);
		set => SetValue(IsHeaderVisibleProperty, value);
	}

	public static readonly BindableProperty IsHeaderVisibleProperty =
		BindableProperty.Create(nameof(IsHeaderVisible), typeof(bool), typeof(VirtualListView), true);

	/// <summary>Allow a visible global header when there are no data items.</summary>
	public bool ShowHeaderWhenEmpty
	{
		get => (bool)GetValue(ShowHeaderWhenEmptyProperty);
		set => SetValue(ShowHeaderWhenEmptyProperty, value);
	}

	public static readonly BindableProperty ShowHeaderWhenEmptyProperty =
		BindableProperty.Create(nameof(ShowHeaderWhenEmpty), typeof(bool), typeof(VirtualListView), false);

	public IView? GlobalFooter
	{
		get => (IView)GetValue(GlobalFooterProperty);
		set => SetValue(GlobalFooterProperty, value);
	}

	public static readonly BindableProperty GlobalFooterProperty =
		BindableProperty.Create(nameof(GlobalFooter), typeof(IView), typeof(VirtualListView), default,
			propertyChanged: (view, oldValue, _) => SuppliedViewChanged(view, oldValue, PositionKind.Footer));

	static void SuppliedViewChanged(BindableObject bindable, object oldValue, PositionKind kind)
	{
		if (bindable is VirtualListView owner && oldValue is IView view)
			RetainedView.ReleaseSupplied(owner, view, kind);
	}


	public bool IsFooterVisible
	{
		get => (bool)GetValue(IsFooterVisibleProperty);
		set => SetValue(IsFooterVisibleProperty, value);
	}

	public static readonly BindableProperty IsFooterVisibleProperty =
		BindableProperty.Create(nameof(IsFooterVisible), typeof(bool), typeof(VirtualListView), true);

	/// <summary>Allow a visible global footer when there are no data items.</summary>
	public bool ShowFooterWhenEmpty
	{
		get => (bool)GetValue(ShowFooterWhenEmptyProperty);
		set => SetValue(ShowFooterWhenEmptyProperty, value);
	}

	public static readonly BindableProperty ShowFooterWhenEmptyProperty =
		BindableProperty.Create(nameof(ShowFooterWhenEmpty), typeof(bool), typeof(VirtualListView), false);


	public DataTemplate? ItemTemplate
	{
		get => (DataTemplate)GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public static readonly BindableProperty ItemTemplateProperty =
		BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

	public VirtualListViewItemTemplateSelector? ItemTemplateSelector
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

	public DataTemplate? SectionHeaderTemplate
	{
		get => (DataTemplate)GetValue(SectionHeaderTemplateProperty);
		set => SetValue(SectionHeaderTemplateProperty, value);
	}

	public static readonly BindableProperty SectionHeaderTemplateProperty =
		BindableProperty.Create(nameof(SectionHeaderTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

	public VirtualListViewSectionTemplateSelector? SectionHeaderTemplateSelector
	{
		get => (VirtualListViewSectionTemplateSelector)GetValue(SectionHeaderTemplateSelectorProperty);
		set => SetValue(SectionHeaderTemplateSelectorProperty, value);
	}

	public static readonly BindableProperty SectionHeaderTemplateSelectorProperty =
		BindableProperty.Create(nameof(SectionHeaderTemplateSelector), typeof(VirtualListViewSectionTemplateSelector), typeof(VirtualListView), default);



	public DataTemplate? SectionFooterTemplate
	{
		get => (DataTemplate)GetValue(SectionFooterTemplateProperty);
		set => SetValue(SectionFooterTemplateProperty, value);
	}

	public static readonly BindableProperty SectionFooterTemplateProperty =
		BindableProperty.Create(nameof(SectionFooterTemplate), typeof(DataTemplate), typeof(VirtualListView), default);

	public VirtualListViewSectionTemplateSelector? SectionFooterTemplateSelector
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

	/// <summary>Starts native refresh feedback and invokes the existing refresh command and event.</summary>
	/// <remarks>
	/// Disabled, disconnected, and already-refreshing controls ignore the request without replaying it.
	/// Complete through the command's callback or <see cref="RefreshEventArgs.Complete"/>.
	/// On Apple platforms, this reveals the indicator at the top without restoring the previous scroll position.
	/// </remarks>
	public void RequestRefresh()
		=> Handler?.Invoke(nameof(RequestRefresh), null);

	void IVirtualListView.Refresh(Action completionCallback)
	{
		var requested = false;
		if (RefreshCommand != null && RefreshCommand.CanExecute(null))
		{
			requested = true;
			RefreshCommand.Execute(completionCallback);
		}

		if (OnRefresh is not null)
		{
			requested = true;
			OnRefresh.Invoke(this, new RefreshEventArgs(completionCallback));
		}
		if (!requested)
			completionCallback();
	}

	public ICommand? RefreshCommand
	{
		get => (ICommand)GetValue(RefreshCommandProperty);
		set => SetValue(RefreshCommandProperty, value);
	}

	public static readonly BindableProperty RefreshCommandProperty =
		BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(VirtualListView), default);

	public Color? RefreshAccentColor
	{
		get => (Color?)GetValue(RefreshAccentColorProperty);
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


	public IView? EmptyView
	{
		get => (IView)GetValue(EmptyViewProperty);
		set => SetValue(EmptyViewProperty, value);
	}

	public static readonly BindableProperty EmptyViewProperty =
		BindableProperty.Create(nameof(EmptyView), typeof(IView), typeof(VirtualListView), null,
			propertyChanged: (bobj, oldValue, newValue) =>
			{
				if (bobj is VirtualListView virtualListView)
				{
					if (oldValue is IView oldView)
					{
						RetainedView.ReleaseSupplied(virtualListView, oldView, PositionKind.EmptyView);
						virtualListView.RemoveLogicalView(oldView);
					}

					if (newValue is IView newView)
						virtualListView.AddLogicalView(newView);
				}
			});

	IView? IVirtualListView.EmptyView => EmptyView;


	public IVirtualListViewSelector ViewSelector => this;

	public IView? Header => GlobalHeader;
	public IView? Footer => GlobalFooter;

	protected override void OnPropertyChanged(string? propertyName = null)
	{
		base.OnPropertyChanged(propertyName);

		switch (propertyName)
		{
			case nameof(GlobalHeader):
				OnPropertyChanged(nameof(IVirtualListView.Header));
				break;
			case nameof(GlobalFooter):
				OnPropertyChanged(nameof(IVirtualListView.Footer));
				break;
			case nameof(ItemTemplate):
			case nameof(ItemTemplateSelector):
			case nameof(SectionHeaderTemplate):
			case nameof(SectionHeaderTemplateSelector):
			case nameof(SectionFooterTemplate):
			case nameof(SectionFooterTemplateSelector):
				OnPropertyChanged(nameof(IVirtualListView.ViewSelector));
				break;
		}
	}

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

	public ICommand? ScrolledCommand
	{
		get => (ICommand)GetValue(ScrolledCommandProperty);
		set => SetValue(ScrolledCommandProperty, value);
	}

	public static readonly BindableProperty RemainingItemsThresholdProperty =
		BindableProperty.Create(nameof(RemainingItemsThreshold), typeof(int), typeof(VirtualListView), -1,
			validateValue: (_, value) =>
			{
				ArgumentOutOfRangeException.ThrowIfLessThan((int)value, -1, nameof(RemainingItemsThreshold));
				return true;
			});

	public int RemainingItemsThreshold
	{
		get => (int)GetValue(RemainingItemsThresholdProperty);
		set => SetValue(RemainingItemsThresholdProperty, value);
	}

	public static readonly BindableProperty RemainingItemsThresholdReachedCommandProperty =
		BindableProperty.Create(nameof(RemainingItemsThresholdReachedCommand), typeof(ICommand), typeof(VirtualListView), default);

	public ICommand? RemainingItemsThresholdReachedCommand
	{
		get => (ICommand)GetValue(RemainingItemsThresholdReachedCommandProperty);
		set => SetValue(RemainingItemsThresholdReachedCommandProperty, value);
	}

	public event EventHandler? RemainingItemsThresholdReached;

	void IVirtualListView.SendRemainingItemsThresholdReached()
	{
		if (RemainingItemsThresholdReachedCommand?.CanExecute(null) == true)
			RemainingItemsThresholdReachedCommand.Execute(null);
		RemainingItemsThresholdReached?.Invoke(this, EventArgs.Empty);
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
	public IList<ItemPosition>? SelectedItems
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
			if (current is null || !current.Contains(itemPosition))
			{
				SelectedItems = (current ?? []).Append(itemPosition).ToArray();
			}
		}
	}

	/// <summary>Adds unique positions in input order with one selection update in multiple-selection mode.</summary>
	public void SelectMany(IEnumerable<ItemPosition> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		if (SelectionMode != Maui.SelectionMode.Multiple)
			return;

		var current = SelectedItems?.ToList() ?? [];
		var selected = current.ToHashSet();
		var changed = false;
		foreach (var item in items)
		{
			if (selected.Add(item))
			{
				current.Add(item);
				changed = true;
			}
		}

		if (changed)
			SelectedItems = current.ToArray();
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

	DataTemplate? ItemTemplateOrDefault => ItemTemplate ?? (ItemTemplateSelector is null ? defaultItemTemplate : null);

	public IView? CreateView(PositionInfo position, object? data)
		=> position.Kind switch
		{
			PositionKind.Item =>
				ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)?.CreateContent() as View
					?? ItemTemplateOrDefault?.CreateContent() as View,
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
			PositionKind.EmptyView =>
				EmptyView,
			_ => default
		};

	public void RecycleView(PositionInfo position, object? data, IView view)
	{
		if (position.Kind is not (PositionKind.Header or PositionKind.Footer or PositionKind.EmptyView) && view is BindableObject controlsView)
		{
			controlsView.SetValue(BindingContextProperty, data);
		}
	}

	public string GetReuseId(PositionInfo position, object? data)
		=> position.Kind switch
		{
			PositionKind.Item =>
				"ITEM_" +
					(ItemTemplateSelector?.SelectTemplate(data, position.SectionIndex, position.ItemIndex)
						?? ItemTemplateOrDefault)?.GetDataTemplateId() ?? "0",
			PositionKind.SectionHeader =>
				"SECTION_HEADER_" +
					(SectionHeaderTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionHeaderTemplate)?.GetDataTemplateId() ?? "0",
			PositionKind.SectionFooter =>
				"SECTION_FOOTER_" +
					(SectionFooterTemplateSelector?.SelectTemplate(data, position.SectionIndex)
						?? SectionFooterTemplate)?.GetDataTemplateId() ?? "0",
			PositionKind.Header =>
				"GLOBAL_HEADER_" + Header.GetStableIdentity(),
			PositionKind.Footer =>
				"GLOBAL_FOOTER_" + Footer.GetStableIdentity(),
			PositionKind.EmptyView =>
				"EMPTY_VIEW_" + EmptyView.GetStableIdentity(),
			_ => "UNKNOWN"
		};

	public void ViewDetached(PositionInfo position, IView view)
	{ }

	public void ViewAttached(PositionInfo position, IView view)
	{ }

	void RaiseSelectedItemsChanged(ItemPosition[] previousSelection, ItemPosition[] newSelection)
		=> this.OnSelectedItemsChanged?.Invoke(this, new SelectedItemsChangedEventArgs(previousSelection, newSelection));

	public IReadOnlyList<IPositionInfo> FindVisiblePositions()
	{
#if ANDROID || IOS || MACCATALYST || WINDOWS
		if (Handler is IVirtualListViewHandler handler)
			return handler.FindVisiblePositions();
#endif
		return [];
	}
}
