using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Adapters;
using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public partial class BindableSelectedItemViewModel : ObservableObject
{
	public BindableSelectedItemViewModel(IDispatcher dispatcher)
	{
		Dispatcher = dispatcher;

		for (int i = 0; i < 10; i++)
		{
			Items.Add($"Item: {i}");
		}

		Adapter = new ObservableCollectionAdapter<string>(Items);
	}

	protected IDispatcher Dispatcher { get; }

	[ObservableProperty]
	ItemPosition? selectedItem;

	[ObservableProperty]
	ObservableCollectionAdapter<string> adapter;

	public ObservableCollection<string> Items = new();

	public void OnAppearing()
	{
		Task.Delay(1000).ContinueWith(t =>
		{
			Dispatcher.Dispatch(() =>
			{
				Items.Add("Item 11");
				Items.Add("Item 12");
			});
		});
	}
}

public partial class BindableSelectedItemPage : ContentPage
{
	public BindableSelectedItemPage()
	{
		InitializeComponent();

		ViewModel = new BindableSelectedItemViewModel(Dispatcher);

		BindingContext = ViewModel;
	}

	public readonly BindableSelectedItemViewModel ViewModel;

	private void Button_Clicked(object sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(entryItem.Text))
		{
			var index = ViewModel.Items.IndexOf(entryItem.Text);

			if (index == ViewModel.SelectedItem?.ItemIndex)
				ViewModel.SelectedItem = null;
			else if (index >= 0)
				ViewModel.SelectedItem = new ItemPosition(0, index);
		}
	}

	private void vlv_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
	{
		var selection = string.Join(", ", e.NewSelection.Select(i => i.ItemIndex));
		System.Diagnostics.Debug.WriteLine($"SelectedItemsChanged: {selection}");
	}
}