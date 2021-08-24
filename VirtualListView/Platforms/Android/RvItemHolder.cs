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
			: base(new RvViewContainer(mauiContext))
		{
			ViewContainer = ItemView as RvViewContainer;
		}

		public bool NeedsView
			=> ViewContainer.VirtualView is null;

		public void CreateView(IView view)
			=> ViewContainer.CreateView(view);

		public void Update(PositionInfo positionInfo)
		{
			PositionInfo = positionInfo;
			if (ViewContainer.VirtualView is IPositionInfo positionInfoView)
				positionInfoView.SetPositionInfo(positionInfo);
		}
	}
}