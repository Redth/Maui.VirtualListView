using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Maui
{
	internal partial class RvAdapter : RecyclerView.Adapter
	{
		readonly VirtualListViewHandler handler;

		readonly object lockObj = new object();

		readonly PositionalViewSelector positionalViewSelector;

		RvViewHolderClickListener clickListener;

		public Context Context { get; }

		public object BindingContext { get; set; }

		public override int ItemCount
			=> positionalViewSelector?.GetTotalCount() ?? 0;

		internal RvAdapter(Context context, VirtualListViewHandler handler, PositionalViewSelector positionalViewSelector)
		{
			Context = context;
			this.handler = handler;
			this.positionalViewSelector = positionalViewSelector;
		}

		public float DisplayScale =>
			handler?.Context?.Resources.DisplayMetrics.Density ?? 1;

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = positionalViewSelector.GetInfo(position);

			if (info == null)
				return;

			// The template selector doesn't infer selected properly
			// so we need to ask the listview which tracks selections about the state
			info.IsSelected = info.Kind == PositionKind.Item
				&& (handler?.VirtualView?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false);

			if (holder is RvItemHolder itemHolder)
			{
				var newView = positionalViewSelector.ViewSelector.ViewFor(info.Kind, info.SectionIndex, info.ItemIndex);

				itemHolder.Update(info, newView);
			}
		}

		public override int GetItemViewType(int position)
		{
			base.GetItemViewType(position);

			var info = positionalViewSelector.GetInfo(position);
			var reuseId = positionalViewSelector.GetReuseId(info.Kind, info.SectionIndex, info.ItemIndex);

			return reuseId;
		}

		public override long GetItemId(int position)
			=> position;

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var viewHolder = new RvItemHolder(handler.MauiContext);

			clickListener = new RvViewHolderClickListener(viewHolder, rvh =>
			{
				if (rvh.PositionInfo == null || rvh.PositionInfo.Kind != PositionKind.Item)
					return;

				var p = new ItemPosition(rvh.PositionInfo.SectionIndex, rvh.PositionInfo.ItemIndex);

				rvh.PositionInfo.IsSelected = !rvh.PositionInfo.IsSelected;

				if (rvh.PositionInfo.IsSelected)
					handler.VirtualView?.SetSelected(p);
				else
					handler.VirtualView?.SetDeselected(p);
			});

			viewHolder.ItemView.SetOnClickListener(clickListener);

			return viewHolder;
		}
	}
}