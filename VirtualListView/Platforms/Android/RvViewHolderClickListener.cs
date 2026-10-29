using AView = global::Android.Views.View;
namespace Microsoft.Maui;

internal partial class RvAdapter
{
	internal class RvViewHolderClickListener : Java.Lang.Object, AView.IOnClickListener
	{
		public RvViewHolderClickListener(RvItemHolder viewHolder, Action<RvItemHolder> clickHandler)
		{
			ViewHolder = new WeakReference<RvItemHolder>(viewHolder);
			ClickHandler = clickHandler;
		}

		public WeakReference<RvItemHolder> ViewHolder { get; }

		public Action<RvItemHolder> ClickHandler { get; }

		public void OnClick(AView v)
		{
			if (ViewHolder.TryGetTarget(out var holder) && holder.PositionInfo?.Kind == PositionKind.Item)
				ClickHandler?.Invoke(holder);
		}
	}
}