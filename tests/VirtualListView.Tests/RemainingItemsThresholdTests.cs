using Microsoft.Maui.Adapters;

namespace VirtualListView.Tests;

public class RemainingItemsThresholdTests
{
	[Fact]
	public void ThresholdDefaultsToDisabledAndRejectsOtherNegativeValues()
	{
		var view = new TestListView();
		Assert.Equal(-1, view.RemainingItemsThreshold);
		Assert.Throws<ArgumentOutOfRangeException>(() => view.RemainingItemsThreshold = -2);
		Assert.Throws<ArgumentOutOfRangeException>(() => view.SetValue(TestListView.RemainingItemsThresholdProperty, -2));
		view.RemainingItemsThreshold = 0;
		Assert.Equal(0, view.RemainingItemsThreshold);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CommandUsesCanExecuteAndNullParameterAndEventStillFires(bool canExecute)
	{
		var view = new TestListView();
		var calls = new List<string>();
		view.RemainingItemsThresholdReachedCommand = new Command(parameter =>
		{
			Assert.Null(parameter);
			calls.Add("command");
		}, parameter =>
		{
			Assert.Null(parameter);
			return canExecute;
		});
		view.RemainingItemsThresholdReached += (sender, args) =>
		{
			Assert.Same(view, sender);
			Assert.Same(EventArgs.Empty, args);
			calls.Add("event");
		};
		((IVirtualListView)view).SendRemainingItemsThresholdReached();
		Assert.Equal(canExecute ? new[] { "command", "event" } : new[] { "event" }, calls);
	}

	[Fact]
	public void ConsumerFailuresPropagateInsteadOfBeingHidden()
	{
		var view = new TestListView
		{
			RemainingItemsThresholdReachedCommand = new Command(() => throw new ApplicationException("load failed"))
		};
		Assert.Throws<ApplicationException>(() => ((IVirtualListView)view).SendRemainingItemsThresholdReached());
	}

	[Fact]
	public void RemainingCountUsesOnlyItemsAcrossEmptySectionsAndAllChromeKinds()
	{
		var source = new MutableAdapter(2, 0, 3, 0);
		var projection = new PositionalViewSelector(new TestListView
		{
			Adapter = source,
			GlobalHeader = new ContentView(),
			GlobalFooter = new ContentView(),
			SectionHeaderTemplate = new DataTemplate(() => new ContentView()),
			SectionFooterTemplate = new DataTemplate(() => new ContentView())
		});
		Assert.Equal(5, projection.DataItemCount);
		Assert.Equal(15, projection.TotalCount);
		var rows = Enumerable.Range(0, projection.TotalCount).Select(projection.GetInfo).ToArray();
		var chrome = rows.Where(info => info.Kind != PositionKind.Item).ToArray();
		Assert.Null(projection.GetRemainingItemCount(chrome));
		Assert.Equal(3, projection.GetRemainingItemCount(new[] { rows.Single(info => info.Kind == PositionKind.Item && info.SectionIndex == 0 && info.ItemIndex == 1) }));
		Assert.Equal(0, projection.GetRemainingItemCount(rows.Reverse().ToArray()));
		Assert.Equal(0, source.ItemReads);
		Assert.Equal(0, source.SectionReads);
		Assert.Equal(4, source.ItemCountReads);
	}

	[Fact]
	public void PositionMetadataCannotOverrideAcceptedCountsOrSupplyAnInvalidItem()
	{
		var source = new MutableAdapter(4);
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		var visible = new PositionInfo { SectionIndex = 0, ItemIndex = 2, ItemsInSection = 100, NumberOfSections = 10 };
		source.Counts = new[] { 100 };
		Assert.Equal(1, projection.GetRemainingItemCount(new[] { visible }));
		Assert.Equal(4, projection.DataItemCount);
		Assert.Null(projection.GetRemainingItemCount(new[]
		{
			new PositionInfo { SectionIndex = -1, ItemIndex = 0 },
			new PositionInfo { SectionIndex = 1, ItemIndex = 0 },
			new PositionInfo { SectionIndex = 0, ItemIndex = -1 },
			new PositionInfo { SectionIndex = 0, ItemIndex = 4 }
		}));
		Assert.Equal(1, source.ItemCountReads);
		projection.Invalidate();
		Assert.Equal(97, projection.GetRemainingItemCount(new[] { visible }));
	}

	[Fact]
	public void SnapshotSubmissionDoesNotChangeTheDisplayedRemainingCountUntilAccepted()
	{
		static VirtualListViewSnapshot Snapshot(long revision, int count) => new(revision, new[]
		{
			new SnapshotSection("section", 0, null,
				Enumerable.Range(0, count).Select(i => new SnapshotItem($"item-{i}", 0, i)))
		});
		var adapter = new SnapshotAdapter(Snapshot(0, 3));
		var view = new TestListView { Adapter = adapter };
		var projection = new PositionalViewSelector(view);
		var visible = new[] { projection.GetInfo(2) };
		adapter.Submit(Snapshot(1, 10));
		Assert.Equal(3, projection.DataItemCount);
		Assert.Equal(0, projection.GetRemainingItemCount(visible));
		projection.Accept(new PositionalViewSelector(view, adapter.CaptureSnapshot()));
		Assert.Equal(10, projection.DataItemCount);
		Assert.Equal(7, projection.GetRemainingItemCount(visible));
	}

	[Fact]
	public void EmptyOrChromeOnlyListsNeverRequestAFirstPage()
	{
		var projection = new PositionalViewSelector(new TestListView
		{
			Adapter = new MutableAdapter(0, 0),
			SectionFooterTemplate = new DataTemplate(() => new ContentView()),
			GlobalFooter = new ContentView()
		});
		var state = new RemainingItemsThresholdState();
		state.UpdateItemCount(projection.DataItemCount);
		var visible = Enumerable.Range(0, projection.TotalCount).Select(projection.GetInfo).ToArray();
		Assert.Equal(0, projection.DataItemCount);
		Assert.False(state.ShouldNotify(100, projection.GetRemainingItemCount(visible)));
		Assert.False(state.ShouldNotify(0, null));
	}

	[Theory]
	[InlineData(-1, 0, false)]
	[InlineData(0, 1, false)]
	[InlineData(0, 0, true)]
	[InlineData(3, 3, true)]
	[InlineData(3, 4, false)]
	public void ThresholdHasExactDataItemSemantics(int threshold, int remaining, bool expected)
	{
		var state = new RemainingItemsThresholdState();
		state.UpdateItemCount(10);
		Assert.Equal(expected, state.ShouldNotify(threshold, remaining));
	}

	[Fact]
	public void InitialShortListNotifiesOnceWithoutAnyScrollAndCallbacksAreLatched()
	{
		var state = new RemainingItemsThresholdState();
		state.UpdateItemCount(3);
		Assert.True(state.ShouldNotify(0, 0));
		for (var callback = 0; callback < 1000; callback++)
			Assert.False(state.ShouldNotify(0, 0));
		Assert.False(state.ShouldNotify(0, null));
		Assert.False(state.ShouldNotify(0, 0));
		Assert.False(state.ShouldNotify(0, 1));
		Assert.True(state.ShouldNotify(0, 0));
	}

	[Fact]
	public void GrowthRearmsButFailureNoProgressContentRefreshOrRemovalDoesNot()
	{
		var state = new RemainingItemsThresholdState();
		state.UpdateItemCount(3);
		Assert.True(state.ShouldNotify(3, 0));
		Assert.False(state.ShouldNotify(3, 0)); // A failed request publishes nothing.
		state.UpdateItemCount(3);
		Assert.False(state.ShouldNotify(3, 0));
		state.UpdateItemCount(2);
		Assert.False(state.ShouldNotify(3, 0));
		state.UpdateItemCount(4);
		Assert.True(state.ShouldNotify(3, 1));
		Assert.False(state.ShouldNotify(3, 1));
		state.UpdateItemCount(10);
		Assert.False(state.ShouldNotify(3, 7));
		Assert.True(state.ShouldNotify(3, 3));
	}

	[Fact]
	public void DisableThresholdChangeAndSourceOrConnectionResetRearmExplicitly()
	{
		var state = new RemainingItemsThresholdState();
		state.UpdateItemCount(5);
		Assert.True(state.ShouldNotify(1, 0));
		Assert.False(state.ShouldNotify(-1, 0));
		Assert.True(state.ShouldNotify(1, 0));
		Assert.True(state.ShouldNotify(2, 0));
		Assert.False(state.ShouldNotify(2, 0));
		state.Reset();
		state.UpdateItemCount(5);
		Assert.True(state.ShouldNotify(2, 0));
	}

	[Fact]
	public void HugeItemCountsStayIndexedAndDoNotOverflowOrReadPayloads()
	{
		var source = new MutableAdapter(int.MaxValue - 10, 0, 10);
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		Assert.Equal(int.MaxValue, projection.DataItemCount);
		Assert.Equal(int.MaxValue - 1, projection.GetRemainingItemCount(new[] { projection.GetInfo(0) }));
		Assert.Equal(0, projection.GetRemainingItemCount(new[] { projection.GetInfo(int.MaxValue - 1) }));
		Assert.Equal(0, source.ItemReads);
		Assert.Equal(3, source.ItemCountReads);
	}
}
