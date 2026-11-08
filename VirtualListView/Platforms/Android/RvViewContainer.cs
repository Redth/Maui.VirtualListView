using AView = global::Android.Views.View;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui;

sealed class RvViewContainer : global::Android.Widget.FrameLayout
{
	readonly VirtualListViewHandler handler;
	RetainedContentLayout contentLayout;
	WeakReference<RetainedView> retainedView;

	public RvViewContainer(VirtualListViewHandler handler)
		: this(handler.MauiContext)
	{
		this.handler = handler;
		UpdateOrientation();
	}

	RvViewContainer(IMauiContext context)
		: base(context.Context ?? throw new ArgumentNullException($"{nameof(context.Context)}"))
	{
		MauiContext = context;
		Id = AView.GenerateViewId();
	}

	public readonly IMauiContext MauiContext;

	public IView VirtualView { get; private set; }

	public AView NativeView { get; private set; }

	public bool NeedsView => VirtualView is null || VirtualView.Handler is null || NativeView is null;

	internal void SetRetainedView(RetainedView view) => retainedView = new(view);

	internal void UpdateOrientation()
	{
		var vertical = handler.VirtualView.Orientation == ListOrientation.Vertical;
		var width = vertical ? LayoutParams.MatchParent : LayoutParams.WrapContent;
		var height = vertical ? LayoutParams.WrapContent : LayoutParams.MatchParent;
		if (LayoutParameters is AndroidX.RecyclerView.Widget.RecyclerView.LayoutParams layout)
		{
			// RecyclerView stores the attached ViewHolder in these native parameters.
			layout.Width = width;
			layout.Height = height;
		}
		else
		{
			LayoutParameters = new AndroidX.RecyclerView.Widget.RecyclerView.LayoutParams(width, height);
		}
		RequestLayout();
	}

	protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		if (contentLayout is null)
		{
			SetMeasuredDimension(0, 0);
			return;
		}
		var axis = handler.VirtualView.Orientation;
		var crossSpec = axis == ListOrientation.Vertical ? widthMeasureSpec : heightMeasureSpec;
		if (MeasureSpec.GetMode(crossSpec) == global::Android.Views.MeasureSpecMode.Unspecified)
			throw new InvalidOperationException("RecyclerView must provide a finite cross-axis measure spec.");
		var density = Context.Resources.DisplayMetrics.Density;
		var cross = MeasureSpec.GetSize(crossSpec) / (double)density;
		var desired = contentLayout.Measure(axis, cross, force: true);
		SetMeasuredDimension(
			ResolveSize(RetainedContentLayout.ToPixels(desired.Width, density), widthMeasureSpec),
			ResolveSize(RetainedContentLayout.ToPixels(desired.Height, density), heightMeasureSpec));
	}

	protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
	{
		if (contentLayout is not null)
		{
			var density = Context.Resources.DisplayMetrics.Density;
			contentLayout.Arrange(handler.VirtualView.Orientation,
				new Size((right - left) / (double)density, (bottom - top) / (double)density));
		}
	}

	// List content doesn't take part in window insets / safe areas, matching MAUI's own exemption for
	// CollectionView items (MauiWindowInsetListener skips views under its internal IMauiRecyclerView unless
	// they set SafeAreaEdges explicitly). Without this, every MAUI layout in a cell gets an insets
	// listener, and recycling (attach, detach, resize) makes those layouts call requestApplyInsets from
	// OnLayout, which re-dispatches insets through the whole window on every scroll step.
	bool passWindowInsets;

#pragma warning disable CS0618, CS0672 // requestApplyInsets() is implemented as requestFitSystemWindows() up the parent chain
	public override void RequestFitSystemWindows()
	{
		if (passWindowInsets)
			base.RequestFitSystemWindows();
	}
#pragma warning restore CS0618, CS0672

	public override global::Android.Views.WindowInsets DispatchApplyWindowInsets(global::Android.Views.WindowInsets insets)
		=> passWindowInsets ? base.DispatchApplyWindowInsets(insets) : insets;

	static bool HasExplicitSafeAreaEdges(IView view)
	{
		if (view is Microsoft.Maui.Controls.BindableObject bindable
			&& (bindable.IsSet(Microsoft.Maui.Controls.Layout.SafeAreaEdgesProperty)
				|| bindable.IsSet(Microsoft.Maui.Controls.ContentView.SafeAreaEdgesProperty)
				|| bindable.IsSet(Microsoft.Maui.Controls.Border.SafeAreaEdgesProperty)
				|| bindable.IsSet(Microsoft.Maui.Controls.ScrollView.SafeAreaEdgesProperty)))
			return true;

		if (view is IVisualTreeElement element)
		{
			foreach (var child in element.GetVisualChildren())
			{
				if (child is IView childView && HasExplicitSafeAreaEdges(childView))
					return true;
			}
		}

		return false;
	}

	public void UpdatePosition(IPositionInfo positionInfo)
	{
        if (VirtualView is IPositionInfo viewWithPositionInfo)
			viewWithPositionInfo.Update(positionInfo);
    }

    public void SetupView(IView view)
	{
		ClearView();
		VirtualView = view;
		passWindowInsets = HasExplicitSafeAreaEdges(view);
		NativeView = view.ToPlatform(MauiContext);
		NativeView.RemoveFromParent();
		AddView(NativeView);
		var weakSelf = new WeakReference<RvViewContainer>(this);
		contentLayout = new RetainedContentLayout(view, handler.DispatchContentLayout, _ =>
		{
			if (weakSelf.TryGetTarget(out var container) && container.handler.IsConnected)
				container.handler.Publish(() => container.handler.PreserveAnchorDuring(container.RequestLayout));
		}, () => weakSelf.TryGetTarget(out var container)
			&& container.retainedView.TryGetTarget(out var content) && content.Position is not null);
    }

	public void ClearView()
	{
		contentLayout?.Dispose();
		contentLayout = null;
		if (NativeView is not null)
			RemoveView(NativeView);
		NativeView = null;
		VirtualView = null;
	}
}