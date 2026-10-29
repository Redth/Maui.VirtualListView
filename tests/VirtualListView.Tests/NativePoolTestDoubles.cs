// Only the native pool boundary is substituted; the eviction algorithm is source-linked.
namespace AndroidX.RecyclerView.Widget
{
	public class RecyclerView
	{
		public class ViewHolder
		{
			public ViewHolder(int type) => ItemViewType = type;
			public int ItemViewType { get; }
			public int NativeReturns { get; set; }
		}

		public class RecycledViewPool
		{
			readonly Dictionary<int, Stack<ViewHolder>> entries = new();
			readonly Dictionary<int, int> capacities = new();
			public virtual void PutRecycledView(ViewHolder holder)
			{
				holder.NativeReturns++;
				if (!entries.TryGetValue(holder.ItemViewType, out var stack))
					entries.Add(holder.ItemViewType, stack = new Stack<ViewHolder>());
				if (stack.Count < capacities.GetValueOrDefault(holder.ItemViewType, 5))
					stack.Push(holder);
			}
			public virtual ViewHolder GetRecycledView(int type)
				=> entries.TryGetValue(type, out var stack) && stack.TryPop(out var holder) ? holder : null;
			public int GetRecycledViewCount(int type) => entries.TryGetValue(type, out var stack) ? stack.Count : 0;
			public void SetMaxRecycledViews(int type, int capacity) => capacities[type] = capacity;
			public virtual void Clear() => entries.Clear();
		}
	}
}

namespace Microsoft.Maui
{
	internal sealed class RvItemHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
	{
		public RvItemHolder(int type) : base(type) { }
		public int Releases { get; private set; }
		public void Release() => Releases++;
	}
}
