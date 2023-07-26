namespace Microsoft.Maui.Adapters;

public class VirtualListViewAdapter<TItem> : VirtualListViewAdapterBase<object, TItem>
{
	public VirtualListViewAdapter(IList<TItem> items)
		: base()
	{
		this.items = items;
	}

	readonly IList<TItem> items;

	public override TItem GetItem(int sectionIndex, int itemIndex)
		=> items[itemIndex];

	public override int GetNumberOfItemsInSection(int sectionIndex)
		=> items.Count;
}
