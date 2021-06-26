using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Microsoft.Maui
{
    internal class RvItemHolder : RecyclerView.ViewHolder
	{
		public ViewGroup NativeView { get; }
		public IView View { get; private set; }
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
				View = Template.CreateView(positionInfo);
				NativeView.AddView(View?.ToNative(View?.Handler?.MauiContext));
			}
			else
            {
				// TODO: Swap view but keep handler
            }

			if (View is IPositionInfo viewPositionInfo)
				viewPositionInfo.SetPositionInfo(positionInfo);
		}
	}
}