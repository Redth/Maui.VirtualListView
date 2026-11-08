using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

internal sealed class RvRecycledViewPool : RecyclerView.RecycledViewPool
{
	// RecyclerView's default is 5 holders per view type, which is too few for a list that's mostly one
	// row type: a full rebind (NotifyDataSetChanged) or a fast fling hands back every visible row of that
	// type at once, and the ones over the cap get released (their MAUI views torn down) only to be rebuilt
	// moments later. Keep at least a screenful (plus a couple) per type.
	const int MinPerType = 5;
	int MaxPerType => Math.Max(MinPerType, VisibleCount + 2);
	readonly HashSet<int> types = new();
	readonly Func<int> visibleCount;
	int count;
	int visibleHighWater;

	// The most rows seen on screen. The live child count drops to ~0 while a full rebind detaches every
	// row, which is exactly when they're all handed to the pool, so sizing from it would evict them.
	int VisibleCount => visibleHighWater = Math.Max(visibleHighWater, visibleCount());

	public RvRecycledViewPool(Func<int> visibleCount) => this.visibleCount = visibleCount;

	public override void PutRecycledView(RecyclerView.ViewHolder holder)
	{
		var type = holder.ItemViewType;
		base.SetMaxRecycledViews(type, MaxPerType);
		var capacity = Math.Max(4, VisibleCount * 2);
		TrimTo(capacity);
		if (base.GetRecycledViewCount(type) >= MaxPerType)
		{
			Release(holder);
			// Keep RecyclerView's own reset/discard bookkeeping.
			base.PutRecycledView(holder);
			return;
		}

		TrimTo(capacity - 1);
		base.PutRecycledView(holder);
		types.Add(type);
		count++;
	}

	void TrimTo(int capacity)
	{
		while (count > capacity)
		{
			var evicted = GetRecycledView(types.First());
			if (evicted is not null)
				Release(evicted);
		}
	}

	public override RecyclerView.ViewHolder GetRecycledView(int viewType)
	{
		var holder = base.GetRecycledView(viewType);
		if (holder is not null)
			count--;
		if (base.GetRecycledViewCount(viewType) == 0)
			types.Remove(viewType);
		return holder;
	}

	public override void Clear()
	{
		while (types.Count > 0)
		{
			var holder = GetRecycledView(types.First());
			if (holder is not null)
				Release(holder);
		}
		base.Clear();
		count = 0;
	}

	static void Release(RecyclerView.ViewHolder holder)
	{
		if (holder is RvItemHolder item)
			item.Release();
	}
}
