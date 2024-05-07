using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

class RvSpanLookup : GridLayoutManager.SpanSizeLookup
{
	readonly PositionalViewSelector positionalViewSelector;
	readonly IVirtualListView virtualListView;

	public RvSpanLookup(IVirtualListView virtualListView, PositionalViewSelector positionalViewSelector)
	{
		this.virtualListView = virtualListView;
		this.positionalViewSelector = positionalViewSelector;
	}

	public override int GetSpanSize(int position)
	{
		var columns = virtualListView.Columns;

		if (columns > 1)
		{
			var kind = positionalViewSelector.GetInfo(position)?.Kind;

			if (kind == PositionKind.Item)
				return 1;
		}

		return columns;
	}
}