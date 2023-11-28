using AView = Android.Views.View;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui;

sealed class RvViewContainer : Android.Widget.FrameLayout
{
	public RvViewContainer(IMauiContext context)
		: base(context.Context ?? throw new ArgumentNullException($"{nameof(context.Context)}"))
	{
		MauiContext = context;
		Id = AView.GenerateViewId();
	}

	public readonly IMauiContext MauiContext;

	public IView VirtualView { get; private set; }

	public AView NativeView { get; private set; }

	public bool NeedsView => VirtualView is null || VirtualView.Handler is null || NativeView is null;

	public void UpdatePosition(IPositionInfo positionInfo)
	{
        if (VirtualView is IPositionInfo viewWithPositionInfo)
			viewWithPositionInfo.Update(positionInfo);
    }

    public void SetupView(IView view)
	{
		if (NativeView is null)
			NativeView = view.ToPlatform(MauiContext);

		if (VirtualView is null)
		{
			VirtualView = view;
			AddView(NativeView);
		}
    }
}