using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Microsoft.Maui.Adapters;

public class ObservableCollectionAdapter<TItem> : VirtualListViewAdapterBase<object, TItem>, ISnapshotVirtualListViewAdapter, IDisposable
	where TItem : class
{
	public ObservableCollectionAdapter(ObservableCollection<TItem> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		this.Items = items;
		snapshots = new SnapshotAdapter(CreateSnapshot(0, items.Select(NewOccurrence)));
		snapshots.OnDataInvalidated += ForwardDataInvalidated;
		this.Items.CollectionChanged += CollectionChanged;
	}

	protected readonly ObservableCollection<TItem> Items;

	bool disposedValue;
	readonly SnapshotAdapter snapshots;
	void ForwardDataInvalidated(object sender, EventArgs args) => RaiseDataInvalidated(args);

	static SnapshotItem NewOccurrence(TItem item) => new(Guid.NewGuid().ToString("N"), 0, item);
	static VirtualListViewSnapshot CreateSnapshot(long revision, IEnumerable<SnapshotItem> rows)
	{
		var copy = rows.ToArray();
		return new(revision, copy.Length == 0 ? Array.Empty<SnapshotSection>()
			: new[] { new SnapshotSection("items", 0, null, copy) });
	}

	public virtual VirtualListViewSnapshot CaptureSnapshot() => snapshots.CaptureSnapshot();

	void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		var snapshot = CaptureSnapshot();
		var rows = snapshot.Sections.Count == 0 ? new List<SnapshotItem>() : snapshot.Sections[0].Items.ToList();
		var reset = e.Action == NotifyCollectionChangedAction.Reset;
		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				ValidateRange(e.NewStartingIndex, 0, rows.Count);
				rows.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TItem>().Select(NewOccurrence));
				break;
			case NotifyCollectionChangedAction.Remove:
				ValidateRange(e.OldStartingIndex, e.OldItems.Count, rows.Count);
				rows.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
				break;
			case NotifyCollectionChangedAction.Replace:
				ValidateRange(e.OldStartingIndex, e.OldItems.Count, rows.Count);
				if (e.NewStartingIndex != e.OldStartingIndex)
					throw new InvalidOperationException("A replacement must use the same old and new index.");
				rows.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
				rows.InsertRange(e.NewStartingIndex, e.NewItems.Cast<TItem>().Select(NewOccurrence));
				break;
			case NotifyCollectionChangedAction.Move:
				ValidateRange(e.OldStartingIndex, e.OldItems.Count, rows.Count);
				var moved = rows.GetRange(e.OldStartingIndex, e.OldItems.Count);
				rows.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
				ValidateRange(e.NewStartingIndex, 0, rows.Count);
				rows.InsertRange(e.NewStartingIndex, moved);
				break;
			case NotifyCollectionChangedAction.Reset:
				rows = Items.Select(NewOccurrence).ToList();
				break;
			default:
				throw new InvalidOperationException($"Unsupported collection action: {e.Action}.");
		}
		if (rows.Count != Items.Count)
			throw new InvalidOperationException("Collection notification count does not match the source.");
		for (var index = 0; index < rows.Count; index++)
			if (!ReferenceEquals(rows[index].Data, Items[index]))
				throw new InvalidOperationException("Collection notification payload/order does not match the source.");
		snapshots.Submit(CreateSnapshot(checked(snapshot.Revision + 1), rows), reset);
	}

	static void ValidateRange(int index, int count, int total)
	{
		if (index < 0 || count < 0 || index > total || count > total - index)
			throw new InvalidOperationException("Collection notification index is outside the accepted source.");
	}

	public override void InvalidateData()
	{
		snapshots.Submit(CreateSnapshot(checked(CaptureSnapshot().Revision + 1), Items.Select(NewOccurrence)), true);
	}

	public override TItem GetItem(int sectionIndex, int itemIndex)
		=> Items[itemIndex];

	public override int GetNumberOfItemsInSection(int sectionIndex)
		=> Items.Count;

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				Items.CollectionChanged -= CollectionChanged;
				snapshots.OnDataInvalidated -= ForwardDataInvalidated;
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
