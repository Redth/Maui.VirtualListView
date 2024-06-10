#nullable enable
using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : IVirtualListViewHandler
{
	#if ANDROID || IOS || MACCATALYST || WINDOWS
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
		[nameof(IVirtualListView.VerticalScrollbarVisibility)] = MapVerticalScrollbarVisibility,
		[nameof(IVirtualListView.HorizontalScrollbarVisibility)] = MapHorizontalScrollbarVisibility,
		[nameof(IVirtualListView.Columns)] = MapColumns,
	};

	public static CommandMapper<IVirtualListView, VirtualListViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(IVirtualListView.ScrollToItem)] = MapScrollToItem,
	};

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

	public VirtualListViewHandler() : base(ViewMapper, CommandMapper)
	{
		#if IOS || MACCATALYST
		PlatformController = new SectionalPlatformController(this);
		#else
		PlatformController = new PositionalPlatformController(this);
		#endif
	}

    #if IOS || MACCATALYST
	internal ISectionalPlatformController PlatformController { get; private set; }
	#else
	internal IPositionalPlatformController PlatformController { get; private set; }
	#endif

	public IVirtualListViewSelector ViewSelector => VirtualView.ViewSelector;
    
	bool ShouldShowEmptyView
	{
		get
		{
			var sections = currentAdapter?.GetNumberOfSections() ?? 0;
			
			if (sections <= 0)
				return true;
			
			for (var s = 0; s < sections; s++)
			{
				var itemsInSection = (currentAdapter?.GetNumberOfItemsInSection(0) ?? 0);
				if (itemsInSection > 0)
					return false;
			}

			return true;
		}
	}

	public static void MapAdapter(IVirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler is VirtualListViewHandler typedHandler)
		{
			typedHandler.currentAdapter.OnDataInvalidated -= typedHandler.Adapter_OnDataInvalidated;

			virtualListView.Adapter.OnDataInvalidated += typedHandler.Adapter_OnDataInvalidated;

			typedHandler.currentAdapter = virtualListView.Adapter;
		}

		handler.InvalidateData();
	}

	IVirtualListViewAdapter currentAdapter = new EmptyAdapter();

	public IVirtualListViewAdapter Adapter => currentAdapter;
	

	void Adapter_OnDataInvalidated(object sender, EventArgs e)
	{
		InvalidateData();
	}

	public bool IsItemSelected(int sectionIndex, int itemIndex)
	{
		if (VirtualView is null)
			return false;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return false;

		return previousSelections.Contains(new ItemPosition(sectionIndex, itemIndex));
	}

	ItemPosition[] previousSelections = Array.Empty<ItemPosition>();

	public static void MapSelectedItems(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		var newSelections = virtualListView.SelectedItems ?? Array.Empty<ItemPosition>();

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

	public static void MapIsHeaderVisible(IVirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.InvalidateData();
	}

	public static void MapIsFooterVisible(IVirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.InvalidateData();
	}
	
	public static void MapVerticalScrollbarVisibility(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.UpdateVerticalScrollbarVisibility(virtualListView.VerticalScrollbarVisibility);
	}
	
	public static void MapHorizontalScrollbarVisibility(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.UpdateHorizontalScrollbarVisibility(virtualListView.HorizontalScrollbarVisibility);
	}
#endif

#if !ANDROID && !IOS && !WINDOWS && !MACCATALYST
	public IVirtualListViewAdapter Adapter => throw new NotImplementedException();

	public IVirtualListViewSelector ViewSelector => throw new NotImplementedException();

	public void InvalidateData() => throw new NotImplementedException();

	public IVirtualListView VirtualView => throw new NotImplementedException();

	public IReadOnlyList<IPositionInfo> FindVisiblePositions() => throw new NotImplementedException();
#endif
}