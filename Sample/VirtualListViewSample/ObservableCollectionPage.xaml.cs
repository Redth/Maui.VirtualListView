using Microsoft.Maui.Adapters;
using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public partial class ObservableCollectionPage : ContentPage
{
	public ObservableCollectionPage()
	{
		InitializeComponent();

		Adapter = new ObservableCollectionAdapter<string>(Items);

		for (int i = 0; i < 10; i++)
		{
			Items.Add($"Item: {i}");
		}

		vlv.Adapter = Adapter;
	}

	public ObservableCollectionAdapter<string> Adapter { get; set; }
	public ObservableCollection<string> Items = new();

	protected override void OnAppearing()
	{
		base.OnAppearing();

		Task.Delay(1000).ContinueWith(t =>
		{
			this.Dispatcher.Dispatch(() =>
			{
				Items.Add("Item 11");
				Items.Add("Item 12");
			});
		});
	}
	private void Button_Clicked(object sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(entryItem.Text))
		{
			Items.Add(entryItem.Text);

			entryItem.Text = string.Empty;
		}
	}

	private void vlv_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
	{
		var item = e.NewSelection?.FirstOrDefault();

		if (item != null)
		{
			Items.RemoveAt(item.Value.ItemIndex);
		}

		(sender as IVirtualListView).ClearSelection();
	}
}