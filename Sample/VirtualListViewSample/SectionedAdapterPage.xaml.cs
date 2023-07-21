using Microsoft.Maui.Adapters;
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
	}
}

public class Section : List<string>
{
	public string Title { get; set; }
}

public class SectionedAdapter : VirtualListViewAdapterBase<Section, string>
{
	public SectionedAdapter(IList<Section> items) : base()
	{
		Items = items;
	}

	public readonly IList<Section> Items;

	public override Section Section(int sectionIndex)
		=> Items[sectionIndex];

	public override int Sections
		=> Items.Count;

	public override int ItemsForSection(int sectionIndex)
		=> Items[sectionIndex].Count;

	public override string Item(int sectionIndex, int itemIndex)
		=> Items[sectionIndex][itemIndex];

	public void AddItem(string sectionTitle, string itemName)
	{
		var section = Items.FirstOrDefault(s => s.Title == sectionTitle);

		if (section is null)
		{
			section = new Section { Title = sectionTitle };
			Items.Add(section);
		}

		section.Add(itemName);
		InvalidateData();
	}

	public void RemoveItem(int sectionIndex, int itemIndex)
	{
		var section = Items.ElementAtOrDefault(sectionIndex);

		if (section is null)
			return;

		section.RemoveAt(itemIndex);

		if (section.Count <= 0)
			Items.RemoveAt(sectionIndex);

		InvalidateData();
	}
}