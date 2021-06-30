using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.HotReload;
using AFrameLayout = Android.Widget.FrameLayout;
namespace Microsoft.Maui
{
	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public RvViewContainer ViewContainer { get; private set; }
		public PositionInfo PositionInfo { get; private set; }

		public RvItemHolder(IMauiContext mauiContext)
			: base(new RvViewContainer(mauiContext)
			{
				//LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
			})
		{
			ViewContainer = ItemView as RvViewContainer;
		}

		public void Update(PositionInfo positionInfo, IView newView)
		{
			PositionInfo = positionInfo;

			ViewContainer.SwapView(newView);
		}
	}
}