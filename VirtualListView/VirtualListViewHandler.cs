using Microsoft.Maui.Adapters;
using System;
using System.Collections.Generic;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler
	{
		public static PropertyMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(VirtualListViewHandler.ViewMapper)
		{
			[nameof(IVirtualListView.Adapter)] = MapAdapter,
			[nameof(IVirtualListView.Header)] = MapHeader,
			[nameof(IVirtualListView.Footer)] = MapFooter,
			[nameof(IVirtualListView.ViewSelector)] = MapViewSelector,
			[nameof(IVirtualListView.SelectionMode)] = MapSelectionMode,
			[nameof(IVirtualListView.Orientation)] = MapOrientation,
		};

		public static CommandMapper<IVirtualListView, VirtualListViewHandler> VirtualListViewCommandMapper = new(VirtualListViewHandler.ViewCommandMapper)
		{
			[nameof(IVirtualListView.SetSelected)] = MapSetSelected,
			[nameof(IVirtualListView.SetDeselected)] = MapSetDeselected
		};

		public VirtualListViewHandler() : base(VirtualListViewMapper, VirtualListViewCommandMapper)
		{

		}

		public VirtualListViewHandler(PropertyMapper mapper = null, CommandMapper commandMapper = null) : base(mapper ?? VirtualListViewMapper, commandMapper ?? VirtualListViewCommandMapper)
		{

		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{
			if (handler.currentAdapter != null)
				handler.currentAdapter.OnDataInvalidated -= handler.Adapter_OnDataInvalidated;

			if (virtualListView?.Adapter != null)
				virtualListView.Adapter.OnDataInvalidated += handler.Adapter_OnDataInvalidated;

			handler.currentAdapter = virtualListView.Adapter;
			handler?.InvalidateData();
		}

		IVirtualListViewAdapter currentAdapter = default;

		void Adapter_OnDataInvalidated(object sender, EventArgs e)
		{
			InvalidateData();
		}
	}
}