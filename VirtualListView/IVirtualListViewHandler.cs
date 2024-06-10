namespace Microsoft.Maui;

public interface IVirtualListViewHandler
{
	IReadOnlyList<IPositionInfo> FindVisiblePositions();

	Adapters.IVirtualListViewAdapter Adapter { get; }

	IVirtualListViewSelector ViewSelector { get; }

	IVirtualListView VirtualView {get; }
	
	void InvalidateData();
}
