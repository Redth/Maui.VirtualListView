using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Maui
{
    internal class RvAdapter : RecyclerView.Adapter
	{
		readonly VirtualListViewHandler handler;

		readonly List<IViewTemplate> templates;

		readonly object lockObj = new object();

		readonly IVirtualListViewAdapter adapter;

		RvViewHolderClickListener clickListener;

		public Context Context { get; }

		public object BindingContext { get; set; }

		internal bool HasHeader => handler?.VirtualView?.HeaderTemplate != null;
		internal bool HasFooter => handler?.VirtualView?.HeaderTemplate != null;
		internal bool HasSectionHeader => handler?.VirtualView?.SectionHeaderTemplate != null;
		internal bool HasSectionFooter => handler?.VirtualView?.SectionFooterTemplate != null;

		public override int ItemCount
			=> PositionTemplateSelector.GetTotalCount(adapter, HasHeader, HasFooter, HasSectionHeader, HasSectionFooter);

		internal RvAdapter(Context context, IVirtualListViewAdapter adapter, VirtualListViewHandler handler)
		{
			Context = context;
			this.adapter = adapter;
			this.handler = handler;

			templates = new List<IViewTemplate>();
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var info = PositionTemplateSelector.GetInfo(adapter, position, HasHeader, HasFooter, HasSectionHeader, HasSectionFooter);

			if (info == null)
				return;

			// The template selector doesn't infer selected properly
			// so we need to ask the listview which tracks selections about the state
			info.IsSelected = info.Kind == PositionKind.Item
				&& (handler?.VirtualView?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false);

			if (holder is RvItemHolder itemHolder && itemHolder.View != null)
				itemHolder.Update(info);
		}

		public override int GetItemViewType(int position)
		{
			int viewType = base.GetItemViewType(position);

			var template = PositionTemplateSelector.GetTemplate(
				adapter,
				position,
				handler?.VirtualView?.HeaderTemplate,
				handler?.VirtualView?.FooterTemplate,
				handler?.VirtualView?.SectionHeaderTemplate,
				handler?.VirtualView?.SectionFooterTemplate,
				handler?.VirtualView?.ItemTemplate);

			lock (lockObj)
			{
				viewType = templates.IndexOf(template);

				if (viewType < 0)
				{
					templates.Add(template);
					viewType = templates.Count - 1;
				}
			}

			return viewType;
		}

		public override long GetItemId(int position)
			=> position;

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var template = templates.ElementAtOrDefault(viewType);

			var wrapper = new WrapperView(parent.Context)
			{
				//MatchWidth = true,
				LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				ViewGroup.LayoutParams.WrapContent)
			};

			var viewHolder = new RvItemHolder(wrapper, template);

			clickListener = new RvViewHolderClickListener(viewHolder, rvh =>
			{
				if (rvh.PositionInfo == null || rvh.PositionInfo.Kind != PositionKind.Item)
					return;

				var p = new ItemPosition(rvh.PositionInfo.SectionIndex, rvh.PositionInfo.ItemIndex);

				rvh.PositionInfo.IsSelected = !rvh.PositionInfo.IsSelected;
				rvh.Update(rvh.PositionInfo);

				if (rvh.PositionInfo.IsSelected)
					handler.VirtualView?.SetSelected(p);
				else
					handler.VirtualView?.SetDeselected(p);
			});

			viewHolder.ItemView.SetOnClickListener(clickListener);

			return viewHolder;
		}

		class RvViewHolderClickListener : Java.Lang.Object, View.IOnClickListener
		{
			public RvViewHolderClickListener(RvItemHolder viewHolder, Action<RvItemHolder> clickHandler)
			{
				ViewHolder = viewHolder;
				ClickHandler = clickHandler;
			}

			public RvItemHolder ViewHolder { get; }

			public Action<RvItemHolder> ClickHandler { get; }

			public void OnClick(View v)
			{
				if (ViewHolder?.PositionInfo != null && ViewHolder.PositionInfo.Kind == PositionKind.Item)
					ClickHandler?.Invoke(ViewHolder);
			}
		}
	}
}