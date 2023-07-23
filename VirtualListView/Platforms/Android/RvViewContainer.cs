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

	public void SwapView(IView newView)
	{
		if (VirtualView == null || VirtualView.Handler == null || NativeView == null)
		{
			NativeView = newView.ToPlatform(MauiContext);
			VirtualView = newView;
			AddView(NativeView);
		}
		else
		{
			var handler = VirtualView.Handler;
			newView.Handler = handler;
			handler.SetVirtualView(newView);
			VirtualView = newView;
		}
	}
}