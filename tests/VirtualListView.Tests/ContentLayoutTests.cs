using System.Collections.Concurrent;
using Microsoft.Maui.Layouts;

namespace VirtualListView.Tests;

public class ContentLayoutTests
{
	[Fact]
	public void ApplicationInsetChangesRemeasureTheHorizontalCrossSizeWithoutPinningThePrimaryExtent()
	{
		var view = new MeasureView();
		var handler = new MeasureHandler(view, (width, height) =>
		{
			Assert.True(double.IsPositiveInfinity(width));
			return new Size(125, height);
		});
		using var layout = Create(view);

		foreach (var applicationInset in new[] { 0d, 30, 60, 59, 30, 1, 0 })
		{
			var cross = 544 - applicationInset;
			Assert.Equal(new Size(125, cross), layout.Measure(ListOrientation.Horizontal, cross));
		}
		Assert.Equal(7, handler.Measures);
	}

	[Fact]
	public void VerticalMeasureUsesFiniteWidthAndArrangesTheAcceptedOuterSize()
	{
		var view = new MeasureView();
		var handler = new MeasureHandler(view, (width, height) =>
		{
			Assert.Equal(336, width);
			Assert.True(double.IsPositiveInfinity(height));
			return new Size(width, 80);
		});
		view.Margin = new Thickness(12, 6);
		using var layout = Create(view);

		Assert.Equal(new Size(360, 92), layout.Measure(ListOrientation.Vertical, 360));
		layout.Arrange(ListOrientation.Vertical, new Size(360, 92));

		Assert.Equal(new Rect(0, 0, 360, 92), view.Accepted);
		Assert.Equal(new Rect(12, 6, 336, 80), handler.Arranged);
		Assert.Equal(1, handler.Measures);
	}

	[Fact]
	public void HorizontalMeasureConstrainsHeightAndKeepsVariableWidth()
	{
		var view = new MeasureView { Margin = new Thickness(7, 5) };
		var handler = new MeasureHandler(view, (width, height) =>
		{
			Assert.True(double.IsPositiveInfinity(width));
			Assert.Equal(190, height);
			return new Size(125, height);
		});
		using var layout = Create(view);

		var measured = layout.Measure(ListOrientation.Horizontal, 200);
		Assert.Equal(new Size(139, 200), measured);
		layout.Arrange(ListOrientation.Horizontal, measured);
		Assert.Equal(new Rect(7, 5, 125, 190), handler.Arranged);
	}

	[Fact]
	public void RootMarginsAndNestedPaddingAreEachAppliedOnceByMaui()
	{
		var leaf = new MeasureView();
		var leafHandler = new MeasureHandler(leaf, (width, _) =>
		{
			Assert.Equal(304, width);
			return new Size(width, 90);
		});
		var inner = new MeasureView { Padding = new Thickness(8), Content = leaf };
		var innerHandler = ContentHandler(inner);
		var root = new MeasureView { Margin = new Thickness(12, 6), Padding = new Thickness(8, 10), Content = inner };
		var rootHandler = ContentHandler(root);
		using var layout = Create(root);

		var measured = layout.Measure(ListOrientation.Vertical, 360);
		Assert.Equal(new Size(360, 138), measured);
		layout.Arrange(ListOrientation.Vertical, measured);

		Assert.Equal(new Rect(12, 6, 336, 126), rootHandler.Arranged);
		Assert.Equal(new Rect(8, 10, 320, 106), innerHandler.Arranged);
		Assert.Equal(new Rect(8, 8, 304, 90), leafHandler.Arranged);
	}

	[Fact]
	public void ResizeAndOrientationChangesRemeasureWithoutReplacingTheView()
	{
		var view = new MeasureView();
		var handler = new MeasureHandler(view, (width, height) => double.IsFinite(width)
			? new Size(width, 1200 / width) : new Size(1200 / height, height));
		using var layout = Create(view);

		Assert.Equal(new Size(300, 4), layout.Measure(ListOrientation.Vertical, 300));
		Assert.Equal(new Size(300, 4), layout.Measure(ListOrientation.Vertical, 300));
		Assert.Equal(new Size(150, 8), layout.Measure(ListOrientation.Vertical, 150));
		Assert.Equal(new Size(8, 150), layout.Measure(ListOrientation.Horizontal, 150));
		Assert.Equal(new Size(4, 300), layout.Measure(ListOrientation.Horizontal, 300));
		Assert.Equal(4, handler.Measures);
	}

