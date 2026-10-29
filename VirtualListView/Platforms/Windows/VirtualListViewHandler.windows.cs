using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WStackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WVisibility = Microsoft.UI.Xaml.Visibility;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
using WScrollMode = Microsoft.UI.Xaml.Controls.ScrollMode;
using WRect = Windows.Foundation.Rect;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;

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
	WGrid contentLayout;
	RefreshContainer refreshContainer;
	WFrameworkElement emptyView;
	Action afterLayout;

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
		contentLayout = new WGrid();
		refreshContainer = new RefreshContainer
		{
			HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
			VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
			PullDirection = NativeOrientation == Orientation.Vertical
				? RefreshPullDirection.TopToBottom : RefreshPullDirection.LeftToRight
		};
		itemsRepeaterScrollHost = new ItemsRepeaterScrollHost();
		scrollViewer = new ScrollViewer();
		itemsRepeater = new ItemsRepeater();

		layout = new WStackLayout { Orientation = NativeOrientation };
		itemsRepeater.Layout = layout;

		scrollViewer.Content = itemsRepeater;
		itemsRepeaterScrollHost.ScrollViewer = scrollViewer;

		contentLayout.Children.Add(itemsRepeaterScrollHost);
		rootLayout.Children.Add(contentLayout);

		return rootLayout;
	}

	void ScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
	{
		if (IsConnected)
		{
			UpdateVisibleElements();
			VirtualView.Scrolled(scrollViewer.HorizontalOffset, scrollViewer.VerticalOffset);
		}
	}

	void ItemsRepeaterLayoutUpdated(object sender, object e)
	{
		UpdateVisibleElements();
		var completion = afterLayout;
		afterLayout = null;
		completion?.Invoke();
	}

	void UpdateVisibleElements()
	{
		if (IsConnected && elementFactory is not null)
			foreach (var element in elementFactory.ActiveElements.ToArray())
				element.ContentOwner.SetVisible(VisibilityHitTest(element, scrollViewer));
	}

	protected override void ConnectHandler(WGrid nativeView)
	{
		if (IsConnected)
			return;
		base.ConnectHandler(nativeView);

		PositionalViewSelector = new PositionalViewSelector(VirtualView);
		elementFactory = new IrElementFactory(MauiContext, PositionalViewSelector, this);
		itemsRepeater.ItemTemplate = elementFactory;

		irSource = new IrSource(MauiContext, PositionalViewSelector, VirtualView);

		itemsRepeater.ItemsSource = irSource;
		scrollViewer.ViewChanged += ScrollViewerViewChanged;
		scrollViewer.DirectManipulationStarted += ScrollManipulationStarted;
		scrollViewer.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(ScrollPointerWheelChanged), true);
		scrollViewer.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ScrollPointerWheelChanged), true);
		scrollViewer.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(ScrollKeyDown), true);
		itemsRepeater.LayoutUpdated += ItemsRepeaterLayoutUpdated;
		refreshContainer.RefreshRequested += NativeRefreshRequested;
		ConnectLifetime();
		UpdateRefreshEnabled();
	}

	void PlatformRequestRefresh()
	{
		refreshContainer.ApplyTemplate();
		rootLayout.UpdateLayout();
		if (refreshContainer.Visualizer is null)
			throw new InvalidOperationException("The connected refresh visualizer is unavailable.");
		refreshContainer.RequestRefresh();
	}

	void NativeRefreshRequested(RefreshContainer sender, RefreshRequestedEventArgs args)
	{
		var completion = args.GetDeferral();
		if (!IsConnected || !VirtualView.IsRefreshEnabled)
		{
			completion.Complete();
			return;
		}
		BeginRefresh(static () => { }, completion.Complete);
	}

	protected override void DisconnectHandler(WGrid nativeView)
	{
		DisconnectLifetime();
		refreshContainer.RefreshRequested -= NativeRefreshRequested;
		afterLayout = null;
		scrollViewer.ViewChanged -= ScrollViewerViewChanged;
		scrollViewer.DirectManipulationStarted -= ScrollManipulationStarted;
		scrollViewer.RemoveHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(ScrollPointerWheelChanged));
		scrollViewer.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(ScrollPointerWheelChanged));
		scrollViewer.RemoveHandler(UIElement.KeyDownEvent, new KeyEventHandler(ScrollKeyDown));
		itemsRepeater.LayoutUpdated -= ItemsRepeaterLayoutUpdated;
		itemsRepeater.ItemsSource = null;
		itemsRepeater.ItemTemplate = null;
		elementFactory?.Dispose();
		elementFactory = null;

		irSource = null;
		if (emptyView is not null)
			contentLayout.Children.Remove(emptyView);
		emptyView = null;

		base.DisconnectHandler(nativeView);
	}

	void PlatformInvalidateData()
	{
		irSource?.Reset();
		UpdateEmptyViewVisibility();
	}

	void ScrollManipulationStarted(object sender, object e) => RecordScrollIntent();
	void ScrollPointerWheelChanged(object sender, PointerRoutedEventArgs e) => RecordScrollIntent();
	void ScrollKeyDown(object sender, KeyRoutedEventArgs e) => RecordScrollIntent();

	void PlatformApplyUpdates(KeyedUpdatePlan plan)
	{
		irSource.Apply(plan);
		elementFactory.Rebind(plan);
		UpdateEmptyViewVisibility();
	}

	(int Index, double Offset) PlatformVisibleAnchor()
	{
		var vertical = VirtualView.Orientation == ListOrientation.Vertical;
		foreach (var container in elementFactory.ActiveElements
			.Where(element => element.PositionInfo?.Kind == PositionKind.Item && VisibilityHitTest(element, scrollViewer))
			.OrderBy(element => element.PositionInfo.Position))
		{
			var point = container.TransformToVisual(scrollViewer).TransformPoint(new global::Windows.Foundation.Point());
			return (container.PositionInfo.Position, vertical ? point.Y : point.X);
		}
		return (-1, 0);
	}

	void PlatformAfterLayout(Action action)
	{
		afterLayout = action;
		itemsRepeater.InvalidateMeasure();
	}

	bool PlatformRestoreAnchor(int index, double offset)
	{
		var element = itemsRepeater.GetOrCreateElement(index);
		itemsRepeater.UpdateLayout();
		var point = element.TransformToVisual(scrollViewer).TransformPoint(new global::Windows.Foundation.Point());
		if (VirtualView.Orientation == ListOrientation.Vertical)
		{
			var target = Math.Clamp(scrollViewer.VerticalOffset + point.Y - offset, 0, scrollViewer.ScrollableHeight);
			if (Math.Abs(scrollViewer.VerticalOffset - target) < 0.5)
				return true;
			scrollViewer.ChangeView(null, target, null, true);
		}
		else
		{
			var target = Math.Clamp(scrollViewer.HorizontalOffset + point.X - offset, 0, scrollViewer.ScrollableWidth);
			if (Math.Abs(scrollViewer.HorizontalOffset - target) < 0.5)
				return true;
			scrollViewer.ChangeView(target, null, null, true);
		}
		return false;
	}

	void PlatformResetTemplates() => elementFactory?.ResetTemplates();
	internal double ContentCrossSize => VirtualView.Orientation == ListOrientation.Vertical
		? Math.Max(0, scrollViewer.ViewportWidth) : Math.Max(0, scrollViewer.ViewportHeight);

	void PlatformInvalidateContentLayout(PositionInfo position)
	{
		if (itemsRepeater.TryGetElement(position.Position) is IrElementContainer container)
			container.InvalidateMeasure();
	}

	void PlatformScrollToItem(ItemPosition itemPosition, bool animated)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		if (position < 0)
			return;

		var elem = itemsRepeater.GetOrCreateElement(position);

		elem.StartBringIntoView(new BringIntoViewOptions() { AnimationDesired = animated });
	}

	public static void MapSelectionMode(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> MapSelectedItems(handler, virtualListView);

	void PlatformUpdateItemSelection(ItemPosition itemPosition, bool selected)
	{
		var position = PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex);

		if (position < 0)
			return;

		var elem = itemsRepeater.TryGetElement(position);

		if (elem is IrElementContainer contentControl && contentControl.PositionInfo is not null)
		{
			contentControl.PositionInfo.IsSelected = selected;

			if (contentControl?.VirtualView is IPositionInfo viewPositionInfo)
				viewPositionInfo.IsSelected = selected;
		}
	}

	public static void MapOrientation(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.Publish(() =>
		{
			handler.layout.Orientation = handler.NativeOrientation;
			var vertical = virtualListView.Orientation == ListOrientation.Vertical;
			handler.refreshContainer.PullDirection = vertical
				? RefreshPullDirection.TopToBottom : RefreshPullDirection.LeftToRight;
			handler.scrollViewer.HorizontalScrollMode = vertical ? WScrollMode.Disabled : WScrollMode.Enabled;
			handler.scrollViewer.VerticalScrollMode = vertical ? WScrollMode.Enabled : WScrollMode.Disabled;
			handler.UpdateHorizontalScrollbarVisibility(virtualListView.HorizontalScrollbarVisibility);
			handler.UpdateVerticalScrollbarVisibility(virtualListView.VerticalScrollbarVisibility);
			if (handler.elementFactory is not null)
				foreach (var container in handler.elementFactory.ActiveElements)
					container.UpdateOrientation();
			handler.itemsRepeater.InvalidateMeasure();
		});
	}

	public static void MapRefreshAccentColor(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler.Publish(handler.UpdateRefreshAccentColor);

	void UpdateRefreshAccentColor()
	{
		if (refreshContainer.Visualizer is RefreshVisualizer visualizer)
		{
			if (VirtualView.RefreshAccentColor is Color color)
				visualizer.Foreground = color.ToPlatform();
			else
				visualizer.ClearValue(Control.ForegroundProperty);
		}
	}

	public static void MapIsRefreshEnabled(VirtualListViewHandler handler, IVirtualListView virtualListView)
		=> handler.Publish(handler.UpdateRefreshEnabled);

	void UpdateRefreshEnabled()
	{
		var enabled = VirtualView.IsRefreshEnabled;
		if (!enabled)
			EndRefresh();
		if (enabled == ReferenceEquals(refreshContainer.Content, contentLayout))
			return;

		PreserveAnchorDuring(() =>
		{
			if (enabled)
			{
				rootLayout.Children.Remove(contentLayout);
				refreshContainer.Content = contentLayout;
				rootLayout.Children.Add(refreshContainer);
				refreshContainer.ApplyTemplate();
				UpdateRefreshAccentColor();
			}
			else
			{
				refreshContainer.Content = null;
				rootLayout.Children.Remove(refreshContainer);
				rootLayout.Children.Add(contentLayout);
			}
		});
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
			contentLayout.Children.Remove(emptyView);

		emptyView = VirtualView?.EmptyView?.ToPlatform(MauiContext);

		if (emptyView is not null)
			contentLayout.Children.Add(emptyView);

		UpdateEmptyViewVisibility();
	}

	void UpdateVerticalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		scrollViewer.VerticalScrollBarVisibility = VirtualView.Orientation == ListOrientation.Horizontal
			? WScrollBarVisibility.Disabled : scrollBarVisibility switch
		{
			ScrollBarVisibility.Default => WScrollBarVisibility.Auto,
			ScrollBarVisibility.Always => WScrollBarVisibility.Visible,
			ScrollBarVisibility.Never => WScrollBarVisibility.Hidden,
			_ => WScrollBarVisibility.Auto
		};
	}

	void UpdateHorizontalScrollbarVisibility(ScrollBarVisibility scrollBarVisibility)
	{
		scrollViewer.HorizontalScrollBarVisibility = VirtualView.Orientation == ListOrientation.Vertical
			? WScrollBarVisibility.Disabled : scrollBarVisibility switch
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
				if (irElementContainer.PositionInfo is not null && VisibilityHitTest(irElementContainer, scrollViewer))
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
