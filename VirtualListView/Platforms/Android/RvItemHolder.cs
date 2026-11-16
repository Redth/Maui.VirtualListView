using Android.Views;
using AndroidX.Core.View;
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
		: base(new RvViewContainer(handler))
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
		ViewContainer.SetRetainedView(Content);
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

	// Syncs the row container's native accessibility state with its current position: only selectable
	// items are clickable (so TalkBack doesn't offer "double-tap to activate" on headers, or on any row
	// while selection is off), rows are read as one unit, section headers are headings, and the selected
	// state is the native one TalkBack announces.
	internal void UpdateAccessibility(SelectionMode mode)
	{
		var position = Content.Position;
		var view = ItemView;
		view.Clickable = RowAccessibility.IsSelectable(position, mode);
		ViewCompat.SetScreenReaderFocusable(view, RowAccessibility.IsRow(position));
		ViewCompat.SetAccessibilityHeading(view, RowAccessibility.IsHeading(position));
		view.Selected = RowAccessibility.IsSelected(position, mode);
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