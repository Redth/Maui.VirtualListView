using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WStackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WVisibility = Microsoft.UI.Xaml.Visibility;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : ViewHandler<IVirtualListView, WGrid>
{
	ItemsRepeaterScrollHost itemsRepeaterScrollHost;
	ScrollViewer scrollViewer;
	ItemsRepeater itemsRepeater;
	IrSource irSource;
	IrElementFactory elementFactory;
	WStackLayout layout;
	WGrid rootLayout;
	WFrameworkElement emptyView;

	Orientation NativeOrientation =>
		VirtualView.Orientation switch
		{
			ListOrientation.Vertical => Orientation.Vertical,
			ListOrientation.Horizontal => Orientation.Horizontal,
			_ => Orientation.Vertical
		};

	protected override WGrid CreatePlatformView()
	{
		rootLayout = new WGrid();
		itemsRepeaterScrollHost = new ItemsRepeaterScrollHost();
		scrollViewer = new ScrollViewer();
		itemsRepeater = new ItemsRepeater();

		layout = new WStackLayout { Orientation = NativeOrientation };
		itemsRepeater.Layout = layout;

		scrollViewer.Content = itemsRepeater;
		itemsRepeaterScrollHost.ScrollViewer = scrollViewer;

		itemsRepeaterScrollHost.Loaded += ItemsRepeaterScrollHost_Loaded;

		rootLayout.Children.Add(itemsRepeaterScrollHost);

		return rootLayout;
	}

	private void ItemsRepeaterScrollHost_Loaded(object sender, UI.Xaml.RoutedEventArgs e)
	{
		itemsRepeaterScrollHost.Loaded -= ItemsRepeaterScrollHost_Loaded;

		scrollViewer?.RegisterPropertyChangedCallback(ScrollViewer.VerticalOffsetProperty, (o, dp) =>
			VirtualView.Scrolled(new ScrolledEventArgs(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset)));
	}

	protected override void ConnectHandler(WGrid nativeView)
	{
		base.ConnectHandler(nativeView);

		PositionalViewSelector = new PositionalViewSelector(VirtualView);
		elementFactory = new IrElementFactory(MauiContext, PositionalViewSelector);
		itemsRepeater.ItemTemplate = elementFactory;

		irSource = new IrSource(MauiContext, PositionalViewSelector, VirtualView);

		itemsRepeater.ItemsSource = irSource;
	}

	protected override void DisconnectHandler(WGrid nativeView)
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
		UpdateEmptyViewVisibility();
	}

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

	public static void MapSelectItems(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
	{
		if (parameter is ItemPosition[] items && items != null && items.Length > 0)
		{
			UpdateSelection(handler, items, true);
		}
	}

	public static void MapDeselectItems(VirtualListViewHandler handler, IVirtualListView virtualListView, object? parameter)
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

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
	}


	public static void MapEmptyView(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateEmptyView();
	}

	void UpdateEmptyViewVisibility()
	{
		if (emptyView is not null)
		{
			var visibility = ShouldShowEmptyView ? WVisibility.Visible : WVisibility.Collapsed;
			emptyView.Visibility = visibility;
		}
	}

	void UpdateEmptyView()
	{
		if (emptyView is not null)
			rootLayout.Children.Remove(emptyView);

		emptyView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

		if (emptyView is not null)
			rootLayout.Children.Add(emptyView);

		UpdateEmptyViewVisibility();
	}
}
