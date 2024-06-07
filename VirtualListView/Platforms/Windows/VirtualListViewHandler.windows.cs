using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WStackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WVisibility = Microsoft.UI.Xaml.Visibility;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
using WRect = Windows.Foundation.Rect;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

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
			VirtualView.Scrolled(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset));
	}

	protected override void ConnectHandler(WGrid nativeView)
	{
		base.ConnectHandler(nativeView);

		PositionalViewSelector = new PositionalViewSelector(VirtualView);
		elementFactory = new IrElementFactory(MauiContext, PositionalViewSelector, this);
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

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		var elem = itemsRepeater.GetOrCreateElement(position);

		elem.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = animated });
	}

	public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler?.InvalidateData();

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{ }

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		var elem = itemsRepeater.TryGetElement(position);

		if (elem is IrElementContainer contentControl)
		{
			contentControl.PositionInfo.IsSelected = selected;

			if (contentControl?.VirtualView is IPositionInfo viewPositionInfo)
				viewPositionInfo.IsSelected = selected;
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

	void UpdateVerticalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		scrollViewer.VerticalScrollBarVisibility = scrollBarVisibility switch
		{
			ScrollBarVisibility.Default => WScrollBarVisibility.Auto,
			ScrollBarVisibility.Always => WScrollBarVisibility.Visible,
			ScrollBarVisibility.Never => WScrollBarVisibility.Hidden,
			_ => WScrollBarVisibility.Auto
		};
	}

	void UpdateHorizontalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		scrollViewer.HorizontalScrollBarVisibility = scrollBarVisibility switch
		{
			ScrollBarVisibility.Default => WScrollBarVisibility.Auto,
			ScrollBarVisibility.Always => WScrollBarVisibility.Visible,
			ScrollBarVisibility.Never => WScrollBarVisibility.Hidden,
			_ => WScrollBarVisibility.Auto
		};
	}

	public IReadOnlyList<IPositionInfo> FindVisiblePositions()
	{
		var positions = new List<PositionInfo>();

		// This gets us all the direct children of our repeater which should be IrElementContainers
		// The items will not all be visible, since virtualizing causes items to get setup out of view
		// before being displayed.  We need to check if they are actually visible.
		var childrenCount = VisualTreeHelper.GetChildrenCount(itemsRepeater);

		for (int i = 0; i < childrenCount; i++)
		{
			// See if the child is an IrElementContainer
			if (VisualTreeHelper.GetChild(itemsRepeater, i) is IrElementContainer irElementContainer)
			{
				// Is the item actually visible?
				if (VisibilityHitTest(irElementContainer, itemsRepeater))
					positions.Add(irElementContainer.PositionInfo);
			}
		}

		return positions;
	}

	static bool VisibilityHitTest(IrElementContainer element, FrameworkElement container)
	{
		var bounds = element.TransformToVisual(container).TransformBounds(new WRect(0.0, 0.0, element.ActualWidth, element.ActualHeight));

		return bounds.Left < container.ActualWidth
			&& bounds.Top < container.ActualHeight
			&& bounds.Right > 0
			&& bounds.Bottom > 0;
	}
}
