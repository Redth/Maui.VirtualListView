using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler
{
	public static new IPropertyMapper<IVirtualListView, VirtualListViewHandler> ViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(Handlers.ViewHandler.ViewMapper)
	{
		[nameof(IVirtualListView.Adapter)] = MapAdapter,
		[nameof(IVirtualListView.Header)] = MapHeader,
		[nameof(IVirtualListView.IsHeaderVisible)] = MapIsHeaderVisible,
		[nameof(IVirtualListView.Footer)] = MapFooter,
		[nameof(IVirtualListView.IsFooterVisible)] = MapIsFooterVisible,
		[nameof(IVirtualListView.ViewSelector)] = MapViewSelector,
		[nameof(IVirtualListView.SelectionMode)] = MapSelectionMode,
		[nameof(IVirtualListView.Orientation)] = MapOrientation,
		[nameof(IVirtualListView.RefreshAccentColor)] = MapRefreshAccentColor,
		[nameof(IVirtualListView.IsRefreshEnabled)] = MapIsRefreshEnabled,
		[nameof(IVirtualListView.EmptyView)] = MapEmptyView,
		[nameof(IVirtualListView.SelectedItems)] = MapSelectedItems,
	};

	public static CommandMapper<IVirtualListView, VirtualListViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(IVirtualListView.ScrollToItem)] = MapScrollToItem,
		[nameof(IVirtualListView.DeleteItems)] = MapDeleteItems,
		[nameof(IVirtualListView.DeleteSection)] = MapDeleteSection,
		[nameof(IVirtualListView.InsertItems)] = MapInsertItems,
		[nameof(IVirtualListView.InsertSection)] = MapInsertSection,
		[nameof(IVirtualListView.InvalidateData)] = MapInvalidateData
	};
	
	public static void MapDeleteItems(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		if (parameter is ItemPosition[] itemPositions)
		{
			handler.PlatformDeleteItems(itemPositions);
		}
	}
	
	public static void MapDeleteSection(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		if (parameter is int sectionIndex)
		{
			handler.PlatformDeleteSection(sectionIndex);
		}
	}
	
	public static void MapInsertItems(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		if (parameter is ItemPosition[] itemPositions)
		{
			handler.PlatformInsertItems(itemPositions);
		}
	}
	
	public static void MapInsertSection(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		if (parameter is int sectionIndex)
		{
			handler.PlatformInsertSection(sectionIndex);
		}
	}

	public static void MapScrollToItem(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		if (parameter is ItemPosition itemPosition)
		{
			handler.PlatformScrollToItem(itemPosition, true);
		}
		else if (parameter is object[] parameters)
		{
			if (parameters?[0] is ItemPosition p)
			{
				var animated = true;
				if (parameters?[1] is bool a)
					animated = a;

				handler.PlatformScrollToItem(p, animated);
			}
		}
	}
	
	public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.currentAdapter = virtualListView.Adapter;
		handler.PlatformInvalidateData();
	}

	public static void MapSelectedItems(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler is null)
			return;

		var newSelections = virtualListView?.SelectedItems ?? Array.Empty<ItemPosition>();

		if (virtualListView.SelectionMode == SelectionMode.None)
			newSelections = Array.Empty<ItemPosition>();
		else if (virtualListView.SelectionMode == SelectionMode.Single && newSelections.Count > 1)
			newSelections = newSelections.Take(1).ToArray();

		// First deselect any previously selected items that aren't in the new set
		foreach (var itemPosition in handler.previousSelections)
		{
			if (!newSelections.Contains(itemPosition))
				handler.PlatformUpdateItemSelection(itemPosition, false);
		}

		// Set all the new state selected to true
		foreach (var itemPosition in newSelections)
		{
			if (!handler.previousSelections.Contains(itemPosition))
				handler.PlatformUpdateItemSelection(itemPosition, true);
		}

		// Keep track of the new state for next time it changes
		handler.previousSelections = newSelections.ToArray();
	}

	public static void MapIsHeaderVisible(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.PlatformInvalidateData();

	public static void MapIsFooterVisible(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.PlatformInvalidateData();
	
	public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.PlatformInvalidateData();

	public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.PlatformInvalidateData();

	public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.PlatformInvalidateData();

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
	}

	public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
	{
		handler?.currentAdapter?.InvalidateData();
		handler?.PlatformInvalidateData();	
	}
	
	public VirtualListViewHandler() : base(ViewMapper, CommandMapper)
	{

	}

	public PositionalViewSelector PositionalViewSelector { get; private set; }

	bool ShouldShowEmptyView
	{
		get
		{
			var sections = PositionalViewSelector?.Adapter?.GetNumberOfSections() ?? 0;

			if (sections <= 0)
				return true;

			return (PositionalViewSelector?.Adapter?.GetNumberOfItemsInSection(0) ?? 0) <= 0;
		}
	}

	IVirtualListViewAdapter currentAdapter = default;
	ItemPosition[] previousSelections = Array.Empty<ItemPosition>();
	
	public bool IsItemSelected(int sectionIndex, int itemIndex)
	{
		if (VirtualView is null)
			return false;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return false;

		return previousSelections.Contains(new ItemPosition(sectionIndex, itemIndex));
	}
}