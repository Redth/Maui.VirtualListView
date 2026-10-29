using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Adapters;
using Microsoft.Maui.Dispatching;

namespace VirtualListView.Tests;

public class KeyedUpdateTests
{
	static SnapshotItem Item(string key, long revision = 0, object data = null) => new(key, revision, data ?? key);
	static SnapshotSection Section(string key, params SnapshotItem[] items) => new(key, 0, key, items);
	static VirtualListViewSnapshot Snapshot(long revision, params string[] keys)
		=> new(revision, new[] { Section("section", keys.Select(key => Item(key)).ToArray()) });
	static KeyedRows Rows(VirtualListViewSnapshot snapshot, bool chrome = false)
	{
		var view = new TestListView
		{
			Adapter = new SnapshotAdapter(snapshot),
			Selector = new CountingSelector { Header = _ => chrome, Footer = _ => chrome },
			GlobalHeader = chrome ? new ContentView() : null,
			GlobalFooter = chrome ? new ContentView() : null
		};
		return new(new PositionalViewSelector(view, snapshot), snapshot);
	}

	[Fact]
	public void SameKeyWithChangedContentEmitsReplaceAndKeepsIdentity()
	{
		var before = Rows(new(0, new[] { Section("s", Item("a", 0, "short")) }));
		var after = Rows(new(1, new[] { Section("s", Item("a", 1, "long wrapping content")) }));
		var plan = new KeyedUpdatePlan(before, after, false);
		Assert.False(plan.Reset);
		Assert.Equal(new RowEdit(RowEditKind.Replace, 0), Assert.Single(plan.Edits));
		Assert.Equal(new[] { 0 }, plan.Changed);
		Assert.Empty(plan.Inserted);
		Assert.Empty(plan.Removed);
		Assert.Equal(before.Rows[0].Key, after.Rows[0].Key);
		Assert.Equal("long wrapping content", after.Rows[0].Data);
	}

	[Fact]
	public void SectionContentRevisionReconfiguresItsHeaderAndFooterButNotUnchangedItems()
	{
		var before = Rows(new(0, new[] { new SnapshotSection("s", 0, "old", new[] { Item("a") }) }), true);
		var after = Rows(new(1, new[] { new SnapshotSection("s", 1, "new", new[] { Item("a") }) }), true);
		var plan = new KeyedUpdatePlan(before, after, false);
		Assert.Equal(new[] { 1, 3 }, plan.Changed);
		Assert.All(plan.Edits, edit => Assert.Equal(RowEditKind.Replace, edit.Kind));
		Assert.Equal(PositionKind.SectionHeader, after.Rows[1].Key.Kind);
		Assert.Equal(PositionKind.SectionFooter, after.Rows[3].Key.Kind);
		Replay(plan);
	}

	[Fact]
	public void DuplicateSectionOrOccurrenceKeysAreRejectedButDifferentSectionsCanRepeatAnItemKey()
	{
		Assert.Throws<ArgumentException>(() => Section("s", Item("a"), Item("a")));
		Assert.Throws<ArgumentException>(() => new VirtualListViewSnapshot(0, new[] { Section("s"), Section("s") }));
		var rows = Rows(new(0, new[] { Section("s", Item("a")), Section("t", Item("a")) }), true);
		Assert.Equal(8, rows.Rows.Length);
		Assert.Equal(rows.Rows.Length, rows.Indices.Count);
		Assert.NotEqual(rows.Rows[2].Key, rows.Rows[5].Key);
	}

