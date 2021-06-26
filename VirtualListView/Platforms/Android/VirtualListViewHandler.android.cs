using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Handlers;
using System;

namespace Microsoft.Maui
{
    public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, RecyclerView>
	{
		RvAdapter adapter;
		RecyclerView recyclerView;
		LinearLayoutManager layoutManager;

		protected override RecyclerView CreateNativeView()
			=> recyclerView ??= new RecyclerView(Context);

		protected override void ConnectHandler(RecyclerView nativeView)
		{
			layoutManager = new LinearLayoutManager(Context);
			//layoutManager.Orientation = LinearLayoutManager.Horizontal;

			adapter = new RvAdapter(Context, this.VirtualView.Adapter, this);
			
			recyclerView.AddOnScrollListener(new RvScrollListener((rv, dx, dy) =>
			{
				var x = Context.FromPixels(dx);
				var y = Context.FromPixels(dy);
				// TODO: Proxy up to event

				// VirtualView?.RaiseScrolled((x, y));
			}));

            recyclerView.SetLayoutManager(layoutManager);
			recyclerView.SetAdapter(adapter);
			recyclerView.LayoutParameters = new ViewGroup.LayoutParams(
				ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
		}

		protected override void DisconnectHandler(RecyclerView nativeView)
		{
			recyclerView.ClearOnScrollListeners();
			recyclerView.SetAdapter(null);
			adapter.Dispose();
			adapter = null;
			layoutManager.Dispose();
			layoutManager = null;
		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.NativeView?.SwapAdapter(handler.adapter, true);

		public static void MapHeaderTemplate(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.adapter?.NotifyDataSetChanged();

		public static void MapFooterTemplate(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.adapter?.NotifyDataSetChanged();

		public static void MapSectionHeaderTemplate(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.adapter?.NotifyDataSetChanged();
		
		public static void MapSectionFooterTemplate(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.adapter?.NotifyDataSetChanged();

		public static void MapItemTemplate(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler.adapter?.NotifyDataSetChanged();

		class RvScrollListener : RecyclerView.OnScrollListener
		{
			public RvScrollListener(Action<RecyclerView, int, int> scrollHandler)
			{
				ScrollHandler = scrollHandler;
			}

			Action<RecyclerView, int, int> ScrollHandler { get; }

			public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
			{
				base.OnScrolled(recyclerView, dx, dy);

				ScrollHandler?.Invoke(recyclerView, dx, dy);
			}
		}
	}
}