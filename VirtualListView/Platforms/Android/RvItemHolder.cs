using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.HotReload;

namespace Microsoft.Maui
{
	internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public readonly ViewGroup NativeView;
		public readonly ReplaceableWrapperView WrapperView;
		public PositionInfo PositionInfo { get; set; }

		public RvItemHolder(ViewGroup nativeView, ReplaceableWrapperView wrapperView)
			: base(nativeView)
		{
			NativeView = nativeView;
			WrapperView = wrapperView;
		}
	}
}