using System.Collections.Generic;
using Xamarin.CommunityToolkit.UI.Views;

namespace VirtualListViewSample
{
	public class ListAdapter<T> : IVirtualListViewAdapter
	{
		public List<T> Items { get; set; } = new List<T>();

		public int Sections => 1;

		public object Item(int sectionIndex, int itemIndex)
			=> Items[itemIndex];

		public int ItemsForSection(int sectionIndex)
			=> Items.Count;

		public object Section(int sectionIndex)
			=> null;
	}

}
