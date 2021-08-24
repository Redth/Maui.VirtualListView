

namespace Microsoft.Maui
{
	public partial class VirtualListViewStackLayout : VirtualListViewLayout
	{
		public VirtualListViewStackLayout()
		{
			Orientation = ListOrientation.Vertical;
		}

		public VirtualListViewStackLayout(ListOrientation orientation)
		{
			Orientation = orientation;
		}

		public ListOrientation Orientation { get; private set; }

#if ANDROID
		public override AndroidX.RecyclerView.Widget.RecyclerView.LayoutManager CreateNativeLayout()
			=> new AndroidX.RecyclerView.Widget.LinearLayoutManager(
				null,
				Orientation == ListOrientation.Vertical ? AndroidX.RecyclerView.Widget.LinearLayoutManager.Vertical : AndroidX.RecyclerView.Widget.LinearLayoutManager.Horizontal,
				false);
#elif IOS || MACCATALYST
		public override UIKit.UICollectionViewLayout CreateNativeLayout()
			=> new CvLayout(Orientation)
			{
				EstimatedItemSize = UIKit.UICollectionViewFlowLayout.AutomaticSize,
				SectionInset = UIKit.UIEdgeInsets.Zero,
				MinimumInteritemSpacing = 0f,
				MinimumLineSpacing = 0f
			};
#endif
	}
}
