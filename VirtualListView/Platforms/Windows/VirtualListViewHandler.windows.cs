using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;
using WStackLayout = Microsoft.UI.Xaml.Controls.StackLayout;

namespace Microsoft.Maui
{
	public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, ItemsRepeaterScrollHost>
	{
		ItemsRepeaterScrollHost itemsRepeaterScrollHost;
		ScrollViewer scrollViewer;
		ItemsRepeater itemsRepeater;
		IrSource irSource;
		IrElementFactory elementFactory;
		WStackLayout layout;

		internal PositionalViewSelector PositionalViewSelector { get; private set; }

		Orientation NativeOrientation =>
			VirtualView.Orientation switch
			{
				ListOrientation.Vertical => Orientation.Vertical,
				ListOrientation.Horizontal => Orientation.Horizontal,
				_ => Orientation.Vertical
			};

		protected override ItemsRepeaterScrollHost CreatePlatformView()
		{
			itemsRepeaterScrollHost = new ItemsRepeaterScrollHost();
			scrollViewer = new ScrollViewer();
			itemsRepeater = new ItemsRepeater();

			layout = new WStackLayout { Orientation = NativeOrientation };
			itemsRepeater.Layout = layout;

			scrollViewer.Content = itemsRepeater;
			itemsRepeaterScrollHost.ScrollViewer = scrollViewer;

			return itemsRepeaterScrollHost;
		}

		protected override void ConnectHandler(ItemsRepeaterScrollHost nativeView)
		{
			base.ConnectHandler(nativeView);

			PositionalViewSelector = new PositionalViewSelector(VirtualView);
			elementFactory = new IrElementFactory(MauiContext, PositionalViewSelector);
			itemsRepeater.ItemTemplate = elementFactory;

			irSource = new IrSource(MauiContext, PositionalViewSelector, VirtualView);

			itemsRepeater.ItemsSource = irSource;
		}

		protected override void DisconnectHandler(ItemsRepeaterScrollHost nativeView)
		{
			itemsRepeater.ItemTemplate = null;
			elementFactory.Dispose();
			elementFactory = null;

			itemsRepeater.ItemsSource = null;
			irSource = null;

			base.DisconnectHandler(nativeView);
		}

		public void InvalidateData()
		{
			irSource?.Reset();
		}

		public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
			=> handler?.InvalidateData();

		public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{ }

		public static void MapInvalidateData(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
			=> handler?.InvalidateData();

		public static void MapSetSelected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items && items != null && items.Length > 0)
			{
				UpdateSelection(handler, items, true);
			}
		}

		public static void MapSetDeselected(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
		{
			if (parameter is ItemPosition[] items && items != null && items.Length > 0)
			{
				UpdateSelection(handler, items, false);
			}
		}

		static void UpdateSelection(VirtualListViewHandler handler, ItemPosition[] itemPositions, bool selected)
		{
			foreach (var itemPosition in itemPositions)
			{
				var position = handler.PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

				var elem = handler.itemsRepeater.TryGetElement(position);
				
				if (elem is IrElementContainer contentControl)
				{
					contentControl.PositionInfo.IsSelected = selected;

					if (contentControl?.VirtualView is IPositionInfo viewPositionInfo)
						viewPositionInfo.IsSelected = selected;
				}
			}
		}

		public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
		{
			handler.layout.Orientation = handler.NativeOrientation;
			handler.InvalidateData();
		}
	}
}
