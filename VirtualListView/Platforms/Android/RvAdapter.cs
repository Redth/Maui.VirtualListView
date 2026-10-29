using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

internal partial class RvAdapter : RecyclerView.Adapter
{
	readonly VirtualListViewHandler handler;

	readonly object lockObj = new object();

	readonly PositionalViewSelector positionalViewSelector;

	readonly NativeViewRegistry<RvItemHolder> holders = new();

	public Context Context { get; }

	public object BindingContext { get; set; }

	int? cachedItemCount = null;

	public override int ItemCount
		=> (cachedItemCount ??= positionalViewSelector?.TotalCount ?? 0);

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

		if (holder is RvItemHolder rvItemHolder)
			rvItemHolder.Content.SetVisible(true);
	}

	public override void OnViewDetachedFromWindow(Java.Lang.Object holder)
	{
		if (holder is RvItemHolder rvItemHolder)
			rvItemHolder.Content.SetVisible(false);

		base.OnViewDetachedFromWindow(holder);
	}

	public override void OnViewRecycled(Java.Lang.Object holder)
	{
		if (holder is RvItemHolder itemHolder)
			itemHolder.Content.Unbind();
		base.OnViewRecycled(holder);
	}

	public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
	{
		var info = positionalViewSelector.GetInfo(position);

		if (info == null)
		{
			if (holder is RvItemHolder stale)
				stale.Content.Unbind();
			return;
		}

		// The template selector doesn't infer selected properly
		// so we need to ask the listview which tracks selections about the state
		info.IsSelected = info.Kind == PositionKind.Item
			&& (handler?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false);

		if (holder is RvItemHolder itemHolder)
		{
			itemHolder.ViewContainer.UpdateOrientation();
			var data = positionalViewSelector.Adapter.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);
			itemHolder.Content.Bind(info, data);
		}
	}

	Dictionary<string, int> cachedReuseIds = new ();
	int reuseIdCount = 100;

	public override int GetItemViewType(int position)
	{
		base.GetItemViewType(position);

		var info = positionalViewSelector.GetInfo(position);

		var data = positionalViewSelector.Adapter.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);

		var reuseId = info.Kind is PositionKind.Header or PositionKind.Footer
			? info.Kind.ToString() : positionalViewSelector.ViewSelector.GetReuseId(info, data);

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
		var viewHolder = new RvItemHolder(handler);
		holders.Add(viewHolder, viewHolder.Content);

		viewHolder.ClickListener = new RvViewHolderClickListener(viewHolder, rvh =>
		{
			if (!handler.IsConnected || rvh.PositionInfo == null || rvh.PositionInfo.Kind != PositionKind.Item)
				return;

			var p = new ItemPosition(rvh.PositionInfo.SectionIndex, rvh.PositionInfo.ItemIndex);

			rvh.PositionInfo.IsSelected = !rvh.PositionInfo.IsSelected;
			
			if (rvh.VirtualView is IPositionInfo positionInfo)
				positionInfo.IsSelected = rvh.PositionInfo.IsSelected;

			if (rvh.PositionInfo.IsSelected)
				handler?.VirtualView?.SelectItem(p);
			else
				handler?.VirtualView?.DeselectItem(p);
		});

		viewHolder.ItemView.SetOnClickListener(viewHolder.ClickListener);

		return viewHolder;
	}

	public void Reset()
	{
		cachedItemCount = null;
		holders.ForEach(_ => { });
	}

	internal void ResetTemplates() => holders.ForEach(holder => holder.Content.Release());
	internal void UpdateOrientation() => holders.ForEach(holder => holder.ViewContainer.UpdateOrientation());

	internal void InvalidateContentLayout(PositionInfo position)
	{
		holders.ForEach(holder =>
		{
			if (ReferenceEquals(holder.PositionInfo, position))
				holder.ViewContainer.RequestLayout();
		});
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			holders.Drain(holder => holder.Release());
			cachedReuseIds.Clear();
		}
		base.Dispose(disposing);
	}
}