
using System;
using System.Collections.Generic;

namespace Microsoft.Maui
{
	public interface IVirtualListView : IView
	{
		IVirtualListViewAdapter Adapter { get; }

		IViewTemplate HeaderTemplate { get; }

		IViewTemplate FooterTemplate { get; }

		IViewTemplate SectionHeaderTemplate { get; }

		IViewTemplate SectionFooterTemplate { get; }

		IViewTemplate ItemTemplate { get; }

		event EventHandler<SelectedItemsChangedEventArgs> SelectedItemsChanged;

		IReadOnlyList<ItemPosition> SelectedItems { get; }

		bool IsItemSelected(int sectionIndex, int itemIndex);

		void SetSelected(params ItemPosition[] paths);

		void SetDeselected(params ItemPosition[] paths);
	}
}
