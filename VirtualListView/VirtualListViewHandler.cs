using System;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler
	{
		public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(ViewHandler.ViewMapper)
		{
			[nameof(IVirtualListView.Adapter)] = MapAdapter,
			[nameof(IVirtualListView.HeaderTemplate)] = MapHeader,
			[nameof(IVirtualListView.FooterTemplate)] = MapFooter,
			[nameof(IVirtualListView.SectionHeaderTemplate)] = MapSectionHeaderTemplate,
			[nameof(IVirtualListView.SectionFooterTemplate)] = MapSectionFooterTemplate,
			[nameof(IVirtualListView.ItemTemplate)] = MapItemTemplate,
		};

		public VirtualListViewHandler() : base(VirtualListViewMapper)
		{

		}

		public VirtualListViewHandler(PropertyMapper? mapper = null) : base(mapper ?? VirtualListViewMapper)
		{

		}
	}
}