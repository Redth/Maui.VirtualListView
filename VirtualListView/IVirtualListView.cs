using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public interface IVirtualListView : IView
{
	IVirtualListViewAdapter Adapter { get; }

	IVirtualListViewSelector ViewSelector { get; }

	IView Header { get; }

	IView Footer { get; }

	event EventHandler<ScrolledEventArgs> OnScrolled;

	void Scrolled(double x, double y);

	SelectionMode SelectionMode { get; }

	IList<ItemPosition> SelectedItems { get; set; }

	event EventHandler<SelectedItemsChangedEventArgs> OnSelectedItemsChanged;

	Color RefreshAccentColor { get; }

	void Refresh();

	bool IsRefreshEnabled { get; }
	
	ListOrientation Orientation { get; }

	IView EmptyView { get; }

	void SelectItem(ItemPosition path);

	void DeselectItem(ItemPosition path);

	void ClearSelectedItems();
}

public enum ListOrientation
{
	Vertical,
	Horizontal
}
