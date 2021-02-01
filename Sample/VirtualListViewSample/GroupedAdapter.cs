using System.Collections.Generic;
using System.Linq;
using Xamarin.CommunityToolkit.UI.Views;

namespace VirtualListViewSample
{
	public class GroupedAdapter<TGroup, TItem> : IVirtualListViewAdapter
		where TGroup : IList<TItem> 
	{
		
		public List<TGroup> Groups { get; set; } = new List<TGroup>();

		public int Sections => Groups.Count;

		public object Item(int sectionIndex, int itemIndex)
			=> Groups[sectionIndex][itemIndex];

		public int ItemsForSection(int sectionIndex)
 			=> Groups[sectionIndex].Count;

		public object Section(int sectionIndex)
			=> Groups[sectionIndex];
	}

}
