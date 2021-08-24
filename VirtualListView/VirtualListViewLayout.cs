

namespace Microsoft.Maui
{
	public abstract partial class VirtualListViewLayout
	{
#if IOS || MACCATALYST
		public abstract UIKit.UICollectionViewLayout CreateNativeLayout();
#elif ANDROID
		public abstract AndroidX.RecyclerView.Widget.RecyclerView.LayoutManager CreateNativeLayout();
#endif
	}
}
