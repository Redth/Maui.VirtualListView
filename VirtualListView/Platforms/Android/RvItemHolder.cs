using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui;

internal class RvItemHolder : RecyclerView.ViewHolder
{
	public RvViewContainer ViewContainer { get; private set; }
	public PositionInfo PositionInfo => Content.Position;
	internal RetainedView Content { get; }
	internal RvAdapter.RvViewHolderClickListener ClickListener { get; set; }
	bool released;

	public RvItemHolder(VirtualListViewHandler handler)
		: base(new RvViewContainer(handler.MauiContext)
		{
			LayoutParameters = new RecyclerView.LayoutParams(
				handler.VirtualView.Orientation == ListOrientation.Vertical ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
				handler.VirtualView.Orientation == ListOrientation.Vertical ? ViewGroup.LayoutParams.WrapContent : ViewGroup.LayoutParams.MatchParent)
		})
	{
		ViewContainer = ItemView as RvViewContainer;
		var container = new WeakReference<RvViewContainer>(ViewContainer);
		Content = new RetainedView(handler.VirtualView,
			view =>
			{
				if (!container.TryGetTarget(out var host))
					throw new InvalidOperationException("The Android content host is no longer available.");
				host.SetupView(view);
			},
			() =>
			{
				if (container.TryGetTarget(out var host))
					host.ClearView();
			});
	}

	public IView VirtualView
		=> ViewContainer?.VirtualView;

	public bool NeedsView
        => ViewContainer?.NeedsView ?? true;

	public void SetupView(IView view)
	{
		ViewContainer.SetupView(view);
	}

	public void UpdatePosition(PositionInfo positionInfo)
	{
		ViewContainer.UpdatePosition(positionInfo);
	}

	internal void Release()
	{
		if (released)
			return;
		released = true;
		ItemView.SetOnClickListener(null);
		ClickListener?.Dispose();
		ClickListener = null;
		Content.Release();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			Release();
		base.Dispose(disposing);
	}
}