namespace Microsoft.Maui.Adapters;

public abstract class VirtualListViewAdapterBase<TSection, TItem> : IVirtualListViewAdapter
{
	public virtual int Sections { get; } = 1;

	public event EventHandler OnDataInvalidated;

	public virtual void InvalidateData()
		=> OnDataInvalidated?.Invoke(this, EventArgs.Empty);

	public abstract TItem Item(int sectionIndex, int itemIndex);

	public abstract int ItemsForSection(int sectionIndex);

	public virtual TSection Section(int sectionIndex)
		=> default;

	object IVirtualListViewAdapter.Item(int sectionIndex, int itemIndex)
		=> Item(sectionIndex, itemIndex);

	object IVirtualListViewAdapter.Section(int sectionIndex)
		=> Section(sectionIndex);
}
