#nullable enable
namespace Microsoft.Maui.Adapters;

/// <summary>Optional immutable, revisioned source boundary. Existing adapters need not implement it.</summary>
public interface ISnapshotVirtualListViewAdapter : IVirtualListViewAdapter
{
	VirtualListViewSnapshot CaptureSnapshot();
}

public sealed record SnapshotItem
{
	public SnapshotItem(string key, long revision, object? data)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		Key = key;
		Revision = revision;
		Data = data;
	}
	public string Key { get; }
	public long Revision { get; }
	public object? Data { get; }
}

public sealed class SnapshotSection
{
	public SnapshotSection(string key, long revision, object? data, IEnumerable<SnapshotItem> items)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		ArgumentNullException.ThrowIfNull(items);
		Key = key;
		Revision = revision;
		Data = data;
		var copy = items.ToArray();
		var keys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in copy)
			if (item is null || !keys.Add(item.Key))
				throw new ArgumentException("Every item occurrence in a section needs a unique non-null key.", nameof(items));
		Items = Array.AsReadOnly(copy);
	}
	public string Key { get; }
	public long Revision { get; }
	public object? Data { get; }
	public IReadOnlyList<SnapshotItem> Items { get; }
}

/// <summary>Copies order and identity metadata. Payloads must not be mutated concurrently with native reads.</summary>
public sealed class VirtualListViewSnapshot : IVirtualListViewAdapter
{
	public VirtualListViewSnapshot(long revision, IEnumerable<SnapshotSection> sections)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(revision);
		ArgumentNullException.ThrowIfNull(sections);
		Revision = revision;
		var copy = sections.ToArray();
		var keys = new HashSet<string>(StringComparer.Ordinal);
		var count = 0;
		foreach (var section in copy)
		{
			if (section is null || !keys.Add(section.Key))
				throw new ArgumentException("Every section occurrence needs a unique non-null key.", nameof(sections));
			count = checked(count + section.Items.Count);
		}
		Sections = Array.AsReadOnly(copy);
	}
	public long Revision { get; }
	public IReadOnlyList<SnapshotSection> Sections { get; }
	public int GetNumberOfSections() => Sections.Count;
	public int GetNumberOfItemsInSection(int sectionIndex) => Sections[sectionIndex].Items.Count;
	public object? GetSection(int sectionIndex) => Sections[sectionIndex].Data;
	public object? GetItem(int sectionIndex, int itemIndex) => Sections[sectionIndex].Items[itemIndex].Data;
	event EventHandler IVirtualListViewAdapter.OnDataInvalidated { add { } remove { } }
	void IVirtualListViewAdapter.InvalidateData() => throw new InvalidOperationException("A snapshot is immutable. Submit a new snapshot through its adapter.");
}

/// <summary>Carries the exact captured input, not a later read of a mutable source.</summary>
public sealed class SnapshotInvalidatedEventArgs : EventArgs
{
	public SnapshotInvalidatedEventArgs(VirtualListViewSnapshot snapshot, bool reset = false)
	{
		Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
		Reset = reset;
	}
	public VirtualListViewSnapshot Snapshot { get; }
	public bool Reset { get; }
}

public sealed class SnapshotAdapter : ISnapshotVirtualListViewAdapter
{
	readonly object sync = new();
	readonly Queue<SnapshotInvalidatedEventArgs> pending = new();
	bool notifying;
	VirtualListViewSnapshot snapshot;

	public SnapshotAdapter(VirtualListViewSnapshot initialSnapshot)
		=> snapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));

	public event EventHandler? OnDataInvalidated;
	public VirtualListViewSnapshot CaptureSnapshot() { lock (sync) return snapshot; }

	/// <summary>Revisions must increase. Use reset for a wholesale query replacement.</summary>
	public void Submit(VirtualListViewSnapshot next, bool reset = false)
	{
		ArgumentNullException.ThrowIfNull(next);
		lock (sync)
		{
			SnapshotValidation.Validate(snapshot, next, reset);
			snapshot = next;
			Notify(new SnapshotInvalidatedEventArgs(next, reset));
		}
	}

	public void InvalidateData()
	{
		lock (sync)
			Notify(new SnapshotInvalidatedEventArgs(snapshot, true));
	}

	void Notify(SnapshotInvalidatedEventArgs args)
	{
		pending.Enqueue(args);
		if (notifying)
			return;
		notifying = true;
		try
		{
			while (pending.TryDequeue(out var next))
				OnDataInvalidated?.Invoke(this, next);
		}
		finally { notifying = false; }
	}
	public int GetNumberOfSections() => CaptureSnapshot().GetNumberOfSections();
	public int GetNumberOfItemsInSection(int sectionIndex) => CaptureSnapshot().GetNumberOfItemsInSection(sectionIndex);
	public object? GetSection(int sectionIndex) => CaptureSnapshot().GetSection(sectionIndex);
	public object? GetItem(int sectionIndex, int itemIndex) => CaptureSnapshot().GetItem(sectionIndex, itemIndex);
}

internal static class SnapshotValidation
{
	public static void Validate(VirtualListViewSnapshot previous, VirtualListViewSnapshot next, bool reset)
	{
		if (next.Revision <= previous.Revision)
			throw new ArgumentException("Snapshot revisions must strictly increase.", nameof(next));
		if (reset)
			return;
		var sections = previous.Sections.ToDictionary(section => section.Key, StringComparer.Ordinal);
		foreach (var section in next.Sections)
		{
			if (!sections.TryGetValue(section.Key, out var oldSection))
				continue;
			if (section.Revision < oldSection.Revision)
				throw new ArgumentException($"Content revision decreased for section '{section.Key}'.", nameof(next));
			var items = oldSection.Items.ToDictionary(item => item.Key, StringComparer.Ordinal);
			foreach (var item in section.Items)
				if (items.TryGetValue(item.Key, out var oldItem) && item.Revision < oldItem.Revision)
					throw new ArgumentException($"Content revision decreased for item '{item.Key}'.", nameof(next));
		}
	}
}
