namespace Microsoft.Maui.Adapters;

public class VirtualListViewAdapter<TItem> : VirtualListViewAdapterBase<object, TItem>
{
    public VirtualListViewAdapter(IList<TItem> items)
        : base()
    {
        this.items = items;
    }

    readonly IList<TItem> items;

    public override TItem Item(int sectionIndex, int itemIndex)
        => items[itemIndex];

    public override int ItemsForSection(int sectionIndex)
        => items.Count;
}
