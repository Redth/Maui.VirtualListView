using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	public interface IItemTemplateSelector : IView
	{
		IView SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex);
	}

	public interface ISectionTemplateSelector : IView
	{
		// Type? return type of view to create instance with later, and 
		IView SelectGroupTemplate(IVirtualListViewAdapter adapter, int sectionIndex);
	}
}
