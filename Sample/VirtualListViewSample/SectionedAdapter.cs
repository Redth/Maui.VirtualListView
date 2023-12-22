using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public class SectionedAdapter : VirtualListViewAdapterBase<Section, Item>
{
	public SectionedAdapter(IList<Section> items) : base()
	{
		Items = items;
	}

	public readonly IList<Section> Items;

	public override Section GetSection(int sectionIndex)
		=> Items[sectionIndex];

	public override int GetNumberOfSections()
		=> Items.Count;

	public override int GetNumberOfItemsInSection(int sectionIndex)
	{
		var c = Items[sectionIndex].Count;
		Console.WriteLine($"GetNumberOfItemsInSection: section={sectionIndex}, count={c}");
		return c;
	}

	public override Item GetItem(int sectionIndex, int itemIndex)
		=> Items[sectionIndex][itemIndex];

	public ItemPosition AddItem(string sectionTitle, string itemName, double itemHeight = 40f)
	{
		Section section = null;
		var sectionIndex = -1;
		
		for (var s = 0; s < Items.Count; s++)
		{
			if (Items[s].Title == sectionTitle)
			{
				section = Items[s];
				sectionIndex = s;
				break;
			}
		}
		
		if (section is null)
		{
			section = new Section { Title = sectionTitle };
			Items.Add(section);
			sectionIndex = Items.Count - 1;
		}

		section.Add(new Item { Text = itemName, Height = itemHeight });
		InvalidateData();

		return new ItemPosition(sectionIndex, section.Count - 1);
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