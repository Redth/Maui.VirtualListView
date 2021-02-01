using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;

namespace Xamarin.CommunityToolkit.UI.Views
{
	public interface IVirtualListViewAdapter
	{
		int Sections { get; }

		object Section(int sectionIndex);

		int ItemsForSection(int sectionIndex);

		object Item(int sectionIndex, int itemIndex);
	}
}
