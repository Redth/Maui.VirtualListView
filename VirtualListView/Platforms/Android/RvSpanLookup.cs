using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

class RvSpanLookup : GridLayoutManager.SpanSizeLookup
{
	readonly IPositionalPlatformController positionalPlatformController;
	readonly IVirtualListView virtualListView;

	public RvSpanLookup(IVirtualListView virtualListView, IPositionalPlatformController positionalPlatformController)
	{
		this.virtualListView = virtualListView;
		this.positionalPlatformController = positionalPlatformController;
	}

	public override int GetSpanSize(int position)
	{
		var columns = virtualListView.Columns;

		if (columns > 1)
		{
			var kind = positionalPlatformController.GetInfo(position)?.Kind;

			if (kind == PositionKind.Item)
				return 1;
		}

		return columns;
	}
}