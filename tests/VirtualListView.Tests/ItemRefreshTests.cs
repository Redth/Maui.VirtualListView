using System.Collections.Specialized;
using Microsoft.Maui.Adapters;
using Microsoft.Maui.Dispatching;

namespace VirtualListView.Tests;

public class ItemRefreshTests
{
	[Fact]
	public void BaseApiCopiesDeduplicatesAndValidatesTheWholeBatchBeforeNotifying()
	{
		var source = new RefreshSource(2, 1);
		var notifications = new List<ItemsInvalidatedEventArgs>();
		source.OnDataInvalidated += (_, args) => notifications.Add(Assert.IsType<ItemsInvalidatedEventArgs>(args));
		var items = new[] { new ItemPosition(1, 0), new ItemPosition(0, 1), new ItemPosition(1, 0) };
		((IItemRefreshVirtualListViewAdapter)source).InvalidateItems(items);
		items[0] = new ItemPosition(0, 0);
		Assert.Equal(new[] { new ItemPosition(1, 0), new ItemPosition(0, 1) }, Assert.Single(notifications).Items);
		Assert.Throws<ArgumentNullException>(() => source.InvalidateItems(null));
		Assert.Throws<ArgumentOutOfRangeException>(() => source.InvalidateItems(new ItemPosition(-1, 0)));
		Assert.Throws<ArgumentOutOfRangeException>(() => source.InvalidateItems(new ItemPosition(0, -1)));
		Assert.Throws<ArgumentOutOfRangeException>(() => source.InvalidateItems(new ItemPosition(2, 0)));
		Assert.Throws<ArgumentOutOfRangeException>(() => source.InvalidateItems(new(0, 0), new(1, 1)));
		source.InvalidateItems();
		Assert.Single(notifications);
		Assert.Equal(0, source.ItemReads);
	}

