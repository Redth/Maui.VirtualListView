using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace Xamarin.CommunityToolkit.UI.Views
{
	public abstract class AdapterItemDataTemplateSelector
	{
		public abstract DataTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex);
	}

	public abstract class AdapterSectionDataTemplateSelector
	{
		public abstract DataTemplate SelectGroupTemplate(IVirtualListViewAdapter adapter, int sectionIndex);
	}
}
