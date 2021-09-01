using Android.Graphics;
using Android.Util;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui
{
	class RvItemSpacingDecorator : RecyclerView.ItemDecoration
	{
		readonly IVirtualListView virtualListView;
		readonly PositionalViewSelector positionalViewSelector;

		public RvItemSpacingDecorator(IVirtualListView virtualListView, PositionalViewSelector positionalViewSelector) : base()
		{
			this.virtualListView = virtualListView;
			this.positionalViewSelector = positionalViewSelector;
		}

		public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
		{
			base.GetItemOffsets(outRect, view, parent, state);

			var positionIndex = parent.GetChildAdapterPosition(view);

			if (virtualListView.ItemSpacing > 0)
			{
				var positionInfo = positionalViewSelector.GetInfo(positionIndex);

				if (positionInfo.ItemIndex != 0)
				{
					var px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, (float)virtualListView.ItemSpacing, global::Android.App.Application.Context.Resources.DisplayMetrics);

					if (virtualListView.Orientation == ListOrientation.Vertical)
					{
						outRect.Top = px;
						outRect.Left = 0;
						outRect.Bottom = 0;
						outRect.Right = 0;
					}
					else
					{
						outRect.Top = 0;
						outRect.Left = px;
						outRect.Bottom = 0;
						outRect.Right = 0;
					}
				}
			}
			else
			{
				outRect.Top = 0;
				outRect.Left = 0;
				outRect.Bottom = 0;
				outRect.Right = 0;
			}
		}
	}
}