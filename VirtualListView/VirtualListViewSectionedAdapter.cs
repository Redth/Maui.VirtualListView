using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

public class VirtualListViewSectionedAdapter<TSection, TItem> : VirtualListViewAdapterBase<TSection, TItem>
	where TSection : IList<TItem>
{
	public VirtualListViewSectionedAdapter(IList<TSection> sections)
		: base()
	{
		this.sections = sections;
	}

	readonly IList<TSection> sections;

	public override int Sections
		=> sections.Count;

	public override TSection Section(int sectionIndex)
		=> sections[sectionIndex];

	public override TItem Item(int sectionIndex, int itemIndex)
		=> sections[sectionIndex][itemIndex];

	public override int ItemsForSection(int sectionIndex)
		=> sections[sectionIndex].Count;
}
