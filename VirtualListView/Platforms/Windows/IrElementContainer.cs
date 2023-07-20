using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.Maui
{
	

	internal class IrElementContainer : ContentControl
	{
		public IrElementContainer(IMauiContext context, string reuseId, PositionalViewSelector positionalViewSelector, object data)
			: base()
		{
			MauiContext = context;
			ReuseId = reuseId;
			PositionalViewSelector = positionalViewSelector;
			Data = data;

			if (positionalViewSelector.VirtualListView.Orientation == ListOrientation.Vertical)
			{
				HorizontalContentAlignment = UI.Xaml.HorizontalAlignment.Stretch;
				HorizontalAlignment = UI.Xaml.HorizontalAlignment.Stretch;
			}
			else
			{
				VerticalContentAlignment = UI.Xaml.VerticalAlignment.Stretch;
				VerticalAlignment = UI.Xaml.VerticalAlignment.Stretch;
			}
		}

		public object Data { get; private set; }

		public readonly string ReuseId;
		public readonly IMauiContext MauiContext;
		public readonly PositionalViewSelector PositionalViewSelector;

		public PositionInfo PositionInfo { get; private set; }

		bool isRecycled = false;

		internal bool IsRecycled
		{
			get => isRecycled;
			set
			{
				isRecycled = value;

			}
		}

		public IView VirtualView { get; private set; }

		public void Update(PositionInfo positionInfo, object data, IView newView)
		{
			PositionInfo = positionInfo;
			Data = data;

			SwapView(newView);
		}

		void SwapView(IView newView)
		{
			if (VirtualView == null || VirtualView.Handler == null || Content == null)
			{
				Content = newView.ToPlatform(MauiContext);
				VirtualView = newView;
			}
			else
			{
				var handler = VirtualView.Handler;
				newView.Handler = handler;
				handler.SetVirtualView(newView);
				VirtualView = newView;
			}
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);

			if (PositionInfo != null)
				PositionInfo.IsSelected = !PositionInfo.IsSelected;

			var itemPos = new ItemPosition(PositionInfo?.SectionIndex ?? 0, PositionInfo?.ItemIndex ?? 0);

			if (PositionInfo?.IsSelected ?? false)
				PositionalViewSelector?.VirtualListView?.SetSelected(itemPos);
			else
				PositionalViewSelector?.VirtualListView?.SetDeselected(itemPos);

			// Update only IsSelected on the view
			//if (View is IPositionInfo positionInfoView)
			//	positionInfoView.IsSelected = Data.position.IsSelected;
		}
	}
}
