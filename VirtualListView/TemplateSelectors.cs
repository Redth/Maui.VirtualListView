using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	public abstract class ItemTemplateSelector : IReplaceableView
	{
		public abstract IReplacableView SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex);
	}

	public abstract class SectionTemplateSelector : IReplaceableView
	{
		public abstract IReplacableView SelectGroupTemplate(IVirtualListViewAdapter adapter, int sectionIndex);
	}
}