	[Fact]
	public void NativeMeasureRequestsRefreshTheSlotEvenWithoutAMauiPropertyEvent()
	{
		var view = new MeasureView();
		var extent = 40d;
		var handler = new MeasureHandler(view, (width, _) => new Size(width, extent));
		using var layout = Create(view);
		layout.Measure(ListOrientation.Vertical, 320);
		extent = 75;
		Assert.Equal(new Size(320, 75), layout.Measure(ListOrientation.Vertical, 320, force: true));
		layout.Arrange(ListOrientation.Vertical, new Size(320, 75));
		Assert.Equal(2, handler.Measures);
		Assert.Equal(75, handler.Arranged.Height);
	}

	[Fact]
	public async Task ContentChangesAreCoalescedAndRemeasuredOnTheOwningDispatcher()
	{
		var dispatcher = new ManualDispatcher();
		var view = new MeasureView();
		var extent = 40d;
		var handler = new MeasureHandler(view, (width, _) => new Size(width, extent));
		var notifications = new List<bool>();
		using var layout = new RetainedContentLayout(view, dispatcher.Dispatch, notifications.Add);
		layout.Measure(ListOrientation.Vertical, 320);

		await Task.Run(() =>
		{
			for (var i = 0; i < 100; i++)
				view.ChangeMeasure();
		});
		extent = 140;
		Assert.Equal(1, dispatcher.Pending);
		Assert.Equal(1, handler.Measures);
		Assert.Empty(notifications);
		dispatcher.Drain();
		Assert.Equal(new[] { true }, notifications);
		Assert.Equal(new Size(320, 140), layout.Measure(ListOrientation.Vertical, 320));
		Assert.Equal(2, handler.Measures);
		Assert.Equal(0, dispatcher.Pending);

		view.ChangeMeasure();
		dispatcher.Drain();
		Assert.Equal(new[] { true, false }, notifications);
		Assert.Equal(0, dispatcher.Pending);
	}

	[Fact]
	public void DisposedContentRejectsQueuedNotificationsAndUnsubscribes()
	{
		var dispatcher = new ManualDispatcher();
		var view = new MeasureView();
		var handler = new MeasureHandler(view, (width, _) => new Size(width, 40));
		var notifications = 0;
		var layout = new RetainedContentLayout(view, dispatcher.Dispatch, _ => notifications++);
		layout.Measure(ListOrientation.Vertical, 320);
		view.ChangeMeasure();
		layout.Dispose();
		layout.Dispose();
		dispatcher.Drain();
		view.ChangeMeasure();
		Assert.Equal(0, dispatcher.Pending);
		Assert.Equal(0, notifications);
		Assert.Equal(1, handler.Measures);
	}

	[Fact]
	public void InterveningArrangeCannotConsumeAPendingExtentChange()
	{
		var dispatcher = new ManualDispatcher();
		var view = new MeasureView();
		var extent = 40d;
		_ = new MeasureHandler(view, (width, _) => new Size(width, extent));
		var notifications = new List<bool>();
		using var layout = new RetainedContentLayout(view, dispatcher.Dispatch, notifications.Add);
		layout.Measure(ListOrientation.Vertical, 320);
		extent = 140;
		view.ChangeMeasure();
		layout.Arrange(ListOrientation.Vertical, new Size(320, 40));
		dispatcher.Drain();
		Assert.Equal(new[] { true }, notifications);
		Assert.Equal(new Size(320, 140), layout.Measure(ListOrientation.Vertical, 320));
	}

