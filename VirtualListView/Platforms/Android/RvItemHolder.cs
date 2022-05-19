using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.HotReload;
using AFrameLayout = Android.Widget.FrameLayout;
namespace Microsoft.Maui
{
	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public RvViewContainer ViewContainer { get; private set; }
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(IMauiContext mauiContext)
			: base(new RvViewContainer(mauiContext))
		{
			ViewContainer = ItemView as RvViewContainer;
		}

		public void SwapView(IView view)
		{
			ViewContainer.SwapView(view);
		}

		public bool HasView
			=> ViewContainer.VirtualView != null;
	}
}