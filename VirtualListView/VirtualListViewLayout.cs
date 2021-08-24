

namespace Microsoft.Maui
{
	public class VirtualListViewStackLayout : VirtualListViewLayout
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
#elif WINDOWS
		public override UI.Xaml.Controls.VirtualizingLayout CreateNativeLayout()
			=> new UI.Xaml.Controls.StackLayout()
			{
				Orientation = this.Orientation == ListOrientation.Vertical 
					? UI.Xaml.Controls.Orientation.Vertical
					: UI.Xaml.Controls.Orientation.Horizontal
			};
#endif
	}

	public abstract class VirtualListViewLayout
	{
#if ANDROID
		public abstract AndroidX.RecyclerView.Widget.RecyclerView.LayoutManager CreateNativeLayout();
#elif IOS || MACCATALYST
		public abstract UIKit.UICollectionViewLayout CreateNativeLayout();
#elif WINDOWS
		public abstract UI.Xaml.Controls.VirtualizingLayout CreateNativeLayout();
#endif
	}
}