	[Fact]
	public void NestedPaddingAndRootMarginChangesInvalidateTheCachedExtent()
	{
		var dispatcher = new ManualDispatcher();
		var leaf = new MeasureView();
		_ = new MeasureHandler(leaf, (width, _) => new Size(width, 90));
		var root = new MeasureView { Content = leaf, Padding = 8, Margin = 6 };
		_ = ContentHandler(root);
		using var layout = new RetainedContentLayout(root, dispatcher.Dispatch, _ => { });
		Assert.Equal(new Size(320, 118), layout.Measure(ListOrientation.Vertical, 320));
		root.Padding = 12;
		root.Margin = 10;
		dispatcher.Drain();
		Assert.Equal(new Size(320, 134), layout.Measure(ListOrientation.Vertical, 320));
	}

	[Fact]
	public void HeaderFooterAndRowsKeepIndependentMeasurements()
	{
		var header = new MeasureView();
		var footer = new MeasureView();
		var row = new MeasureView();
		_ = new MeasureHandler(header, (width, _) => new Size(width, 145));
		_ = new MeasureHandler(footer, (width, _) => new Size(width, 98));
		_ = new MeasureHandler(row, (width, _) => new Size(width, 20));
		using var headerLayout = Create(header);
		using var footerLayout = Create(footer);
		using var rowLayout = Create(row);
		Assert.Equal(20, rowLayout.Measure(ListOrientation.Vertical, 320).Height);
		Assert.Equal(145, headerLayout.Measure(ListOrientation.Vertical, 320).Height);
		Assert.Equal(98, footerLayout.Measure(ListOrientation.Vertical, 320).Height);
	}

	[Theory]
	[InlineData(2.625, 92.1, 242)]
	[InlineData(1, 92.1, 93)]
	[InlineData(3, 92, 276)]
	public void AndroidExtentRoundsOutwardInNativePixels(double density, double logical, int expected)
		=> Assert.Equal(expected, RetainedContentLayout.ToPixels(logical, density));

	[Fact]
	public void InvalidConstraintsAndBrokenDesiredSizesFailExplicitly()
	{
		var view = new MeasureView();
		_ = new MeasureHandler(view, (_, _) => new Size(double.PositiveInfinity, 12));
		using var layout = Create(view);
		Assert.Throws<ArgumentOutOfRangeException>(() => layout.Measure(ListOrientation.Vertical, double.PositiveInfinity));
		Assert.Throws<ArgumentOutOfRangeException>(() => layout.Measure(ListOrientation.Vertical, double.NaN));
		Assert.Throws<ArgumentOutOfRangeException>(() => layout.Measure(ListOrientation.Vertical, -1));
		Assert.Throws<InvalidOperationException>(() => layout.Measure(ListOrientation.Vertical, 300));
		Assert.Throws<ArgumentOutOfRangeException>(() => layout.Arrange(ListOrientation.Vertical, new Size(300, double.NaN)));
		Assert.Throws<ArgumentOutOfRangeException>(() => RetainedContentLayout.ToPixels(20, 0));
	}

	[Fact]
	public void DispatcherRejectionIsNotHiddenAndDoesNotWedgeTheQueue()
	{
		var view = new MeasureView();
		var accepted = false;
		var dispatcher = new ManualDispatcher();
		using var layout = new RetainedContentLayout(view, action => accepted && dispatcher.Dispatch(action), _ => { });
		Assert.Throws<InvalidOperationException>(view.ChangeMeasure);
		accepted = true;
		view.ChangeMeasure();
		Assert.Equal(1, dispatcher.Pending);
		dispatcher.Drain();
	}

	[Fact]
	public void RepeatedGrowthAndCollapseDoNotChangeAnUnrelatedSlotsMeasurement()
	{
		var dispatcher = new ManualDispatcher();
		var changed = new MeasureView();
		var neighbor = new MeasureView();
		var extent = 40d;
		_ = new MeasureHandler(changed, (width, _) => new Size(width, extent));
		var neighborHandler = new MeasureHandler(neighbor, (width, _) => new Size(width, 65));
		var notifications = new List<bool>();
		using var changedLayout = new RetainedContentLayout(changed, dispatcher.Dispatch, notifications.Add);
		using var neighborLayout = Create(neighbor);
		changedLayout.Measure(ListOrientation.Vertical, 320);
		neighborLayout.Measure(ListOrientation.Vertical, 320);

		foreach (var next in new[] { 180d, 40d, 180d, 40d })
		{
			extent = next;
			changed.ChangeMeasure();
			dispatcher.Drain();
			Assert.Equal(new Size(320, next), changedLayout.Measure(ListOrientation.Vertical, 320));
			Assert.Equal(new Size(320, 65), neighborLayout.Measure(ListOrientation.Vertical, 320));
		}

		Assert.Equal(new[] { true, true, true, true }, notifications);
		Assert.Equal(1, neighborHandler.Measures);
	}

