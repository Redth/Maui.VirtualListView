using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public interface IVirtualListView : IView
{
	IVirtualListViewAdapter Adapter { get; }

	IVirtualListViewSelector ViewSelector { get; }

	IView Header { get; }

	IView Footer { get; }

	event EventHandler<SelectedItemsChangedEventArgs> SelectedItemsChanged;
	void OnSelectedItemsChanged(SelectedItemsChangedEventArgs eventArgs);

	event EventHandler DataInvalidated;

	Color RefreshAccentColor { get; }

	void Refresh();
	
	void Scrolled(ScrolledEventArgs args);

	SelectionMode SelectionMode { get; }

	IReadOnlyList<ItemPosition> SelectedItems { get; }

	ListOrientation Orientation { get; }

	IView EmptyView { get; }

	IView RefreshView { get; }

	bool IsItemSelected(int sectionIndex, int itemIndex);

	void SelectItems(params ItemPosition[] paths);

	void DeselectItems(params ItemPosition[] paths);

	void ClearSelection();

	//void InvalidateData();
}

public enum ListOrientation
{
	Vertical,
	Horizontal
}