	[Fact]
	public void SnapshotCopiesInputCollectionsAndRejectsBadRevisionsAndIndices()
	{
		var items = new[] { Item("a") };
		var sections = new[] { Section("s", items) };
		var snapshot = new VirtualListViewSnapshot(4, sections);
		items[0] = Item("changed");
		sections[0] = Section("changed");
		Assert.Equal("a", snapshot.Sections[0].Items[0].Key);
		Assert.Equal("s", snapshot.Sections[0].Key);
		Assert.Throws<ArgumentOutOfRangeException>(() => Item("a", -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => new VirtualListViewSnapshot(-1, Array.Empty<SnapshotSection>()));
		Assert.Throws<ArgumentException>(() => Item(""));
		Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetItem(0, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetNumberOfItemsInSection(-1));
		var adapter = new SnapshotAdapter(snapshot);
		Assert.Throws<ArgumentException>(() => adapter.Submit(snapshot));
		Assert.Throws<ArgumentException>(() => adapter.Submit(Snapshot(3, "a")));
		Assert.Same(snapshot, adapter.CaptureSnapshot());
	}

	[Fact]
	public void ContentRevisionsCannotGoBackwardsExceptForAnExplicitQueryReset()
	{
		var adapter = new SnapshotAdapter(new(0, new[] { new SnapshotSection("s", 2, null, new[] { Item("a", 2) }) }));
		Assert.Throws<ArgumentException>(() => adapter.Submit(new(1, new[] { new SnapshotSection("s", 1, null, new[] { Item("a", 2) }) })));
		Assert.Throws<ArgumentException>(() => adapter.Submit(new(1, new[] { new SnapshotSection("s", 2, null, new[] { Item("a", 1) }) })));
		adapter.Submit(new(1, new[] { Section("s", Item("a")) }), reset: true);
		Assert.Equal(1, adapter.CaptureSnapshot().Revision);
	}

	[Fact]
	public void ProjectionUsesAcceptedDataNotLaterSourceSubmissions()
	{
		var adapter = new SnapshotAdapter(Snapshot(0, "a"));
		var view = new TestListView { Adapter = adapter };
		var accepted = new PositionalViewSelector(view);
		adapter.Submit(Snapshot(1, "b", "c"));
		Assert.Equal(1, accepted.TotalCount);
		Assert.Equal("a", accepted.Adapter.GetItem(0, 0));
		var next = new PositionalViewSelector(view, adapter.CaptureSnapshot());
		accepted.Accept(next);
		Assert.Equal(2, accepted.TotalCount);
		Assert.Equal("b", accepted.Adapter.GetItem(0, 0));
	}

	[Fact]
	public void FlatUpdatesIncludeSectionChromeAndMoveCoordinates()
	{
		var before = Rows(new(0, new[] { Section("s", Item("a"), Item("b")), Section("t", Item("c")) }), true);
		var after = Rows(new(1, new[] { Section("t", Item("c")), Section("s", Item("b"), Item("a"), Item("new")) }), true);
		var plan = new KeyedUpdatePlan(before, after, false);
		Assert.False(plan.Reset);
		Replay(plan);
		Assert.Equal(PositionKind.Header, after.Rows[0].Key.Kind);
		Assert.Equal(PositionKind.Footer, after.Rows[^1].Key.Kind);
		Assert.Equal(2, after.Rows[2].Position.Position);
		Assert.Contains(plan.Moved, move => before.Rows[move.From].Key == after.Rows[move.To].Key);
		Assert.All(plan.Moved, move => Assert.Equal(before.Rows[move.From].Key, after.Rows[move.To].Key));
		Assert.Equal(2, after.Rows.Count(row => row.Key.Kind == PositionKind.SectionHeader));
		Assert.Equal(4, after.Rows.Count(row => row.Key.Kind == PositionKind.Item));
	}

	[Fact]
	public void LargePrependNeedsOneNativeInsertionNotMovesForEveryShiftedRow()
	{
		var keys = Enumerable.Range(0, 10000).Select(index => $"row-{index}").ToArray();
		var plan = new KeyedUpdatePlan(Rows(Snapshot(0, keys)), Rows(Snapshot(1, new[] { "new" }.Concat(keys).ToArray())), false);
		Assert.False(plan.Reset);
		Assert.Equal(new RowEdit(RowEditKind.Insert, 0), Assert.Single(plan.Edits));
		Assert.Empty(plan.Moved);
		Assert.Equal(new[] { 0 }, plan.Inserted);
	}

	[Fact]
	public void LargeChangesAndExplicitInvalidationUseDocumentedResetFallback()
	{
		var before = Rows(Snapshot(0, "old"));
		var after = Rows(Snapshot(1, Enumerable.Range(0, 200).Select(index => $"new-{index}").ToArray()));
		Assert.True(new KeyedUpdatePlan(before, after, false).Reset);
		Assert.True(new KeyedUpdatePlan(before, before, true).Reset);
		Assert.True(new KeyedUpdatePlan(null, before, false).Reset);
	}

	[Fact]
	public void RandomizedSequentialAndUIKitBatchPlansProduceTheExactTarget()
	{
		var random = new Random(91);
		for (var trial = 0; trial < 500; trial++)
		{
			var oldKeys = Enumerable.Range(0, 15).OrderBy(_ => random.Next()).Take(random.Next(16)).Select(index => $"key-{index}").ToArray();
			var nextKeys = Enumerable.Range(0, 18).OrderBy(_ => random.Next()).Take(random.Next(19)).Select(index => $"key-{index}").ToArray();
			var plan = new KeyedUpdatePlan(Rows(Snapshot(0, oldKeys), true), Rows(Snapshot(1, nextKeys), true), false);
			Replay(plan);
		}
	}

	static void Replay(KeyedUpdatePlan plan)
	{
		Assert.False(plan.Reset);
		var current = plan.Before.Rows.Select(row => row.Key).ToList();
		foreach (var edit in plan.Edits)
			switch (edit.Kind)
			{
				case RowEditKind.Remove: current.RemoveAt(edit.Index); break;
				case RowEditKind.Insert: current.Insert(edit.Index, plan.After.Rows[edit.Index].Key); break;
				case RowEditKind.Move:
					var key = current[edit.From];
					current.RemoveAt(edit.From);
					current.Insert(edit.Index, key);
					break;
				case RowEditKind.Replace: Assert.Equal(plan.After.Rows[edit.Index].Key, current[edit.Index]); break;
			}
		Assert.Equal(plan.After.Rows.Select(row => row.Key), current);
		var final = new PresentationKey?[plan.After.Rows.Length];
		foreach (var index in plan.Inserted)
			final[index] = plan.After.Rows[index].Key;
		foreach (var move in plan.Moved)
		{
			Assert.Null(final[move.To]);
			final[move.To] = plan.Before.Rows[move.From].Key;
		}
		var removed = plan.Removed.Concat(plan.Moved.Select(move => move.From)).ToHashSet();
		var survivors = new Queue<PresentationKey>(plan.Before.Rows.Where((_, index) => !removed.Contains(index)).Select(row => row.Key));
		for (var index = 0; index < final.Length; index++)
			final[index] ??= survivors.Dequeue();
		Assert.Empty(survivors);
		Assert.Equal(plan.After.Rows.Select(row => (PresentationKey?)row.Key), final);
	}

	[Fact]
	public void SelectionFollowsOccurrencesAndDropsDeletedKeysWithoutSelectingChrome()
	{
		var before = Rows(Snapshot(0, "a", "b", "c"), true);
		var after = Rows(Snapshot(1, "new", "c", "a"), true);
		Assert.Equal(new[] { new ItemPosition(0, 2), new ItemPosition(0, 1) },
			before.ReconcileSelection(new[] { new ItemPosition(0, 0), new ItemPosition(0, 1), new ItemPosition(0, 2),
				new ItemPosition(-1, -1), new ItemPosition(0, 0) }, after));
	}

	[Fact]
	public void AnchorPreservesKeyAndRelativeOffsetThenUsesNextAndPreviousNeighbors()
	{
		var before = Rows(Snapshot(0, "a", "b", "c"));
		var state = new KeyedAnchorState();
		var anchor = state.Capture(before, 1, -13.5);
		Assert.Equal(2, state.Resolve(anchor, Rows(Snapshot(1, "new", "a", "b", "c"))));
		Assert.Equal(-13.5, anchor.Offset);
		Assert.Equal(1, state.Resolve(anchor, Rows(Snapshot(2, "a", "c"))));
		Assert.Equal(0, state.Resolve(anchor, Rows(Snapshot(3, "a"))));
		Assert.Equal(-1, state.Resolve(anchor, Rows(Snapshot(4))));
	}

	[Fact]
	public void LaterScrollSourceReplacementOrNewLayoutSupersedesAnAnchor()
	{
		var rows = Rows(Snapshot(0, "a", "b"));
		var state = new KeyedAnchorState();
		var anchor = state.Capture(rows, 0, 10);
		state.Supersede();
		Assert.Equal(-1, state.Resolve(anchor, rows));
		anchor = state.Capture(rows, 0, 10);
		state.Invalidate();
		Assert.Equal(-1, state.Resolve(anchor, rows));
		anchor = state.Capture(rows, 0, 10);
		state.Capture(rows, 1, 0);
		Assert.Equal(-1, state.Resolve(anchor, rows));
		Assert.Null(state.Capture(rows, -1, 0));
		Assert.Null(state.Capture(rows, 0, double.NaN));
	}

	[Fact]
	public void RapidUpdatesKeepTheOriginalKeyedBaselineUntilLayoutCompletes()
	{
		var before = Rows(Snapshot(0, "a", "b", "c"));
		var state = new KeyedAnchorState();
		var pending = state.Capture(before, 1, -8);
		var afterFirst = Rows(Snapshot(1, "new", "a", "b", "c"));
		// Native index 1 may still display b while accepted index 1 now means a.
		var latest = state.Capture(afterFirst, 1, 100, pending);
		Assert.Equal(2, state.Resolve(latest, afterFirst));
		Assert.Equal(-8, latest.Offset);
		Assert.Equal(-1, state.Resolve(pending, afterFirst));
		var afterSecond = Rows(Snapshot(2, "other", "new", "a", "b", "c"));
		Assert.Equal(3, state.Resolve(latest, afterSecond));
		state.Supersede();
		Assert.Equal(-1, state.Resolve(latest, afterSecond));
	}

	[Fact]
	public void ReentrantSubmissionsNotifyEverySubscriberInRevisionOrder()
	{
		var source = new SnapshotAdapter(Snapshot(0, "a"));
		var first = new List<long>();
		var second = new List<long>();
		source.OnDataInvalidated += (_, args) =>
		{
			var snapshot = Assert.IsType<SnapshotInvalidatedEventArgs>(args).Snapshot;
			first.Add(snapshot.Revision);
			if (snapshot.Revision == 1)
				source.Submit(Snapshot(2, "c"));
		};
		source.OnDataInvalidated += (_, args) => second.Add(((SnapshotInvalidatedEventArgs)args).Snapshot.Revision);
		source.Submit(Snapshot(1, "b"));
		Assert.Equal(new long[] { 1, 2 }, first);
		Assert.Equal(first, second);
	}

	[Fact]
	public void SubscriptionTimeSnapshotEventCannotBeOverwrittenByAnOlderInitialCapture()
	{
		var source = new CapturableSource(Snapshot(0, "old"));
		source.OnCapture = () => source.Callback(source, new SnapshotInvalidatedEventArgs(Snapshot(1, "new")));
		var dispatcher = new QueueDispatcher();
		var publication = new HandlerPublication();
		var accepted = new List<long>();
		publication.Connect(dispatcher, source, () => { }, (snapshot, _) => accepted.Add(snapshot.Revision));
		dispatcher.Drain();
		Assert.Equal(new long[] { 1 }, accepted);
	}

	[Fact]
	public async Task BackgroundSnapshotsAreAcceptedInOrderWithCoherentBackingDataAndNoDroppedEdits()
	{
		var source = new SnapshotAdapter(Snapshot(0, "a"));
		var dispatcher = new QueueDispatcher();
		var publication = new HandlerPublication();
		var view = new TestListView { Adapter = source };
		var projection = new PositionalViewSelector(view);
		KeyedRows rows = null;
		var accepted = new List<long>();
		publication.Connect(dispatcher, source, () => throw new Exception("legacy reset"), (snapshot, reset) =>
		{
			Assert.True(dispatcher.IsDraining);
			var next = new PositionalViewSelector(view, snapshot);
			var after = new KeyedRows(next, snapshot);
			var plan = new KeyedUpdatePlan(rows, after, reset);
			projection.Accept(next);
			Assert.Same(snapshot, projection.Adapter);
			if (rows is not null) Replay(plan);
			Assert.Equal(after.Rows.Length, projection.TotalCount);
			rows = after;
			accepted.Add(snapshot.Revision);
		});
		await Task.Run(() =>
		{
			source.Submit(Snapshot(1, "new", "a"));
			source.Submit(Snapshot(2, "new"));
			source.Submit(Snapshot(3, "last", "new"));
		});
		Assert.Empty(accepted);
		dispatcher.Drain();
		Assert.Equal(new long[] { 0, 1, 2, 3 }, accepted);
		Assert.Equal("last", projection.Adapter.GetItem(0, 0));
	}

	[Fact]
	public void CapturedSnapshotCallbacksFromOldSourceAndConnectionCannotChangeCurrentData()
	{
		var first = new CapturableSource(Snapshot(0, "old"));
		var second = new SnapshotAdapter(Snapshot(0, "current"));
		var dispatcher = new QueueDispatcher();
		var publication = new HandlerPublication();
		var accepted = new List<string>();
		publication.Connect(dispatcher, first, () => { }, (snapshot, _) => accepted.Add((string)snapshot.GetItem(0, 0)));
		var stale = first.Callback;
		publication.SetAdapter(second);
		first.OnCapture = () => throw new Exception("A stale callback must not read its retired source.");
		stale(first, EventArgs.Empty);
		stale(first, new SnapshotInvalidatedEventArgs(Snapshot(1, "stale")));
		dispatcher.Drain();
		Assert.Equal(new[] { "current" }, accepted);
		publication.Disconnect();
		publication.Connect(dispatcher, second, () => { }, (snapshot, _) => accepted.Add((string)snapshot.GetItem(0, 0)));
		stale(first, new SnapshotInvalidatedEventArgs(Snapshot(2, "stale")));
		dispatcher.Drain();
		Assert.Equal(new[] { "current", "current" }, accepted);
		Assert.Null(first.Callback);
	}

	[Fact]
	public void ObservableEditsRetainOccurrenceKeysAndCaptureEachIntermediateState()
	{
		var a = new object();
		var b = new object();
		var items = new ObservableCollection<object> { a, b };
		using var adapter = new ObservableCollectionAdapter<object>(items);
		var original = adapter.CaptureSnapshot();
		var events = new List<SnapshotInvalidatedEventArgs>();
		adapter.OnDataInvalidated += (_, args) => events.Add(Assert.IsType<SnapshotInvalidatedEventArgs>(args));
		items.Insert(0, new object());
		items.Move(2, 0);
		items.Remove(a);
		items[0] = new object();
		Assert.Equal(4, events.Count);
		Assert.Same(a, events[0].Snapshot.GetItem(0, 1));
		Assert.Equal(original.Sections[0].Items[1].Key, events[1].Snapshot.Sections[0].Items[0].Key);
		Assert.NotEqual(original.Sections[0].Items[1].Key, events[3].Snapshot.Sections[0].Items[0].Key);
		Assert.All(events, args => Assert.False(args.Reset));
		items.Clear();
		Assert.True(events[^1].Reset);
		Assert.Empty(adapter.CaptureSnapshot().Sections);
		items.Add(a);
		Assert.False(events[^1].Reset);
		adapter.Dispose();
		var count = events.Count;
		items.Add(b);
		Assert.Equal(count, events.Count);
	}

	[Fact]
	public void ObservableRangeEventsAreCapturedAndMalformedNotificationsFailExplicitly()
	{
		var items = new RangeCollection();
		using var adapter = new ObservableCollectionAdapter<object>(items);
		var a = new object();
		var b = new object();
		items.AddRange(a, b);
		var before = adapter.CaptureSnapshot();
		items.RemoveFirstRange(2);
		Assert.Empty(adapter.CaptureSnapshot().Sections);
		Assert.Equal(2, before.Sections[0].Items.Count);
		Assert.Throws<InvalidOperationException>(items.InvalidIndex);
		Assert.Throws<InvalidOperationException>(items.InvalidCount);
	}

	[Fact]
	public void ReentrantObservableEditsKeepAllSubscriberRevisionsOrdered()
	{
		var items = new ObservableCollection<object>();
		using var adapter = new ObservableCollectionAdapter<object>(items);
		var first = new List<long>();
		var second = new List<long>();
		adapter.OnDataInvalidated += (_, args) =>
		{
			var snapshot = ((SnapshotInvalidatedEventArgs)args).Snapshot;
			first.Add(snapshot.Revision);
			if (snapshot.Revision == 1) items.Add(new object());
		};
		adapter.OnDataInvalidated += (_, args) => second.Add(((SnapshotInvalidatedEventArgs)args).Snapshot.Revision);
		items.Add(new object());
		Assert.Equal(new long[] { 1, 2 }, first);
		Assert.Equal(first, second);
		Assert.Equal(2, adapter.CaptureSnapshot().GetNumberOfItemsInSection(0));
	}

	[Fact]
	public void WindowsIntermediateReadsUseAcceptedPositionsForEverySurvivingKey()
	{
		var before = Rows(Snapshot(0, "a", "b", "c", "d"));
		var after = Rows(Snapshot(1, "d", "c", "x", "a", "b"));
		var view = new TestListView { Adapter = new SnapshotAdapter(before.Snapshot) };
		var projection = new PositionalViewSelector(view, before.Snapshot);
		var source = new IrSource(null, projection, view);
		source.Apply(new KeyedUpdatePlan(null, before, true));
		source.CollectionChanged += (_, args) =>
		{
			foreach (var wrapper in source)
				Assert.Equal(after.Indices[wrapper.key.Value], wrapper.position.Position);
			if (args.Action == NotifyCollectionChangedAction.Move)
				Assert.Equal(args.NewStartingIndex, ((IrDataWrapper)args.NewItems[0]).position.Position);
		};
		projection.Accept(new PositionalViewSelector(view, after.Snapshot));
		source.Apply(new KeyedUpdatePlan(before, after, false));
		Assert.Equal(after.Rows.Length, source.Count);
	}

	[Fact]
	public void WindowsSameKeyRevisionUsesReplaceWithBothPayloadsAndStableCount()
	{
		var before = Rows(new(0, new[] { Section("s", Item("a", 0, "old")) }));
		var after = Rows(new(1, new[] { Section("s", Item("a", 1, "new")) }));
		var view = new TestListView { Adapter = new SnapshotAdapter(before.Snapshot) };
		var projection = new PositionalViewSelector(view, before.Snapshot);
		var source = new IrSource(null, projection, view);
		source.Apply(new KeyedUpdatePlan(null, before, true));
		var calls = 0;
		source.CollectionChanged += (_, args) =>
		{
			calls++;
			Assert.Equal(NotifyCollectionChangedAction.Replace, args.Action);
			Assert.Equal(0, args.OldStartingIndex);
			Assert.Equal(0, args.NewStartingIndex);
			Assert.Equal("old", ((IrDataWrapper)args.OldItems[0]).data);
			Assert.Equal("new", ((IrDataWrapper)args.NewItems[0]).data);
			Assert.Single(source);
			Assert.Equal("new", source[0].data);
		};
		projection.Accept(new PositionalViewSelector(view, after.Snapshot));
		source.Apply(new KeyedUpdatePlan(before, after, false));
		Assert.Equal(1, calls);
	}

	[Fact]
	public void LegacyAdapterStillUsesOneResetNotificationAndTheExistingPositionProjection()
	{
		var source = new MutableAdapter(2);
		var dispatcher = new QueueDispatcher();
		var publication = new HandlerPublication();
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		var resets = 0;
		publication.Connect(dispatcher, source, () => { projection.Invalidate(); resets++; },
			(_, _) => throw new Exception("legacy adapters must not need snapshots"));
		source.Counts = new[] { 3 };
		source.InvalidateData();
		dispatcher.Drain();
		Assert.Equal(1, resets);
		Assert.Equal(3, projection.TotalCount);
		Assert.Equal(1, projection.GetPosition(0, 1));
	}

	[Fact]
	public void WindowsNotificationsExposeCorrectIntermediateCountsIndicesPayloadsAndEnumeration()
	{
		var before = Rows(Snapshot(0, "a", "b", "c"), true);
		var after = Rows(Snapshot(1, "c", "new", "a"), true);
		var view = new TestListView { Adapter = new SnapshotAdapter(before.Snapshot), Selector = new CountingSelector { Header = _ => true, Footer = _ => true },
			GlobalHeader = new ContentView(), GlobalFooter = new ContentView() };
		var projection = new PositionalViewSelector(view, before.Snapshot);
		var source = new IrSource(null, projection, view);
		source.Apply(new KeyedUpdatePlan(null, before, true));
		var mirrored = source.Select(wrapper => wrapper.key).ToList();
		var notifications = 0;
		source.CollectionChanged += (_, args) =>
		{
			notifications++;
			Assert.NotEqual(NotifyCollectionChangedAction.Reset, args.Action);
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					mirrored.Insert(args.NewStartingIndex, ((IrDataWrapper)args.NewItems[0]).key);
					break;
				case NotifyCollectionChangedAction.Remove:
					Assert.Equal(mirrored[args.OldStartingIndex], ((IrDataWrapper)args.OldItems[0]).key);
					mirrored.RemoveAt(args.OldStartingIndex);
					break;
				case NotifyCollectionChangedAction.Move:
					var key = mirrored[args.OldStartingIndex];
					mirrored.RemoveAt(args.OldStartingIndex);
					mirrored.Insert(args.NewStartingIndex, key);
					break;
			}
			Assert.Equal(mirrored.Count, source.Count);
			Assert.Equal(mirrored, source.Select(wrapper => wrapper.key).ToList());
		};
		projection.Accept(new PositionalViewSelector(view, after.Snapshot));
		source.Apply(new KeyedUpdatePlan(before, after, false));
		Assert.True(notifications > 0);
		Assert.Equal(after.Rows.Select(row => (PresentationKey?)row.Key), source.Select(wrapper => wrapper.key));
	}

