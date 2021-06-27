using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.HotReload;

namespace Microsoft.Maui
{
	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public ViewGroup NativeView { get; }
		public ReplaceableWrapperView WrapperView { get; private set; }
		public IView View { get; private set; }
		public IViewTemplate Template { get; }
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(ViewGroup nativeView, ReplaceableWrapperView wrapperView, IViewTemplate template)
			: base(nativeView)
		{
			Template = template;
			NativeView = nativeView;
			WrapperView = wrapperView;
		}

		public void Update(PositionInfo positionInfo)
		{
			if (View == null)
			{
				View = Template.CreateView(positionInfo) as IView;
				NativeView.AddView(View?.ToNative(View?.Handler?.MauiContext));
			}
			else
			{
				// Already created, let's recycle it
				View = Template.CreateView(positionInfo);
				WrapperView.ReplaceView(View);
			}

			if (WrapperView is IPositionInfo viewPositionInfo)
				viewPositionInfo.SetPositionInfo(positionInfo);
		}
	}
}