	[Fact]
	public void NestedChildVisibilityExpandsAndCollapsesTheRetainedRoot()
	{
		var dispatcher = new ManualDispatcher();
		var title = new MeasureView();
		var details = new MeasureView { IsVisible = false };
		_ = new MeasureHandler(title, (width, _) => new Size(width, 20));
		_ = new MeasureHandler(details, (width, _) => new Size(width, 90));
		var inner = new VerticalStackLayout { Children = { title, details } };
		var manager = new VerticalStackLayoutManager(inner);
		_ = new MeasureHandler(inner, manager.Measure);
		var root = new MeasureView { Content = inner, Margin = 6, Padding = 8 };
		_ = ContentHandler(root);
		var notifications = new List<bool>();
		using var layout = new RetainedContentLayout(root, dispatcher.Dispatch, notifications.Add);
		Assert.Equal(new Size(320, 48), layout.Measure(ListOrientation.Vertical, 320));

		details.IsVisible = true;
		dispatcher.Drain();
		Assert.Equal(new Size(320, 138), layout.Measure(ListOrientation.Vertical, 320));
		details.IsVisible = false;
		dispatcher.Drain();
		Assert.Equal(new Size(320, 48), layout.Measure(ListOrientation.Vertical, 320));
		Assert.Equal(new[] { true, true }, notifications);
	}

	static RetainedContentLayout Create(IView view) => new(view, _ => true, _ => { });

	static MeasureHandler ContentHandler(MeasureView view)
		=> new(view, (width, height) => ((IContentView)view).MeasureContent(width, height),
			frame => ((IContentView)view).ArrangeContent(new Rect(Point.Zero, frame.Size)));

	sealed class MeasureView : ContentView
	{
		public Rect Accepted { get; private set; }
		public void ChangeMeasure() => InvalidateMeasure();
		protected override Size ArrangeOverride(Rect bounds)
		{
			Accepted = bounds;
			return base.ArrangeOverride(bounds);
		}
	}

	sealed class ManualDispatcher
	{
		readonly ConcurrentQueue<Action> pending = new();
		public int Pending => pending.Count;
		public bool Dispatch(Action action) { pending.Enqueue(action); return true; }
		public void Drain()
		{
			var count = 0;
			while (pending.TryDequeue(out var action))
			{
				Assert.True(++count < 100, "Content layout did not converge.");
				action();
			}
		}
	}

	sealed class MeasureHandler : IViewHandler
	{
		readonly Func<double, double, Size> measure;
		readonly Action<Rect> arrange;
		public MeasureHandler(IView view, Func<double, double, Size> measure, Action<Rect> arrange = null)
		{
			this.measure = measure;
			this.arrange = arrange;
			VirtualView = view;
			view.Handler = this;
		}
		public int Measures { get; private set; }
		public Rect Arranged { get; private set; }
		public Size GetDesiredSize(double width, double height) { Measures++; return measure(width, height); }
		public void PlatformArrange(Rect frame) { Arranged = frame; arrange?.Invoke(frame); }
		public bool HasContainer { get; set; }
		public object ContainerView => null;
		public object PlatformView => null;
		public IView VirtualView { get; private set; }
		IElement IElementHandler.VirtualView => VirtualView;
		public IMauiContext MauiContext => null;
		public void SetMauiContext(IMauiContext context) { }
		public void SetVirtualView(IElement view) => VirtualView = (IView)view;
		public void UpdateValue(string property) { }
		public void Invoke(string command, object args) { }
		public void DisconnectHandler() { }
	}
}
