using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

internal partial class RvAdapter : RecyclerView.Adapter
{
	readonly VirtualListViewHandler handler;

	readonly object lockObj = new object();

	readonly PositionalViewSelector positionalViewSelector;

	RvViewHolderClickListener clickListener;

	public Context Context { get; }

	public object BindingContext { get; set; }

	public override int ItemCount
		=> positionalViewSelector?.TotalCount ?? 0;

	internal RvAdapter(Context context, VirtualListViewHandler handler, PositionalViewSelector positionalViewSelector)
	{
		Context = context;
		HasStableIds = false;

		this.handler = handler;
		this.positionalViewSelector = positionalViewSelector;
	}

	public float DisplayScale =>
		handler?.Context?.Resources.DisplayMetrics.Density ?? 1;

	public override void OnViewAttachedToWindow(Java.Lang.Object holder)
	{
		base.OnViewAttachedToWindow(holder);

		if (holder is RvItemHolder rvItemHolder && rvItemHolder?.ViewContainer?.VirtualView != null)
			handler.VirtualView.ViewSelector.ViewAttached(rvItemHolder.PositionInfo, rvItemHolder.ViewContainer.VirtualView);
	}

	public override void OnViewDetachedFromWindow(Java.Lang.Object holder)
	{
		if (holder is RvItemHolder rvItemHolder && rvItemHolder?.ViewContainer?.VirtualView != null)
			handler.VirtualView.ViewSelector.ViewDetached(rvItemHolder.PositionInfo, rvItemHolder.ViewContainer.VirtualView);

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
			var data = info.Kind switch {
				PositionKind.Item =>
					positionalViewSelector?.Adapter?.GetItem(info.SectionIndex, info.ItemIndex),
				PositionKind.SectionHeader =>
					positionalViewSelector?.Adapter?.GetSection(info.SectionIndex),
				PositionKind.SectionFooter =>
					positionalViewSelector?.Adapter?.GetSection(info.SectionIndex),
				_ => null
			};

			var view = itemHolder?.VirtualView ?? positionalViewSelector?.ViewSelector?.CreateView(info, data);

			itemHolder.Update(info, view);

			positionalViewSelector?.ViewSelector?.RecycleView(info, data, itemHolder.ViewContainer.VirtualView);
		}
	}

	Dictionary<string, int> cachedReuseIds = new ();
	int reuseIdCount = 100;

	public override int GetItemViewType(int position)
	{
		base.GetItemViewType(position);

		var info = positionalViewSelector.GetInfo(position);

		var data = info.Kind switch {
			PositionKind.Item =>
				positionalViewSelector?.Adapter?.GetItem(info.SectionIndex, info.ItemIndex),
			PositionKind.SectionHeader =>
				positionalViewSelector?.Adapter?.GetSection(info.SectionIndex),
			PositionKind.SectionFooter =>
				positionalViewSelector?.Adapter?.GetSection(info.SectionIndex),
			_ => null
		};

		var reuseId = positionalViewSelector.ViewSelector.GetReuseId(info, data);

		int vt = -1;

		lock (lockObj)
		{
			if (!cachedReuseIds.TryGetValue(reuseId, out var reuseIdNumber))
			{
				reuseIdNumber = ++reuseIdCount;
				cachedReuseIds.Add(reuseId, reuseIdNumber);
			}

			vt = reuseIdNumber;
		}

		return vt;
	}

	public override long GetItemId(int position)
		=> RecyclerView.NoId;

	public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
	{
		var viewHolder = new RvItemHolder(handler.MauiContext, handler.VirtualView.Orientation);

		clickListener = new RvViewHolderClickListener(viewHolder, rvh =>
		{
			if (rvh.PositionInfo == null || rvh.PositionInfo.Kind != PositionKind.Item)
				return;

			var p = new ItemPosition(rvh.PositionInfo.SectionIndex, rvh.PositionInfo.ItemIndex);

			rvh.PositionInfo.IsSelected = !rvh.PositionInfo.IsSelected;
			
			if (rvh.VirtualView is IPositionInfo positionInfo)
				positionInfo.IsSelected = rvh.PositionInfo.IsSelected;

			if (rvh.PositionInfo.IsSelected)
				handler.VirtualView?.SelectItems(p);
			else
				handler.VirtualView?.DeselectItems(p);
		});

		viewHolder.ItemView.SetOnClickListener(clickListener);

		return viewHolder;
	}

	public void Reset()
	{
		//lock (lockObj)
		//{
		//	cachedReuseIds.Clear();
		//}
	}
}