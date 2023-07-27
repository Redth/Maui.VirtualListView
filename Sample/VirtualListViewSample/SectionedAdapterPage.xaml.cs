using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public partial class SectionedAdapterPage : ContentPage
{
	public SectionedAdapterPage()
	{
		InitializeComponent();

		Adapter = new SectionedAdapter(Sections);

		var rnd = new Random();

		for (int i = 0; i < 5; i++)
		{
			for (int j = 1; j <= rnd.Next(1, 7); j++)
			{
				Adapter.AddItem($"Section {i}", $"Item {j}");
			}
		}

		vlv.Adapter = Adapter;
	}


	public SectionedAdapter Adapter { get; set; }
	public ObservableCollection<Section> Sections = new();

	private void Button_Clicked(object sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(entrySection.Text) && !string.IsNullOrEmpty(entryItem.Text))
		{
			Adapter.AddItem(entrySection.Text, entryItem.Text);
			entryItem.Text = string.Empty;
		}
	}

	private void vlv_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
	{
		var item = e.NewSelection?.FirstOrDefault();

		if (item != null)
		{
			Adapter.RemoveItem(item.Value.SectionIndex, item.Value.ItemIndex);
		}

		(sender as VirtualListView).SelectedItems = null;
	}
}
