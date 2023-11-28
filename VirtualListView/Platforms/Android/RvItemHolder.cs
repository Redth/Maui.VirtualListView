using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui;

internal class RvItemHolder : RecyclerView.ViewHolder
{
	public RvViewContainer ViewContainer { get; private set; }
	public PositionInfo PositionInfo { get; private set; }

	public RvItemHolder(IMauiContext mauiContext, ListOrientation orientation)
		: base(new RvViewContainer(mauiContext)
		{
			LayoutParameters = new RecyclerView.LayoutParams(
				orientation == ListOrientation.Vertical ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
				orientation == ListOrientation.Vertical ? ViewGroup.LayoutParams.WrapContent : ViewGroup.LayoutParams.MatchParent)
		})
	{
		ViewContainer = ItemView as RvViewContainer;
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
		PositionInfo = positionInfo;
		ViewContainer.UpdatePosition(positionInfo);
	}

	//public void Update(PositionInfo positionInfo, IView newView)
	//{
	//	PositionInfo = positionInfo;

	//	if (newView is IPositionInfo viewWithPositionInfo)
	//		viewWithPositionInfo.Update(PositionInfo);

 //       ViewContainer.SwapView(newView);
 //   }
}