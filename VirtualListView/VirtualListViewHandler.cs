using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
	public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(VirtualListViewHandler.ViewMapper)
	{
		[nameof(IVirtualListView.Adapter)] = MapAdapter,
		[nameof(IVirtualListView.Header)] = MapHeader,
		[nameof(IVirtualListView.Footer)] = MapFooter,
		[nameof(IVirtualListView.ViewSelector)] = MapViewSelector,
		[nameof(IVirtualListView.SelectionMode)] = MapSelectionMode,
		[nameof(IVirtualListView.Orientation)] = MapOrientation,
		[nameof(IVirtualListView.RefreshAccentColor)] = MapRefreshAccentColor,
		[nameof(IVirtualListView.IsRefreshEnabled)] = MapIsRefreshEnabled,
		[nameof(IVirtualListView.EmptyView)] = MapEmptyView,
	};

	public static CommandMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewCommandMapper = new(VirtualListViewHandler.ViewCommandMapper)
	{
		[nameof(IVirtualListView.SelectItems)] = MapSelectItems,
		[nameof(IVirtualListView.DeselectItems)] = MapDeselectItems,
	};

	public VirtualListViewHandler() : base(VirtualListViewMapper, VirtualListViewCommandMapper)
	{

	}

	public VirtualListViewHandler(PropertyMapper mapper = null, CommandMapper commandMapper = null) : base(mapper ?? VirtualListViewMapper, commandMapper ?? VirtualListViewCommandMapper)
	{

	}

	internal PositionalViewSelector PositionalViewSelector { get; private set; }

	bool ShouldShowEmptyView => (PositionalViewSelector?.Adapter?.Sections ?? 0) <= 1
					&& (PositionalViewSelector?.Adapter?.ItemsForSection(0) ?? 0) <= 0;


	public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler.currentAdapter != null)
			handler.currentAdapter.OnDataInvalidated -= handler.Adapter_OnDataInvalidated;

		if (virtualListView?.Adapter != null)
			virtualListView.Adapter.OnDataInvalidated += handler.Adapter_OnDataInvalidated;

		handler.currentAdapter = virtualListView.Adapter;
		handler?.InvalidateData();
	}

	IVirtualListViewAdapter currentAdapter = default;

	void Adapter_OnDataInvalidated(object sender, EventArgs e)
	{
		InvalidateData();
	}

	readonly object selectedItemsLocker = new object();
	readonly List<ItemPosition> selectedItems = new List<ItemPosition>();

	public IReadOnlyList<ItemPosition> SelectedItems
	{
		get
		{
			if (VirtualView.SelectionMode == SelectionMode.None)
				return new List<ItemPosition>();

			lock (selectedItemsLocker)
				return selectedItems.AsReadOnly();
		}
	}

	public bool IsItemSelected(int sectionIndex, int itemIndex)
	{
		if (VirtualView is null)
			return false;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return false;

		lock (selectedItemsLocker)
			return selectedItems.Contains(new ItemPosition(sectionIndex, itemIndex));
	}

	public void SelectItems(params ItemPosition[] paths)
	{
		if (VirtualView is null)
			return;

		// Can't select any items in none mode
		if (VirtualView.SelectionMode == SelectionMode.None)
			return;

		// Can't select multiple items in single mode
		if (VirtualView.SelectionMode == SelectionMode.Single && selectedItems.Count > 0)
			return;

		// Keep track of previous selection state
		var prev = new List<ItemPosition>(selectedItems);

		lock (selectedItemsLocker)
		{
			var toAdd = new List<ItemPosition>();

			foreach (var path in paths)
			{
				// Check if the item is already selected
				if (selectedItems.Contains(path))
					continue;

				toAdd.Add(path);
			}

			foreach (var path in toAdd)
				selectedItems.Add(path);

			Invoke(nameof(SelectItems), toAdd.ToArray());
		}

		// Raise event
		VirtualView.OnSelectedItemsChanged(new SelectedItemsChangedEventArgs(prev, selectedItems));
	}



	public void DeselectItems(params ItemPosition[] paths)
	{
		if (VirtualView is null)
			return;

		// Nothing to deselect in none mode
		if (VirtualView.SelectionMode == SelectionMode.None)
			return;

		// Nothing to deselect in single mode if we have no items selected
		if (VirtualView.SelectionMode == SelectionMode.Single && selectedItems.Count <= 0)
			return;

		var prev = new List<ItemPosition>(selectedItems);

		lock (selectedItemsLocker)
		{
			var toRemove = new List<ItemPosition>();

			foreach (var path in paths)
			{
				// If our selection doesn't contain the requested item, we can't deselect it
				if (!selectedItems.Contains(path))
					continue;

				toRemove.Add(path);
			}

			foreach (var path in toRemove)
				selectedItems.Remove(path);

			Invoke(nameof(DeselectItems), toRemove.ToArray());
		}

		// Raise event
		VirtualView.OnSelectedItemsChanged(new SelectedItemsChangedEventArgs(prev, selectedItems));
	}

	public void ClearSelection()
	{
		if (VirtualView is null)
			return;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return;

		var prev = new List<ItemPosition>(selectedItems);

		lock (selectedItemsLocker)
		{
			var toRemove = new List<ItemPosition>(selectedItems);
			selectedItems.Clear();

			Invoke(nameof(ClearSelection), toRemove.ToArray());
		}

		// Raise event
		//VirtualView.OnSelectedItemsChanged(new SelectedItemsChangedEventArgs(prev, selectedItems));
	}
}