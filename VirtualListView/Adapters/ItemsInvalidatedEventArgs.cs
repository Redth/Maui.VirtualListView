#nullable enable
namespace Microsoft.Maui.Adapters;

/// <summary>Optional positional content refresh. Structural changes still require InvalidateData.</summary>
public interface IItemRefreshVirtualListViewAdapter : IVirtualListViewAdapter
{
	void InvalidateItems(params ItemPosition[] items);
}

/// <summary>Refreshes item occurrences without changing section/item counts, order, or chrome.</summary>
public sealed class ItemsInvalidatedEventArgs : EventArgs
{
	public ItemsInvalidatedEventArgs(IEnumerable<ItemPosition> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		var copy = items.Distinct().ToArray();
		foreach (var item in copy)
			if (item.SectionIndex < 0 || item.ItemIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(items), "Item coordinates must be nonnegative.");
		Items = Array.AsReadOnly(copy);
	}

	public IReadOnlyList<ItemPosition> Items { get; }

	internal void Validate(IVirtualListViewAdapter adapter)
	{
		if (adapter is ISnapshotVirtualListViewAdapter or VirtualListViewSnapshot)
			throw new InvalidOperationException("Snapshot sources must submit a new snapshot with increased item content revisions.");
		var sectionCount = adapter.GetNumberOfSections();
		var counts = new Dictionary<int, int>();
		foreach (var item in Items)
		{
			if (item.SectionIndex >= sectionCount)
				throw new ArgumentOutOfRangeException(nameof(Items), "The section does not exist.");
			if (!counts.TryGetValue(item.SectionIndex, out var count))
				counts.Add(item.SectionIndex, count = adapter.GetNumberOfItemsInSection(item.SectionIndex));
			if (item.ItemIndex >= count)
				throw new ArgumentOutOfRangeException(nameof(Items), "The item does not exist.");
		}
	}
}
