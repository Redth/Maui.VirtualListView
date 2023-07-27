using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public interface IVirtualListView : IView
{
	IVirtualListViewAdapter Adapter { get; }

	IVirtualListViewSelector ViewSelector { get; }

	IView Header { get; }

	IView Footer { get; }

	event EventHandler<SelectedItemsChangedEventArgs> SelectedItemsChanged;

	void RaiseSelectedItemsChanged(ItemPosition[] previousSelection, ItemPosition[] newSelection);

	event EventHandler DataInvalidated;

	Color RefreshAccentColor { get; }

	void Refresh();

	bool IsRefreshEnabled { get; }
	
	void Scrolled(ScrolledEventArgs args);

	SelectionMode SelectionMode { get; }

	ListOrientation Orientation { get; }

	IView EmptyView { get; }

	IList<ItemPosition> SelectedItems { get; set; }

	//bool IsItemSelected(int sectionIndex, int itemIndex);

	void SelectItem(ItemPosition path);

	void DeselectItem(ItemPosition path);

	void ClearSelectedItems();
}

public enum ListOrientation
{
	Vertical,
	Horizontal
}
