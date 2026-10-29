using AView = global::Android.Views.View;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui;

sealed class RvViewContainer : global::Android.Widget.FrameLayout
{
	readonly VirtualListViewHandler handler;
	RetainedContentLayout contentLayout;

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

	public void UpdatePosition(IPositionInfo positionInfo)
	{
        if (VirtualView is IPositionInfo viewWithPositionInfo)
			viewWithPositionInfo.Update(positionInfo);
    }

    public void SetupView(IView view)
	{
		ClearView();
		VirtualView = view;
		NativeView = view.ToPlatform(MauiContext);
		NativeView.RemoveFromParent();
		AddView(NativeView);
		var weakSelf = new WeakReference<RvViewContainer>(this);
		contentLayout = new RetainedContentLayout(view, handler.DispatchContentLayout, _ =>
		{
			if (weakSelf.TryGetTarget(out var container) && container.handler.IsConnected)
				container.handler.Publish(() => container.handler.PreserveAnchorDuring(container.RequestLayout));
		});
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