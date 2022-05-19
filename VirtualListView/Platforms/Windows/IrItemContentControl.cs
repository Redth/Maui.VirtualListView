using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.Maui.Controls
{
	public partial class IrItemContentControl : ContentControl
	{
		public IrItemContentControl() : base()
        {
        }
		
		public IView View { get; private set; }

		internal IrDataWrapper Data { get; private set; }

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Data = DataContext as IrDataWrapper;

			this.Loaded += IrItemContentControl_Loaded;
			this.Unloaded += IrItemContentControl_Unloaded;
			this.DataContextChanged += IrItemContentControl_DataContextChanged;

			Update();
		}

		private void IrItemContentControl_DataContextChanged(UI.Xaml.FrameworkElement sender, UI.Xaml.DataContextChangedEventArgs args)
		{
			if (args.NewValue is IrDataWrapper wrapper)
			{
				Data = wrapper;
				Update();
			}
		}

		void Update()
		{
			if (View == null)
			{
				View = Data.positionalViewSelector.ViewSelector.CreateView(Data.position, Data.data);

				var frameworkElement = View.ToPlatform(Data.context);

				Content = frameworkElement;
			}

			Data.positionalViewSelector?.ViewSelector?.RecycleView(Data.position, Data.data, View);
		}
		private void IrItemContentControl_Unloaded(object sender, UI.Xaml.RoutedEventArgs e)
		{
			Data.virtualListView.ViewSelector.ViewDetached(Data.position, View);
		}

		private void IrItemContentControl_Loaded(object sender, UI.Xaml.RoutedEventArgs e)
		{
			Data.virtualListView.ViewSelector.ViewAttached(Data.position, View);
		}

		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			base.OnTapped(e);

			if (Data?.position != null)
				Data.position.IsSelected = !Data.position.IsSelected;

			var itemPos = new ItemPosition(Data?.position?.SectionIndex ?? 0, Data?.position?.ItemIndex ?? 0);

			if (Data?.position?.IsSelected ?? false)
				Data?.virtualListView?.SetSelected(itemPos);
			else
				Data?.virtualListView?.SetDeselected(itemPos);

			// Update only IsSelected on the view
			//if (View is IPositionInfo positionInfoView)
			//	positionInfoView.IsSelected = Data.position.IsSelected;
		}
	}
}
