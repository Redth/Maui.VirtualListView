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

		public override void OnDetachedFromRecyclerView(RecyclerView recyclerView)
		{
			base.OnDetachedFromRecyclerView(recyclerView);

			
		}

		public override void OnViewDetachedFromWindow(Java.Lang.Object holder)
		{
			base.OnViewDetachedFromWindow(holder);
		}

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
				itemHolder.PositionInfo = info;

				var newView = positionalViewSelector.ViewSelector.ViewFor(info.Kind, info.SectionIndex, info.ItemIndex);
				itemHolder.WrapperView.ReplaceView(newView);

				if (itemHolder.NativeView.ChildCount <= 0)
					itemHolder.NativeView.AddView(itemHolder.WrapperView.ReplacedView.ToNative(handler.MauiContext));
			}
		}

		public override int GetItemViewType(int position)
		{
			base.GetItemViewType(position);

			var info = positionalViewSelector.GetInfo(position);
			var reuseId = positionalViewSelector.GetReuseId(info.Kind, info.SectionIndex, info.ItemIndex);
			
			Console.WriteLine($"{position} => {info.SectionIndex}.{info.ItemIndex} ({reuseId})");
			return reuseId;
		}

		public override long GetItemId(int position)
			=> position;

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var wrapper = new ReplaceableWrapperView(parent.Context)
			{
				//MatchWidth = true,
				LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.WrapContent)
			};

			var viewHolder = new RvItemHolder(wrapper, wrapper);

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