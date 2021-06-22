using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

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
