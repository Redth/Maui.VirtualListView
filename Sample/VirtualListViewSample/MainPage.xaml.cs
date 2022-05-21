using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public partial class MainPage : ContentPage, ISafeAreaView
	{
		MainViewModel vm;

		public MainPage()
		{
			InitializeComponent();

			Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, false);

			vm = new MainViewModel();
			BindingContext = vm;

			Task.Delay(2000).ContinueWith(t =>
			{
				Dispatcher.Dispatch(() =>
				{
					vlv.SetSelected(new ItemPosition(0, 2), new ItemPosition(0, 4));
				});
			});
		}

		protected override void OnNavigatedTo(NavigatedToEventArgs args)
		{
			base.OnNavigatedTo(args);

			
			
		}

		void VirtualListView_SelectedItemsChanged(System.Object sender, SelectedItemsChangedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine($"Selected Items:"
				+ string.Join(", ", e.NewSelection.Select(s => $"{s.SectionIndex}:{s.ItemIndex}")));
		}
	}
}
