using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.HotReload;

namespace Microsoft.Maui
{
	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public ViewGroup NativeView { get; }
		public IHotReloadableView View { get; private set; }
		public IViewTemplate Template { get; }
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(ViewGroup nativeView, IViewTemplate template)
			: base(nativeView)
		{
			Template = template;
			NativeView = nativeView;
		}

		public void Update(PositionInfo positionInfo)
		{
			if (View == null)
			{
				View = Template.CreateView(positionInfo) as IHotReloadableView;
				NativeView.AddView(View?.ToNative(View?.Handler?.MauiContext));
			}
			else
			{
				// Already created, let's recycle it
				var newView = Template.CreateView(positionInfo);
				View.TransferState(newView);
				View.Reload();
			}

			if (View is IPositionInfo viewPositionInfo)
				viewPositionInfo.SetPositionInfo(positionInfo);
		}
	}
}