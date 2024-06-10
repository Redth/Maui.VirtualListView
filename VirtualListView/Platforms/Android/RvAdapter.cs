#nullable enable
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui;

internal partial class RvAdapter : RecyclerView.Adapter
{
	readonly VirtualListViewHandler handler;

	readonly object lockObj = new object();

	readonly IPositionalPlatformController positionalPlatformController;

	RvViewHolderClickListener clickListener;

	public Context Context { get; }

	public object BindingContext { get; set; }

	int? cachedItemCount = null;

	public override int ItemCount
		=> (cachedItemCount ??= positionalPlatformController?.TotalCount ?? 0);

	internal RvAdapter(Context context, VirtualListViewHandler handler, IPositionalPlatformController positionalPlatformController)
	{
		Context = context;
		HasStableIds = false;

		this.handler = handler;
		this.positionalPlatformController = positionalPlatformController;
	}

	public float DisplayScale =>
		handler.Context?.Resources.DisplayMetrics.Density ?? 1;

	public override void OnViewAttachedToWindow(Java.Lang.Object holder)
	{
		base.OnViewAttachedToWindow(holder);

		if (holder is RvItemHolder rvItemHolder && rvItemHolder.ViewContainer?.VirtualView != null)
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
		var info = positionalPlatformController.GetInfo(position);

		if (info == null)
			return;

		// The template selector doesn't infer selected properly
		// so we need to ask the listview which tracks selections about the state
		info.IsSelected = info.Kind == PositionKind.Item
			&& handler.IsItemSelected(info.SectionIndex, info.ItemIndex);

		if (holder is RvItemHolder itemHolder)
		{
			var data = info.Kind switch {
				PositionKind.Item =>
					handler.Adapter.GetItem(info.SectionIndex, info.ItemIndex),
				PositionKind.SectionHeader =>
					handler.Adapter.GetSection(info.SectionIndex),
				PositionKind.SectionFooter =>
					handler.Adapter.GetSection(info.SectionIndex),
				_ => null
			};

			if (itemHolder.NeedsView)
			{
				var newView = handler.ViewSelector.CreateView(info, data);
				itemHolder.SetupView(newView);
            }

            itemHolder.UpdatePosition(info);

            handler.ViewSelector.RecycleView(info, data, itemHolder.ViewContainer.VirtualView);
		}
	}

	Dictionary<string, int> cachedReuseIds = new ();
	int reuseIdCount = 100;

	public override int GetItemViewType(int position)
	{
		base.GetItemViewType(position);

		var info = positionalPlatformController.GetInfo(position);

		var data = info.Kind switch {
			PositionKind.Item =>
				handler.Adapter.GetItem(info.SectionIndex, info.ItemIndex),
			PositionKind.SectionHeader =>
				handler.Adapter.GetSection(info.SectionIndex),
			PositionKind.SectionFooter =>
				handler.Adapter.GetSection(info.SectionIndex),
			_ => null
		};

		var reuseId = handler.ViewSelector.GetReuseId(info, data);

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
				handler?.VirtualView?.SelectItem(p);
			else
				handler?.VirtualView?.DeselectItem(p);
		});

		viewHolder.ItemView.SetOnClickListener(clickListener);

		return viewHolder;
	}

	public void Reset()
	{
		cachedItemCount = null;
		//lock (lockObj)
		//{
		//	cachedReuseIds.Clear();
		//}
	}
}