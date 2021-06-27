using Android.Content;

namespace Microsoft.Maui
{
	internal class ReplaceableWrapperView : WrapperView, IReplaceableView
	{
		public ReplaceableWrapperView(Context context) : base(context)
		{
		}

		public void ReplaceView(IView newView)
			=> ReplacedView = newView;

		public IView ReplacedView { get; private set; }
	}
}