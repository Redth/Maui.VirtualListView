#nullable enable
using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal readonly record struct PresentationKey(PositionKind Kind, string Section, string Item);
internal sealed record KeyedRow(PresentationKey Key, long Revision, PositionInfo Position, object? Data);

internal sealed class KeyedRows
{
	public KeyedRows(PositionalViewSelector projection, VirtualListViewSnapshot snapshot)
	{
		Snapshot = snapshot;
		Rows = new KeyedRow[projection.TotalCount];
		for (var p = 0; p < Rows.Length; p++)
		{
			var info = projection.GetInfo(p);
			var section = info.SectionIndex >= 0 ? snapshot.Sections[info.SectionIndex] : null;
			var item = info.Kind == PositionKind.Item ? section!.Items[info.ItemIndex] : null;
			var key = new PresentationKey(info.Kind, section?.Key ?? "", item?.Key ?? "");
			var row = new KeyedRow(key, item?.Revision ?? section?.Revision ?? 0, info,
				projection.Adapter.DataFor(info.Kind, info.SectionIndex, info.ItemIndex));
			Rows[p] = row;
			if (!Indices.TryAdd(key, p))
				throw new InvalidOperationException("Duplicate presentation key in the flat projection.");
			if (info.Kind == PositionKind.Item)
				Items.Add(new ItemPosition(info.SectionIndex, info.ItemIndex), key);
		}
	}

	public VirtualListViewSnapshot Snapshot { get; }
	public KeyedRow[] Rows { get; }
	public Dictionary<PresentationKey, int> Indices { get; } = new();
	public Dictionary<ItemPosition, PresentationKey> Items { get; } = new();

	public ItemPosition[] ReconcileSelection(IEnumerable<ItemPosition> selection, KeyedRows next)
	{
		var result = new List<ItemPosition>();
		foreach (var position in selection.Distinct())
			if (Items.TryGetValue(position, out var key) && next.Indices.TryGetValue(key, out var index))
			{
				var info = next.Rows[index].Position;
				result.Add(new ItemPosition(info.SectionIndex, info.ItemIndex));
			}
		return result.ToArray();
	}
}

internal enum RowEditKind { Insert, Remove, Move, Replace }
internal readonly record struct RowEdit(RowEditKind Kind, int Index, int From = -1);

// Sequential indices are used by RecyclerView/WinUI. UIKit uses the same accepted
// before/after keys, translated to old removal/move and final insertion coordinates.
internal sealed class KeyedUpdatePlan
{
	const int MaximumEdits = 128;
	public KeyedRows? Before { get; }
	public KeyedRows After { get; }
	public bool Reset { get; private set; }
	public List<RowEdit> Edits { get; } = new();
	public int[] Removed { get; private set; } = Array.Empty<int>();
	public int[] Inserted { get; private set; } = Array.Empty<int>();
	public (int From, int To)[] Moved { get; private set; } = Array.Empty<(int, int)>();
	public int[] Changed { get; private set; } = Array.Empty<int>();