	[Fact]
	public void RefreshMapsSectionAndGlobalChromeButReadsOnlyRequestedPayloads()
	{
		var source = new RefreshSource(2, 0, 3);
		var view = new TestListView
		{
			Adapter = source,
			Selector = new CountingSelector { Header = _ => true, Footer = _ => true },
			GlobalHeader = new ContentView(),
			GlobalFooter = new ContentView()
		};
		var projection = new PositionalViewSelector(view);
		var plan = new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(2, 1), new ItemPosition(0, 1), new ItemPosition(2, 1) }));
		Assert.False(plan.Reset);
		Assert.True(plan.IsItemRefresh);
		Assert.Equal(new[] { 3, 9 }, plan.Changed);
		Assert.Equal(new[] { new RowEdit(RowEditKind.Replace, 3), new RowEdit(RowEditKind.Replace, 9) }, plan.Edits);
		Assert.Empty(plan.Removed);
		Assert.Empty(plan.Inserted);
		Assert.Empty(plan.Moved);
		Assert.Equal(2, source.ItemReads);
		Assert.Equal(0, source.SectionReads);
		Assert.Equal(13, projection.TotalCount);
		Assert.Same(source.Items[2][1], plan.RefreshedItems[9].Data);
	}

	[Fact]
	public void ContentOnlyPlansNeverFallBackToResetEvenAboveTheStructuralEditBudget()
	{
		var source = new RefreshSource(500);
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		var plan = new KeyedUpdatePlan(projection, new(Enumerable.Range(0, 200).Select(index => new ItemPosition(0, index))));
		Assert.False(plan.Reset);
		Assert.Equal(200, plan.Edits.Count);
		Assert.Equal(200, source.ItemReads);
		Assert.All(plan.Edits, edit => Assert.Equal(RowEditKind.Replace, edit.Kind));
	}

	[Fact]
	public void ShapeChangesAreRejectedBeforeAnyPayloadReadOrNativeNotification()
	{
		var source = new RefreshSource(2, 1);
		var selector = new CountingSelector();
		var view = new TestListView { Adapter = source, Selector = selector };
		var projection = new PositionalViewSelector(view);
		var args = new ItemsInvalidatedEventArgs(new[] { new ItemPosition(0, 0) });
		source.Items[0].RemoveAt(1);
		source.Items[1].Add(new object());
		Assert.Throws<InvalidOperationException>(() => new KeyedUpdatePlan(projection, args));
		source.Items[0].Add(new object());
		source.Items[1].RemoveAt(1);
		selector.Header = _ => true;
		Assert.Throws<InvalidOperationException>(() => new KeyedUpdatePlan(projection, args));
		selector.Header = _ => false;
		view.GlobalHeader = new ContentView();
		Assert.Throws<InvalidOperationException>(() => new KeyedUpdatePlan(projection, args));
		Assert.Equal(0, source.ItemReads);
	}

	[Fact]
	public void SameObjectRefreshRebindsOnlyItsRetainedTreeAndPreservesSelection()
	{
		var source = new RefreshSource(2);
		var selector = new BindingSelector();
		var view = new TestListView { Adapter = source, Selector = selector };
		var projection = new PositionalViewSelector(view);
		using var first = Slot(view);
		using var second = Slot(view);
		first.Bind(projection.GetInfo(0), source.Items[0][0]);
		second.Bind(projection.GetInfo(1), source.Items[0][1]);
		first.SetVisible(true);
		second.SetVisible(true);
		var firstTree = first.View;
		var secondTree = second.View;
		var firstBindings = new List<object>();
		var secondBindings = 0;
		((BindableObject)first.View).BindingContextChanged += (sender, _) => firstBindings.Add(((BindableObject)sender).BindingContext);
		((BindableObject)second.View).BindingContextChanged += (_, _) => secondBindings++;
		selector.Recycled = 0;
		selector.ReuseReads = 0;
		var plan = new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(0, 0) }));
		first.Rebind(plan, (section, item) => section == 0 && item == 0);
		second.Rebind(plan, (_, _) => false);
		Assert.Same(firstTree, first.View);
		Assert.Same(secondTree, second.View);
		Assert.Equal(new[] { null, source.Items[0][0] }, firstBindings);
		Assert.Equal(0, secondBindings);
		Assert.Equal(1, selector.Recycled);
		Assert.Equal(1, selector.ReuseReads);
		Assert.True(((IPositionInfo)first.View).IsSelected);
		Assert.Equal(0, ((IPositionInfo)first.View).ItemIndex);
		Assert.Equal(3, selector.Attached);
		Assert.Equal(1, selector.Detached);
	}

	[Fact]
	public void ChangedTemplateIsReselectedAndOldTreeReleasedWithoutTouchingOtherSlots()
	{
		var source = new RefreshSource(2);
		var selector = new BindingSelector();
		var view = new TestListView { Adapter = source, Selector = selector };
		var projection = new PositionalViewSelector(view);
		using var first = Slot(view);
		using var second = Slot(view);
		first.Bind(projection.GetInfo(0), source.Items[0][0]);
		second.Bind(projection.GetInfo(1), source.Items[0][1]);
		first.SetVisible(true);
		second.SetVisible(true);
		var old = Assert.IsType<TrackedCell>(first.View);
		var untouched = second.View;
		source.Items[0][0] = "different-template";
		var plan = new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(0, 0) }));
		first.Rebind(plan, (_, _) => true);
		second.Rebind(plan, (_, _) => false);
		Assert.NotSame(old, first.View);
		Assert.Null(old.Parent);
		Assert.Equal(1, old.Disposals);
		Assert.Same(untouched, second.View);
		Assert.Equal(3, selector.Created);
		Assert.Same(source.Items[0][0], ((BindableObject)first.View).BindingContext);
		Assert.True(((IPositionInfo)first.View).IsSelected);
		Assert.Equal(3, selector.Attached);
		Assert.Equal(1, selector.Detached);
	}

	[Fact]
	public void UnboundOffscreenSlotDoesNotCreateATreeForARefresh()
	{
		var source = new RefreshSource(1);
		var selector = new BindingSelector();
		var view = new TestListView { Adapter = source, Selector = selector };
		var projection = new PositionalViewSelector(view);
		using var slot = Slot(view);
		var plan = new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(0, 0) }));
		slot.Rebind(plan, (_, _) => false);
		Assert.Null(slot.View);
		Assert.Equal(0, selector.Created);
		slot.Bind(projection.GetInfo(0), source.Items[0][0]);
		slot.Unbind();
		slot.Rebind(plan, (_, _) => false);
		Assert.Equal(1, selector.Created);
		Assert.Null(slot.Position);
	}

	[Fact]
	public void RefreshCanReplaceAPayloadWithNullWithoutInheritingTheListsBindingContext()
	{
		var source = new RefreshSource(1);
		var view = new TestListView
		{
			Adapter = source, BindingContext = new object(),
			ItemTemplate = new DataTemplate(() => new VirtualViewCell())
		};
		var projection = new PositionalViewSelector(view);
		using var slot = Slot(view);
		slot.Bind(projection.GetInfo(0), source.Items[0][0]);
		var tree = slot.View;
		source.Items[0][0] = null;
		slot.Rebind(new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(0, 0) })), (_, _) => false);
		Assert.Same(tree, slot.View);
		Assert.Null(((BindableObject)slot.View).BindingContext);
	}

	[Fact]
	public void WindowsEmitsOnlyTargetedReplaceNotificationsWithLiveDataAndNoReset()
	{
		var source = new RefreshSource(2, 1);
		var view = new TestListView
		{
			Adapter = source, Selector = new CountingSelector { Header = _ => true, Footer = _ => true },
			GlobalHeader = new ContentView(), GlobalFooter = new ContentView()
		};
		var projection = new PositionalViewSelector(view);
		var windows = new IrSource(null, projection, view);
		source.Items[1][0] = "replacement";
		var plan = new KeyedUpdatePlan(projection, new(new[] { new ItemPosition(1, 0) }));
		var notifications = new List<NotifyCollectionChangedEventArgs>();
		windows.CollectionChanged += (_, args) =>
		{
			notifications.Add(args);
			Assert.Equal(9, windows.Count);
			Assert.Equal("replacement", windows[6].data);
		};
		windows.Apply(plan);
		var notification = Assert.Single(notifications);
		Assert.Equal(NotifyCollectionChangedAction.Replace, notification.Action);
		Assert.Equal(6, notification.NewStartingIndex);
		Assert.Equal(6, notification.OldStartingIndex);
		Assert.Equal("replacement", Assert.IsType<IrDataWrapper>(Assert.Single(notification.NewItems.Cast<object>())).data);
		Assert.Null(windows[6].key);
		Assert.Equal(PositionKind.Header, windows[0].position.Kind);
	}

	[Fact]
	public void CachesForwardTargetedEventsAndWrapperCallsOnceWithoutResettingSource()
	{
		var source = new RefreshSource(2);
		using var inner = new CachedAdapter(source);
		using var outer = new CachedAdapter(inner);
		inner.GetNumberOfSections();
		inner.GetNumberOfItemsInSection(0);
		inner.GetSection(0);
		outer.GetNumberOfSections();
		var innerEvents = new List<ItemsInvalidatedEventArgs>();
		var outerEvents = new List<ItemsInvalidatedEventArgs>();
		inner.OnDataInvalidated += (_, args) => innerEvents.Add(Assert.IsType<ItemsInvalidatedEventArgs>(args));
		outer.OnDataInvalidated += (_, args) => outerEvents.Add(Assert.IsType<ItemsInvalidatedEventArgs>(args));
		source.InvalidateItems(new ItemPosition(0, 1));
		outer.InvalidateItems(new ItemPosition(0, 0));
		Assert.Equal(2, innerEvents.Count);
		Assert.Equal(2, outerEvents.Count);
		Assert.Equal(new[] { new ItemPosition(0, 1), new ItemPosition(0, 0) }, outerEvents.Select(args => Assert.Single(args.Items)));
		Assert.Equal(0, source.Resets);
		outer.Dispose();
		source.InvalidateItems(new ItemPosition(0, 1));
		Assert.Equal(2, outerEvents.Count);
		Assert.Throws<ObjectDisposedException>(() => outer.InvalidateItems(new ItemPosition(0, 0)));
	}

	[Fact]
	public void CacheRefreshSupportsUnchangedLegacyInterfaceAndNeverFallsBackToSourceReset()
	{
		var source = new DirectSource();
		using var cache = new CachedAdapter(source);
		var events = 0;
		cache.OnDataInvalidated += (_, args) => { Assert.IsType<ItemsInvalidatedEventArgs>(args); events++; };
		cache.InvalidateItems(new ItemPosition(0, 0));
		source.Emit(new ItemsInvalidatedEventArgs(new[] { new ItemPosition(0, 0) }));
		Assert.Equal(2, events);
		Assert.Equal(0, source.Resets);
	}

	[Fact]
	public void DisposedCacheRejectsAnAlreadyCapturedTargetedSourceCallback()
	{
		var source = new DirectSource();
		var cache = new CachedAdapter(source);
		var calls = 0;
		cache.OnDataInvalidated += (_, _) => calls++;
		var stale = source.Notification;
		cache.Dispose();
		stale(source, new ItemsInvalidatedEventArgs(new[] { new ItemPosition(0, 0) }));
		Assert.Equal(0, calls);
	}

	[Fact]
	public void CacheDoesNotDropReentrantRefreshesOrDuplicateANonNotifyingSourcesRefresh()
	{
		var source = new RefreshSource(2);
		using var cache = new CachedAdapter(source);
		var items = new List<int>();
		cache.OnDataInvalidated += (_, args) =>
		{
			var index = Assert.Single(Assert.IsType<ItemsInvalidatedEventArgs>(args).Items).ItemIndex;
			items.Add(index);
			if (index == 0)
				cache.InvalidateItems(new ItemPosition(0, 1));
		};
		cache.InvalidateItems(new ItemPosition(0, 0));
		Assert.Equal(new[] { 0, 1 }, items);

		using var silentCache = new CachedAdapter(new SilentRefreshSource());
		var calls = 0;
		silentCache.OnDataInvalidated += (_, args) => { Assert.IsType<ItemsInvalidatedEventArgs>(args); calls++; };
		silentCache.InvalidateItems(new ItemPosition(0, 0));
		Assert.Equal(1, calls);
	}

	[Fact]
	public void CachedCountsCannotHideAnUnannouncedStructuralChange()
	{
		var source = new RefreshSource(2);
		using var cache = new CachedAdapter(source);
		var projection = new PositionalViewSelector(new TestListView { Adapter = cache });
		cache.GetNumberOfItemsInSection(0);
		source.Items[0].Add(new object());
		cache.OnDataInvalidated += (_, args) => new KeyedUpdatePlan(projection, Assert.IsType<ItemsInvalidatedEventArgs>(args));
		Assert.Throws<InvalidOperationException>(() => source.InvalidateItems(new ItemPosition(0, 0)));
		Assert.Equal(0, source.ItemReads);
	}

	[Fact]
	public void RefreshFailuresPropagateAndDoNotPermanentlySuppressLaterNotifications()
	{
		var source = new RefreshSource(1);
		using var cache = new CachedAdapter(source);
		var calls = 0;
		cache.OnDataInvalidated += (_, _) => calls++;
		EventHandler failure = (_, _) => throw new ApplicationException("source refresh");
		source.OnDataInvalidated += failure;
		Assert.Throws<ApplicationException>(() => cache.InvalidateItems(new ItemPosition(0, 0)));
		source.OnDataInvalidated -= failure;
		cache.InvalidateItems(new ItemPosition(0, 0));
		Assert.Equal(2, calls);
	}

	[Fact]
	public void SnapshotPlansStillSkipUnchangedBindingsAndRebindChangedRevisions()
	{
		var source = new RefreshSource(2);
		var data = source.Items[0][0];
		var selector = new BindingSelector();
		var view = new TestListView { Selector = selector };
		VirtualListViewSnapshot Snapshot(long revision) => new(revision, new[]
		{
			new SnapshotSection("s", 0, null, new[]
			{
				new SnapshotItem("first", 0, data),
				new SnapshotItem("second", revision, source.Items[0][1])
			})
		});
		var before = Snapshot(0);
		var projection = new PositionalViewSelector(view, before);
		using var first = Slot(view);
		using var second = Slot(view);
		first.Bind(projection.GetInfo(0), data);
		second.Bind(projection.GetInfo(1), source.Items[0][1]);
		selector.Recycled = 0;
		var after = Snapshot(1);
		var plan = new KeyedUpdatePlan(new KeyedRows(projection, before), new KeyedRows(new(view, after), after), false);
		first.Rebind(plan, (_, _) => false);
		second.Rebind(plan, (_, item) => item == 1);
		Assert.Equal(1, selector.Recycled);
		Assert.Equal(plan.After.Rows[0].Key, first.Key);
		Assert.True(((IPositionInfo)second.View).IsSelected);
	}

	[Fact]
	public void RefreshRetainsItsPositionalAnchorUntilLayoutAndRespectsNewScrollAndReset()
	{
		var state = new KeyedAnchorState();
		var pending = state.Capture(null, 5, -12, positionalCount: 20);
		Assert.Equal(5, state.Resolve(pending, null));
		var next = state.Capture(null, 4, 300, pending);
		Assert.Equal(5, state.Resolve(next, null));
		Assert.Equal(-12, next.Offset);
		Assert.Equal(-1, state.Resolve(pending, null));
		state.Supersede();
		Assert.Equal(-1, state.Resolve(next, null));
		next = state.Capture(null, 5, -12, positionalCount: 20);
		state.Invalidate();
		Assert.Equal(-1, state.Resolve(next, null));
		Assert.Null(state.Capture(null, 20, 0, positionalCount: 20));
	}

	[Fact]
	public async Task RefreshAndResetUseOneOrderedOwningDispatcherQueueIncludingReentrantRefresh()
	{
		var source = new RefreshSource(2);
		var publication = new HandlerPublication();
		var dispatcher = new QueueDispatcher();
		var events = new List<string>();
		publication.Connect(dispatcher, source, () => events.Add("reset"), onItems: args =>
		{
			Assert.True(dispatcher.IsDraining);
			var index = Assert.Single(args.Items).ItemIndex;
			events.Add($"item-{index}");
			if (index == 0)
				source.InvalidateItems(new ItemPosition(0, 1));
		});
		await Task.Run(() =>
		{
			source.InvalidateItems(new ItemPosition(0, 0));
			source.InvalidateData();
		});
		Assert.Empty(events);
		dispatcher.Drain();
		Assert.Equal(new[] { "item-0", "reset", "item-1" }, events);
	}

	[Fact]
	public void RefreshSubscriptionsAndPendingEventsCannotOutliveTheirSourceOrConnection()
	{
		var first = new DirectSource();
		var second = new DirectSource();
		var publication = new HandlerPublication();
		var dispatcher = new QueueDispatcher();
		var calls = 0;
		var args = new ItemsInvalidatedEventArgs(new[] { new ItemPosition(0, 0) });
		publication.Connect(dispatcher, first, () => { }, onItems: _ => calls++);
		var stale = first.Notification;
		first.Emit(args);
		publication.SetAdapter(second);
		stale(first, args);
		second.Emit(args);
		dispatcher.Drain();
		Assert.Equal(1, calls);
		Assert.Null(first.Notification);
		second.Emit(args);
		publication.Disconnect();
		Assert.Null(second.Notification);
		publication.Connect(dispatcher, first, () => { }, onItems: _ => calls++);
		dispatcher.Drain();
		Assert.Equal(1, calls);
		first.Emit(args);
		dispatcher.Drain();
		Assert.Equal(2, calls);
	}

	[Fact]
	public void SnapshotAdaptersCannotBypassRevisionSemanticsEvenThroughNestedCaches()
	{
		var source = new CustomSnapshotSource();
		Assert.Throws<InvalidOperationException>(() => source.InvalidateItems(new ItemPosition(0, 0)));
		using var inner = new CachedAdapter(source);
		using var outer = new CachedAdapter(inner);
		Assert.Throws<InvalidOperationException>(() => outer.InvalidateItems(new ItemPosition(0, 0)));
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		var args = new ItemsInvalidatedEventArgs(new[] { new ItemPosition(0, 0) });
		Assert.Throws<InvalidOperationException>(() => new KeyedUpdatePlan(projection, args));
		var publication = new HandlerPublication();
		var published = new CustomSnapshotSource();
		publication.Connect(new QueueDispatcher(), published, () => { }, (_, _) => { }, _ => throw new Exception("unexpected refresh"));
		Assert.Throws<InvalidOperationException>(() => published.Emit(args));
	}

	static RetainedView Slot(TestListView view) => new(view, _ => { }, () => { });

	class RefreshSource : VirtualListViewAdapterBase<object, object>
	{
		public RefreshSource(params int[] counts)
			=> Items = counts.Select(count => Enumerable.Range(0, count).Select(_ => new object()).ToList()).ToList();
		public List<List<object>> Items { get; }
		public int ItemReads { get; private set; }
		public int SectionReads { get; private set; }
		public int Resets { get; private set; }
		public override int GetNumberOfSections() => Items.Count;
		public override int GetNumberOfItemsInSection(int sectionIndex) => Items[sectionIndex].Count;
		public override object GetItem(int sectionIndex, int itemIndex) { ItemReads++; return Items[sectionIndex][itemIndex]; }
		public override object GetSection(int sectionIndex) { SectionReads++; return Items[sectionIndex]; }
		public override void InvalidateData() { Resets++; base.InvalidateData(); }
		public void Emit(EventArgs args) => RaiseDataInvalidated(args);
	}

	sealed class CustomSnapshotSource : RefreshSource, ISnapshotVirtualListViewAdapter
	{
		public CustomSnapshotSource() : base(1) { }
		public VirtualListViewSnapshot CaptureSnapshot() => new(0, new[]
		{
			new SnapshotSection("s", 0, null, new[] { new SnapshotItem("i", 0, Items[0][0]) })
		});
	}

	sealed class SilentRefreshSource : RefreshSource
	{
		public SilentRefreshSource() : base(1) { }
		public override void InvalidateItems(params ItemPosition[] items) { }
	}

	sealed class DirectSource : IVirtualListViewAdapter
	{
		public EventHandler Notification { get; private set; }
		public int Resets { get; private set; }
		public event EventHandler OnDataInvalidated { add => Notification += value; remove => Notification -= value; }
		public int GetNumberOfSections() => 1;
		public int GetNumberOfItemsInSection(int sectionIndex) => 1;
		public object GetSection(int sectionIndex) => null;
		public object GetItem(int sectionIndex, int itemIndex) => "item";
		public void InvalidateData() { Resets++; Emit(EventArgs.Empty); }
		public void Emit(EventArgs args) => Notification?.Invoke(this, args);
	}

	sealed class TrackedCell : VirtualViewCell, IDisposable
	{
		public int Disposals { get; private set; }
		public void Dispose() => Disposals++;
	}

	sealed class BindingSelector : IVirtualListViewSelector
	{
		public int Recycled { get; set; }
		public int ReuseReads { get; set; }
		public int Created { get; private set; }
		public int Attached { get; private set; }
		public int Detached { get; private set; }
		public bool SectionHasHeader(int index) => false;
		public bool SectionHasFooter(int index) => false;
		public IView CreateView(PositionInfo info, object data) { Created++; return new TrackedCell(); }
		public void RecycleView(PositionInfo info, object data, IView view)
		{
			Recycled++;
			((BindableObject)view).BindingContext = data;
		}
		public string GetReuseId(PositionInfo info, object data) { ReuseReads++; return data is string ? "alternate" : "standard"; }
		public void ViewAttached(PositionInfo info, IView view) => Attached++;
		public void ViewDetached(PositionInfo info, IView view) => Detached++;
	}

	sealed class QueueDispatcher : IDispatcher
	{
		readonly Queue<Action> queue = new();
		public bool IsDraining { get; private set; }
		public bool IsDispatchRequired => !IsDraining;
		public bool Dispatch(Action action) { queue.Enqueue(action); return true; }
		public void Drain()
		{
			IsDraining = true;
			try { while (queue.TryDequeue(out var action)) action(); }
			finally { IsDraining = false; }
		}
		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();
		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
	}
}
