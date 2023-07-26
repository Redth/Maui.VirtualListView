namespace Microsoft.Maui.Adapters;

public abstract class CachingVirtualListViewAdapter<TSection, TItem> : VirtualListViewAdapterBase<TSection, TItem>
	where TSection : class
	where TItem : class
{
	int? cachedNumberOfSections = null;
	LRUCache<int, int> cachedNumberOfItemsInSection;
	LRUCache<int, TSection> cachedSections;

	public CachingVirtualListViewAdapter(VirtualListViewAdapterBase<TSection, TItem> sourceAdapter, int sectionCacheSize = 1000)
		: this((IVirtualListViewAdapter)sourceAdapter, sectionCacheSize)
	{
	}

	public CachingVirtualListViewAdapter(IVirtualListViewAdapter sourceAdapter, int sectionCacheSize = 1000)
	{
		SourceAdapter = sourceAdapter;

		cachedSections = new LRUCache<int, TSection>(sectionCacheSize);
		cachedNumberOfItemsInSection = new LRUCache<int, int>(sectionCacheSize);
	}

	public readonly IVirtualListViewAdapter SourceAdapter;

	public sealed override int GetNumberOfSections()
		=> cachedNumberOfSections ??= SourceAdapter.GetNumberOfSections();


	public sealed override void InvalidateData()
	{
		SourceAdapter.InvalidateData();
		base.InvalidateData();
	}

	public sealed override TItem GetItem(int sectionIndex, int itemIndex)
	{
		return SourceAdapter.GetItem(sectionIndex, itemIndex) as TItem;
	}

	public sealed override int GetNumberOfItemsInSection(int sectionIndex)
	{
		if (cachedNumberOfItemsInSection.TryGet(sectionIndex, out var itemCount))
			return itemCount;

		itemCount = SourceAdapter.GetNumberOfItemsInSection(sectionIndex);
		cachedNumberOfItemsInSection.AddReplace(sectionIndex, itemCount);
		return itemCount;
	}

	public sealed override TSection GetSection(int sectionIndex)
	{
		if (cachedSections.TryGet(sectionIndex, out var section))
			return section;

		section = SourceAdapter.GetSection(sectionIndex) as TSection;

		cachedSections.AddReplace(sectionIndex, section);
		return section;
	}
}
