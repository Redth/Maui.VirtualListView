namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler
	{
		public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(ViewHandler.ViewMapper)
		{
			[nameof(IVirtualListView.Adapter)] = MapAdapter,
			[nameof(IVirtualListView.Header)] = MapHeader,
			[nameof(IVirtualListView.Footer)] = MapFooter,
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