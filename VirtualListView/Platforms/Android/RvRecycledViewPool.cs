using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

internal sealed class RvRecycledViewPool : RecyclerView.RecycledViewPool
{
	const int MaxPerType = 5;
	readonly HashSet<int> types = new();
	readonly Func<int> visibleCount;
	int count;

	public RvRecycledViewPool(Func<int> visibleCount) => this.visibleCount = visibleCount;

	public override void PutRecycledView(RecyclerView.ViewHolder holder)
	{
		var type = holder.ItemViewType;
		base.SetMaxRecycledViews(type, MaxPerType);
		var capacity = Math.Max(4, visibleCount() * 2);
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
