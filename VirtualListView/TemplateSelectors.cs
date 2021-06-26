using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	public abstract class ItemTemplateSelector : IViewTemplate
	{
		public IView CreateView(PositionInfo positionInfo) => throw new InvalidCastException();

		public abstract IViewTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex);
	}

	public abstract class SectionTemplateSelector : IViewTemplate
	{
		public IView CreateView(PositionInfo positionInfo) => throw new InvalidCastException();

		public abstract IViewTemplate SelectGroupTemplate(IVirtualListViewAdapter adapter, int sectionIndex);
	}
}
