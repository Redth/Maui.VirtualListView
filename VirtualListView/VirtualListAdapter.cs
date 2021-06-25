using System.Collections.Generic;

namespace Microsoft.Maui
{
	public abstract class VirtualListAdapter : IVirtualListViewAdapter
	{
		public abstract int GetSections();

		int? sections = null;

		public int Sections
			=> sections ??= GetSections();

		public abstract object Item(int sectionIndex, int itemIndex);

		Dictionary<int, int> cachedSectionCount = new Dictionary<int, int>();

		public abstract int GetItemsForSection(int sectionIndex);

		public int ItemsForSection(int sectionIndex)
		{
			if (cachedSectionCount.ContainsKey(sectionIndex))
				return cachedSectionCount[sectionIndex];

			var i = GetItemsForSection(sectionIndex);
			cachedSectionCount.Add(sectionIndex, i);
			return i;
		}

		public abstract object Section(int sectionIndex);

		public void ReloadData()
		{
			sections = null;
			cachedSectionCount.Clear();
		}
	}
}
