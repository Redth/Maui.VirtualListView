using System;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler
	{
		public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(VirtualListViewHandler.ViewMapper)
		{
			[nameof(IVirtualListView.Adapter)] = MapAdapter,
			[nameof(IVirtualListView.HeaderTemplate)] = MapHeaderTemplate,
			[nameof(IVirtualListView.FooterTemplate)] = MapFooterTemplate,
			[nameof(IVirtualListView.SectionHeaderTemplate)] = MapSectionHeaderTemplate,
			[nameof(IVirtualListView.SectionFooterTemplate)] = MapSectionFooterTemplate,
			[nameof(IVirtualListView.ItemTemplate)] = MapItemTemplate,
		};

		public VirtualListViewHandler() : base(VirtualListViewMapper)
		{

		}

		public VirtualListViewHandler(PropertyMapper mapper = null) : base(mapper ?? VirtualListViewMapper)
		{

		}
	}
}