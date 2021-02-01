using System;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace VirtualListViewSample
{
	public partial class MainPage : ContentPage
	{
		MainViewModel vm;

		public MainPage()
		{
			InitializeComponent();
			vm = new MainViewModel();
			BindingContext = vm;
		}

		void VirtualListView_SelectedItemsChanged(System.Object sender, SelectedItemsChangedEventArgs e)
		{
			Console.WriteLine($"Selected Items:");
			foreach (var s in e.NewSelection)
			{
				Console.WriteLine($"  -> {s.SectionIndex} ... {s.ItemIndex}");
			}
		}
	}
}
