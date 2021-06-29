using System;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler
	{
		public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(VirtualListViewHandler.ViewMapper)
		{
			[nameof(IVirtualListView.Adapter)] = MapAdapter,
			[nameof(IVirtualListView.Header)] = MapHeader,
			[nameof(IVirtualListView.Footer)] = MapFooter,
			[nameof(IVirtualListView.ViewSelector)] = MapViewSelector
		};

		public VirtualListViewHandler() : base(VirtualListViewMapper)
		{

		}

		public VirtualListViewHandler(PropertyMapper mapper = null) : base(mapper ?? VirtualListViewMapper)
		{

		}
	}
}