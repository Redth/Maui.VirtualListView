using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public partial class SectionedAdapterPage : ContentPage
{
	public SectionedAdapterPage()
	{
		InitializeComponent();

		Adapter = new SectionedAdapter(Sections);
		SetContent(5, 6);
		vlv.Adapter = Adapter;
	}


	public SectionedAdapter Adapter { get; set; }
	public ObservableCollection<Section> Sections = new();
	int refreshCount;

	void RequestRefresh_Clicked(object sender, EventArgs e)
		=> vlv.RequestRefresh();

	void FullContent_Clicked(object sender, EventArgs e) => SetContent(5, 6);
	void ShortContent_Clicked(object sender, EventArgs e) => SetContent(1, 1);
	void EmptyContent_Clicked(object sender, EventArgs e) => SetContent(0, 0);

	void ScrollEnd_Clicked(object sender, EventArgs e)
	{
		var section = Sections.Count - 1;
		if (section >= 0 && Sections[section].Count > 0)
			vlv.ScrollToItem(new Microsoft.Maui.ItemPosition(section, Sections[section].Count - 1), false);
	}

	void Horizontal_Toggled(object sender, ToggledEventArgs e)
		=> vlv.Orientation = e.Value ? Microsoft.Maui.ListOrientation.Horizontal : Microsoft.Maui.ListOrientation.Vertical;

	void SetContent(int sectionCount, int itemCount)
	{
		vlv.ClearSelectedItems();
		Sections.Clear();
		for (var i = 0; i < sectionCount; i++)
		{
			var section = new Section { Title = $"Section {i}" };
			for (var j = 1; j <= itemCount; j++)
				section.Add($"Item {j}");
			Sections.Add(section);
		}
		Adapter.InvalidateData();
	}

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
		if (e.NewSelection.Any())
		{
			var item = e.NewSelection.First();

			Adapter.RemoveItem(item.SectionIndex, item.ItemIndex);
			vlv.ClearSelectedItems();
		}
	
	}

	private async void Vlv_OnOnRefresh(object sender, RefreshEventArgs e)
	{
		var request = ++refreshCount;
		refreshStatus.Text = $"Refreshing #{request}";
		try
		{
			await Task.Delay(3000);
		}
		finally
		{
			e.Complete();
			if (request == refreshCount)
				refreshStatus.Text = $"Finished #{request}";
		}
	}
}
