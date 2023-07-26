namespace Microsoft.Maui.Adapters;

public abstract class VirtualListViewAdapterBase<TSection, TItem> : IVirtualListViewAdapter
{
	public virtual int GetNumberOfSections() => 1;

	public event EventHandler OnDataInvalidated;

	public virtual void InvalidateData()
	{
		OnDataInvalidated?.Invoke(this, EventArgs.Empty);
	}

	public abstract TItem GetItem(int sectionIndex, int itemIndex);

	public abstract int GetNumberOfItemsInSection(int sectionIndex);

	public virtual TSection GetSection(int sectionIndex)
		=> default;

	object IVirtualListViewAdapter.GetItem(int sectionIndex, int itemIndex)
		=> GetItem(sectionIndex, itemIndex);

	object IVirtualListViewAdapter.GetSection(int sectionIndex)
		=> GetSection(sectionIndex);

	void IVirtualListViewAdapter.InvalidateData()
		=> InvalidateData();
}
