#nullable enable
namespace Microsoft.Maui;

internal sealed class HandlerSelection
{
	ItemPosition[] positions = Array.Empty<ItemPosition>();
	HashSet<ItemPosition> selected = new();

	public IReadOnlyList<ItemPosition> Positions => positions;

	public bool Contains(ItemPosition position) => selected.Contains(position);

	public void Clear()
	{
		positions = Array.Empty<ItemPosition>();
		selected.Clear();
	}

	public void Reset(IEnumerable<ItemPosition> selection)
	{
		positions = selection.ToArray();
		selected = positions.ToHashSet();
	}

	public void Apply(IList<ItemPosition>? selection, SelectionMode mode, PositionalViewSelector projection,
		Action<ItemPosition, bool> updateSelection)
	{
		var next = selection ?? Array.Empty<ItemPosition>();
		if (mode == SelectionMode.None)
			next = Array.Empty<ItemPosition>();
		else if (mode == SelectionMode.Single && next.Count > 1)
			next = next.Take(1).ToArray();

		var nextPositions = new List<ItemPosition>();
		var nextSet = new HashSet<ItemPosition>();
		foreach (var position in next)
		{
			if (projection.GetPosition(position.SectionIndex, position.ItemIndex) >= 0 && nextSet.Add(position))
				nextPositions.Add(position);
		}

		foreach (var position in positions)
		{
			if (!nextSet.Contains(position))
				updateSelection(position, false);
		}

		foreach (var position in nextPositions)
		{
			if (!selected.Contains(position))
				updateSelection(position, true);
		}

		positions = nextPositions.ToArray();
		selected = nextSet;
	}
}
