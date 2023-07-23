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
	};

	public static CommandMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewCommandMapper = new(VirtualListViewHandler.ViewCommandMapper)
	{
		[nameof(IVirtualListView.SetSelected)] = MapSetSelected,
		[nameof(IVirtualListView.SetDeselected)] = MapSetDeselected
	};

	public VirtualListViewHandler() : base(VirtualListViewMapper, VirtualListViewCommandMapper)
	{

	}

	public VirtualListViewHandler(PropertyMapper mapper = null, CommandMapper commandMapper = null) : base(mapper ?? VirtualListViewMapper, commandMapper ?? VirtualListViewCommandMapper)
	{

	}

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

	public void SetSelected(params ItemPosition[] paths)
	{
		if (VirtualView is null)
			return;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return;

		var prev = selectedItems.ToArray();

		IReadOnlyList<ItemPosition> current;

		lock (selectedItemsLocker)
		{
			if (VirtualView.SelectionMode == SelectionMode.Single)
			{
				current = paths.Any()
					? new List<ItemPosition> { paths.First() }
					: new List<ItemPosition>();
			}
			else if (VirtualView.SelectionMode == SelectionMode.Multiple)
			{
				foreach (var path in paths)
				{
					if (!selectedItems.Contains(path))
						selectedItems.Add(path);
				}

				current = selectedItems;
			}
			else
			{
				current = new List<ItemPosition>();
			}
		}

		// Raise event
		VirtualView.OnSelectedItemsChanged(new SelectedItemsChangedEventArgs(prev, current));
	}

	public void SetDeselected(params ItemPosition[] paths)
	{
		if (VirtualView is null)
			return;

		if (VirtualView.SelectionMode == Maui.SelectionMode.None)
			return;

		var prev = new List<ItemPosition>(selectedItems);

		IReadOnlyList<ItemPosition> current;

		lock (selectedItemsLocker)
		{
			if (VirtualView.SelectionMode == Maui.SelectionMode.Multiple)
			{
				foreach (var path in paths)
				{
					if (selectedItems.Contains(path))
						selectedItems.Remove(path);
				}

				current = selectedItems ?? new List<ItemPosition>();
			}
			else
			{
				current = new List<ItemPosition>();
			}
		}

		// Raise event
		VirtualView.OnSelectedItemsChanged(new SelectedItemsChangedEventArgs(prev, current));
	}

	public void ClearSelection()
	{
		if (VirtualView is null)
			return;

		selectedItems.Clear();
	}
}