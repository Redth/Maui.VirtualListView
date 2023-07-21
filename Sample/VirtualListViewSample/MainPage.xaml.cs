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
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			Navigation.PushAsync(new MusicLibraryPage());
		}

		private void Button_Clicked_1(object sender, EventArgs e)
		{
			Navigation.PushAsync(new ObservableCollectionPage());
		}

		private void Button_Clicked_2(object sender, EventArgs e)
		{
			Navigation.PushAsync(new SectionedAdapterPage());
		}
	}
}
