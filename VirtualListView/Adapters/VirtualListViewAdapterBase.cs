namespace Microsoft.Maui.Adapters;

public abstract class VirtualListViewAdapterBase<TSection, TItem> : IItemRefreshVirtualListViewAdapter
{
	// This adapter assumes we only ever have 1 section
	// however we really want to return 0 if there's no items at all
	// So, ask the derived class how many items might be in the first
	// section and if any, we return 1 section otherwise 0
	public virtual int GetNumberOfSections() =>
		GetNumberOfItemsInSection(0) > 0 ? 1 : 0;

	public event EventHandler OnDataInvalidated;

	public virtual void InvalidateData()
	{
		RaiseDataInvalidated(EventArgs.Empty);
	}

	/// <summary>Rebinds the specified item occurrences, including template selection, without a data reset.</summary>
	public virtual void InvalidateItems(params ItemPosition[] items)
	{
		var args = new ItemsInvalidatedEventArgs(items);
		args.Validate(this);
		if (args.Items.Count > 0)
			RaiseDataInvalidated(args);
	}

	protected void RaiseDataInvalidated(EventArgs args) => OnDataInvalidated?.Invoke(this, args);

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
