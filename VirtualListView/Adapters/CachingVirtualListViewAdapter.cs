namespace Microsoft.Maui.Adapters;

public abstract class CachingVirtualListViewAdapter<TSection, TItem> : VirtualListViewAdapterBase<TSection, TItem>, IDisposable
	where TSection : class
	where TItem : class
{
	int? cachedNumberOfSections = null;
	LRUCache<int, int> cachedNumberOfItemsInSection;
	LRUCache<int, TSection> cachedSections;
	bool invalidating;
	bool disposedValue;

	public CachingVirtualListViewAdapter(VirtualListViewAdapterBase<TSection, TItem> sourceAdapter, int sectionCacheSize = 1000)
		: this((IVirtualListViewAdapter)sourceAdapter, sectionCacheSize)
	{
	}

	public CachingVirtualListViewAdapter(IVirtualListViewAdapter sourceAdapter, int sectionCacheSize = 1000)
	{
		ArgumentNullException.ThrowIfNull(sourceAdapter);
		if (sectionCacheSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(sectionCacheSize));

		SourceAdapter = sourceAdapter;
		cachedSections = new LRUCache<int, TSection>(sectionCacheSize);
		cachedNumberOfItemsInSection = new LRUCache<int, int>(sectionCacheSize);
		SourceAdapter.OnDataInvalidated += SourceDataInvalidated;
	}

	public readonly IVirtualListViewAdapter SourceAdapter;

	public sealed override int GetNumberOfSections()
		=> cachedNumberOfSections ??= SourceAdapter.GetNumberOfSections();


	public sealed override void InvalidateData()
	{
		ObjectDisposedException.ThrowIf(disposedValue, this);
		if (invalidating)
			return;

		try
		{
			invalidating = true;
			ClearCaches();
			SourceAdapter.InvalidateData();
			// A source event subscriber may have read through this wrapper while forwarding.
			ClearCaches();
			base.InvalidateData();
		}
		finally
		{
			invalidating = false;
		}
	}

	void SourceDataInvalidated(object sender, EventArgs e)
	{
		if (invalidating)
			return;

		try
		{
			invalidating = true;
			ClearCaches();
			base.InvalidateData();
		}
		finally
		{
			invalidating = false;
		}
	}

	void ClearCaches()
	{
		cachedNumberOfSections = null;
		cachedNumberOfItemsInSection.Clear();
		cachedSections.Clear();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				SourceAdapter.OnDataInvalidated -= SourceDataInvalidated;
				ClearCaches();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
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