	public KeyedUpdatePlan(KeyedRows? before, KeyedRows after, bool reset)
	{
		Before = before;
		After = after;
		Reset = reset || before is null;
		if (Reset)
			return;
		var old = before!;
		Removed = old.Rows.Select((row, index) => (row, index))
			.Where(pair => !after.Indices.ContainsKey(pair.row.Key)).Select(pair => pair.index).ToArray();
		Inserted = after.Rows.Select((row, index) => (row, index))
			.Where(pair => !old.Indices.ContainsKey(pair.row.Key)).Select(pair => pair.index).ToArray();
		Changed = after.Rows.Select((row, index) => (row, index))
			.Where(pair => old.Indices.TryGetValue(pair.row.Key, out var index)
				&& old.Rows[index].Revision != pair.row.Revision).Select(pair => pair.index).ToArray();
		if (Removed.Length + Inserted.Length + Changed.Length > MaximumEdits) { Reset = true; return; }
		var current = old.Rows.Select(row => row.Key).ToList();
		foreach (var index in Removed.Reverse())
		{
			current.RemoveAt(index);
			Edits.Add(new(RowEditKind.Remove, index));
		}
		if (Edits.Count > MaximumEdits) { Reset = true; return; }
		for (var index = 0; index < after.Rows.Length; index++)
		{
			var key = after.Rows[index].Key;
			if (index < current.Count && current[index] == key)
				continue;
			if (!old.Indices.ContainsKey(key))
			{
				current.Insert(index, key);
				Edits.Add(new(RowEditKind.Insert, index));
			}
			else
			{
				var from = current.IndexOf(key, index);
				if (from < 0)
					throw new InvalidOperationException("The update plan lost a surviving key.");
				current.RemoveAt(from);
				current.Insert(index, key);
				Edits.Add(new(RowEditKind.Move, index, from));
			}
			if (Edits.Count > MaximumEdits) { Reset = true; return; }
		}
		foreach (var index in Changed)
			Edits.Add(new(RowEditKind.Replace, index));
		Moved = FindMoves(old, after);
		Reset = Edits.Count > MaximumEdits;
	}

	static (int From, int To)[] FindMoves(KeyedRows before, KeyedRows after)
	{
		var survivors = after.Rows.Select((row, index) => (row, index))
			.Where(pair => before.Indices.ContainsKey(pair.row.Key))
			.Select(pair => (From: before.Indices[pair.row.Key], To: pair.index)).ToArray();
		var tails = new int[survivors.Length];
		var previous = new int[survivors.Length];
		var length = 0;
		for (var i = 0; i < survivors.Length; i++)
		{
			var low = 0;
			var high = length;
			while (low < high)
			{
				var middle = (low + high) / 2;
				if (survivors[tails[middle]].From < survivors[i].From) low = middle + 1;
				else high = middle;
			}
			previous[i] = low > 0 ? tails[low - 1] : -1;
			tails[low] = i;
			if (low == length) length++;
		}
		var stationary = new HashSet<int>();
		for (var i = length > 0 ? tails[length - 1] : -1; i >= 0; i = previous[i])
			stationary.Add(i);
		return survivors.Where((_, index) => !stationary.Contains(index)).ToArray();
	}
}

internal sealed record KeyedAnchor(KeyedRows Before, int Index, double Offset, long Intent, long Version);

internal sealed class KeyedAnchorState
{
	long intent;
	long version;
	public void Supersede() => intent++;
	public void Invalidate() { intent++; version++; }
	public KeyedAnchor? Capture(KeyedRows? rows, int index, double offset, KeyedAnchor? pending = null)
	{
		if (pending is not null && Resolve(pending, rows) >= 0)
		{
			rows = pending.Before;
			index = pending.Index;
			offset = pending.Offset;
		}
		version++;
		if (rows is null || index < 0 || index >= rows.Rows.Length || !double.IsFinite(offset))
			return null;
		return new(rows, index, offset, intent, version);
	}

	public int Resolve(KeyedAnchor anchor, KeyedRows? current)
	{
		if (current is null || anchor.Intent != intent || anchor.Version != version)
			return -1;
		if (current.Indices.TryGetValue(anchor.Before.Rows[anchor.Index].Key, out var index))
			return index;
		for (var p = anchor.Index + 1; p < anchor.Before.Rows.Length; p++)
			if (anchor.Before.Rows[p].Key.Kind == PositionKind.Item
				&& current.Indices.TryGetValue(anchor.Before.Rows[p].Key, out index))
				return index;
		for (var p = anchor.Index - 1; p >= 0; p--)
			if (anchor.Before.Rows[p].Key.Kind == PositionKind.Item
				&& current.Indices.TryGetValue(anchor.Before.Rows[p].Key, out index))
				return index;
		return -1;
	}
}