	sealed class CapturableSource : ISnapshotVirtualListViewAdapter
	{
		readonly VirtualListViewSnapshot snapshot;
		public CapturableSource(VirtualListViewSnapshot snapshot) => this.snapshot = snapshot;
		public EventHandler Callback { get; private set; }
		public Action OnCapture { get; set; }
		public event EventHandler OnDataInvalidated { add => Callback += value; remove => Callback -= value; }
		public VirtualListViewSnapshot CaptureSnapshot() { OnCapture?.Invoke(); return snapshot; }
		public int GetNumberOfSections() => snapshot.GetNumberOfSections();
		public object GetSection(int index) => snapshot.GetSection(index);
		public int GetNumberOfItemsInSection(int index) => snapshot.GetNumberOfItemsInSection(index);
		public object GetItem(int section, int item) => snapshot.GetItem(section, item);
		public void InvalidateData() => Callback?.Invoke(this, EventArgs.Empty);
	}

	sealed class RangeCollection : ObservableCollection<object>
	{
		public void AddRange(params object[] values)
		{
			var index = Count;
			foreach (var value in values) Items.Add(value);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, values, index));
		}
		public void RemoveFirstRange(int count)
		{
			var removed = Items.Take(count).ToArray();
			for (var i = 0; i < count; i++) Items.RemoveAt(0);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, 0));
		}
		public void InvalidIndex() => OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new object(), 99));
		public void InvalidCount() => OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new object(), 0));
	}

	sealed class QueueDispatcher : IDispatcher
	{
		readonly Queue<Action> queue = new();
		public bool IsDraining { get; private set; }
		public bool IsDispatchRequired => !IsDraining;
		public bool Dispatch(Action action) { lock (queue) queue.Enqueue(action); return true; }
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
