﻿using Microsoft.Maui.Adapters;

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
		=> Items[sectionIndex].Count;

	public override Item GetItem(int sectionIndex, int itemIndex)
		=> Items[sectionIndex][itemIndex];

	public void AddItem(string sectionTitle, string itemName)
	{
		var section = Items.FirstOrDefault(s => s.Title == sectionTitle);

		if (section is null)
		{
			section = new Section { Title = sectionTitle };
			Items.Add(section);
		}

		section.Add(new Item { Name = itemName });
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

	public void UpdateItem(int sectionIndex, int itemIndex)
	{
        var section = Items.ElementAtOrDefault(sectionIndex);

        if (section is null)
            return;

		section[itemIndex].Name = $"{section[itemIndex].Name} Updated";

		InvalidateItems(new ItemPosition(sectionIndex, itemIndex));
    }
}