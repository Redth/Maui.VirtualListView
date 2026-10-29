using System.Collections;
using SelectionMode = Microsoft.Maui.SelectionMode;

namespace VirtualListView.Tests;

public class SelectionTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SelectedItemsStillAcceptsBoundArraysAndLists(bool useArray)
	{
		IList<ItemPosition> original = useArray
			? new[] { new ItemPosition(0, 2), new ItemPosition(0, 0) }
			: new List<ItemPosition> { new(0, 2), new(0, 0) };
		var model = new SelectionModel { Items = original };
		var view = new TestListView { SelectionMode = SelectionMode.Multiple };
		view.SetBinding(Microsoft.Maui.Controls.VirtualListView.SelectedItemsProperty,
			new Binding(nameof(SelectionModel.Items), BindingMode.TwoWay, source: model));
		IVirtualListView contract = view;

		Assert.Equal(typeof(IList<ItemPosition>), Microsoft.Maui.Controls.VirtualListView.SelectedItemsProperty.ReturnType);
		Assert.Equal(typeof(IList<ItemPosition>), typeof(IVirtualListView).GetProperty(nameof(IVirtualListView.SelectedItems)).PropertyType);
		Assert.Same(original, contract.SelectedItems);
		contract.SelectMany(new[] { new ItemPosition(0, 1) });
		Assert.Equal(new[] { new ItemPosition(0, 2), new ItemPosition(0, 0), new ItemPosition(0, 1) }, model.Items);
		Assert.Same(view.SelectedItems, model.Items);
		Assert.Equal(new[] { new ItemPosition(0, 2), new ItemPosition(0, 0) }, original);
	}

	[Fact]
	public void SelectManyPreservesExistingOrderAndDuplicatesAndAppendsOnlyNewUniquePositions()
	{
		var original = new[] { new ItemPosition(1, 2), new ItemPosition(0, 2), new ItemPosition(1, 2) };
		var view = new TestListView { SelectionMode = SelectionMode.Multiple, SelectedItems = original };
		var events = new List<SelectedItemsChangedEventArgs>();
		var propertyChanges = 0;
		view.OnSelectedItemsChanged += (_, args) => events.Add(args);
		view.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(view.SelectedItems))
				propertyChanges++;
		};

		view.SelectMany(new[] { new ItemPosition(0, 2), new ItemPosition(0, 1), new ItemPosition(0, 1),
			new ItemPosition(1, 0), new ItemPosition(1, 2) });

		var expected = new[] { new ItemPosition(1, 2), new ItemPosition(0, 2), new ItemPosition(1, 2),
			new ItemPosition(0, 1), new ItemPosition(1, 0) };
		Assert.Equal(expected, view.SelectedItems);
		Assert.Equal(1, propertyChanges);
		var notification = Assert.Single(events);
		Assert.Equal(original, notification.PreviousSelection);
		Assert.Equal(expected, notification.NewSelection);
		Assert.NotSame(view.SelectedItems, notification.NewSelection);
		Assert.Equal(3, original.Length);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EmptyAndAlreadySelectedBatchesDoNotPublishOrReplaceTheCollection(bool empty)
	{
		var original = new List<ItemPosition> { new(0, 2), new(0, 1) };
		var view = new TestListView { SelectionMode = SelectionMode.Multiple, SelectedItems = original };
		var notifications = 0;
		view.OnSelectedItemsChanged += (_, _) => notifications++;
		view.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(view.SelectedItems))
				notifications++;
		};

		view.SelectMany(empty ? Array.Empty<ItemPosition>() : new[] { new ItemPosition(0, 1), new ItemPosition(0, 2), new ItemPosition(0, 1) });

		Assert.Same(original, view.SelectedItems);
		Assert.Equal(0, notifications);
	}

	[Fact]
	public void SingleUseInputIsEnumeratedOnceAndNotRetained()
	{
		var source = new[] { new ItemPosition(0, 1), new ItemPosition(0, 2), new ItemPosition(0, 1) };
		var input = new SingleUsePositions(source);
		var view = new TestListView { SelectionMode = SelectionMode.Multiple };
		view.SelectMany(input);
		source[0] = new ItemPosition(5, 5);
		Assert.Equal(new[] { new ItemPosition(0, 1), new ItemPosition(0, 2) }, view.SelectedItems);
		Assert.Equal(1, input.Enumerations);
		Assert.Equal(3, input.Reads);
	}

	[Theory]
	[InlineData(SelectionMode.None)]
	[InlineData(SelectionMode.Single)]
	public void SelectManyDoesNotAffectOtherSelectionModesOrEnumerateTheirInput(SelectionMode mode)
	{
		var view = new TestListView { SelectionMode = mode, SelectedItem = new ItemPosition(0, 1) };
		var original = view.SelectedItems;
		var input = new SingleUsePositions(new[] { new ItemPosition(0, 2) });
		var notifications = 0;
		view.OnSelectedItemsChanged += (_, _) => notifications++;
		view.SelectMany(input);
		Assert.Same(original, view.SelectedItems);
		Assert.Equal(new ItemPosition(0, 1), view.SelectedItem);
		Assert.Equal(0, input.Enumerations);
		Assert.Equal(0, notifications);
	}

	[Theory]
	[InlineData(SelectionMode.None)]
	[InlineData(SelectionMode.Single)]
	[InlineData(SelectionMode.Multiple)]
	public void NullInputThrowsWithoutChangingSelection(SelectionMode mode)
	{
		var view = new TestListView { SelectionMode = mode };
		var original = view.SelectedItems;
		Assert.Throws<ArgumentNullException>(() => view.SelectMany(null));
		Assert.Same(original, view.SelectedItems);
	}

	[Fact]
	public void EnumerationFailureDoesNotPublishAPartialSelection()
	{
		var view = new TestListView { SelectionMode = SelectionMode.Multiple, SelectedItems = new[] { new ItemPosition(0, 0) } };
		var original = view.SelectedItems;
		var events = 0;
		view.OnSelectedItemsChanged += (_, _) => events++;
		Assert.Throws<ApplicationException>(() => view.SelectMany(FailingInput()));
		Assert.Same(original, view.SelectedItems);
		Assert.Equal(0, events);

		static IEnumerable<ItemPosition> FailingInput()
		{
			yield return new ItemPosition(0, 1);
			throw new ApplicationException("input failed");
		}
	}

	[Fact]
	public void SelectionDefaultsAndBulkChangesAreIsolatedBetweenControls()
	{
		var first = new TestListView { SelectionMode = SelectionMode.Multiple };
		var second = new TestListView { SelectionMode = SelectionMode.Multiple };
		Assert.True(first.SelectedItems.IsReadOnly);
		first.SelectMany(new[] { new ItemPosition(0, 1) });
		Assert.Empty(second.SelectedItems);
		second.SelectMany(new[] { new ItemPosition(0, 2) });
		first.SelectMany(first.SelectedItems);
		Assert.Equal(new ItemPosition(0, 1), Assert.Single(first.SelectedItems));
		Assert.Equal(new ItemPosition(0, 2), Assert.Single(second.SelectedItems));
		first.ClearSelectedItems();
		Assert.Empty(first.SelectedItems);
		Assert.Single(second.SelectedItems);
	}

	[Fact]
	public void IndividualSelectionStillWorksAfterABulkUpdate()
	{
		var view = new TestListView { SelectionMode = SelectionMode.Multiple };
		view.SelectMany(new[] { new ItemPosition(0, 2), new ItemPosition(0, 0) });
		view.SelectItem(new ItemPosition(0, 1));
		view.SelectItem(new ItemPosition(0, 0));
		view.DeselectItem(new ItemPosition(0, 2));
		Assert.Equal(new[] { new ItemPosition(0, 0), new ItemPosition(0, 1) }, view.SelectedItems);
	}

	[Fact]
	public void HandlerUpdatesOnlyChangedValidPositionsWithDeselectionsFirst()
	{
		var projection = new PositionalViewSelector(new TestListView { Adapter = new MutableAdapter(3, 2) });
		var state = new HandlerSelection();
		var updates = new List<(ItemPosition, bool)>();
		var original = new[] { new ItemPosition(0, 2), new ItemPosition(1, 1), new ItemPosition(0, 2),
			new ItemPosition(-1, 0), new ItemPosition(0, 3) };
		state.Apply(original, SelectionMode.Multiple, projection, (position, selected) => updates.Add((position, selected)));
		Assert.Equal(new[] { (new ItemPosition(0, 2), true), (new ItemPosition(1, 1), true) }, updates);
		original[0] = new ItemPosition(0, 0);
		Assert.True(state.Contains(new ItemPosition(0, 2)));
		Assert.False(state.Contains(new ItemPosition(0, 0)));
		updates.Clear();

		state.Apply(new[] { new ItemPosition(0, 1), new ItemPosition(1, 1), new ItemPosition(0, 1) },
			SelectionMode.Multiple, projection, (position, selected) => updates.Add((position, selected)));

		Assert.Equal(new[] { (new ItemPosition(0, 2), false), (new ItemPosition(0, 1), true) }, updates);
		Assert.Equal(new[] { new ItemPosition(0, 1), new ItemPosition(1, 1) }, state.Positions);
		Assert.False(state.Contains(new ItemPosition(0, 2)));
		updates.Clear();
		state.Apply(new[] { new ItemPosition(1, 1), new ItemPosition(0, 1) },
			SelectionMode.Multiple, projection, (position, selected) => updates.Add((position, selected)));
		Assert.Empty(updates);
		Assert.Equal(new[] { new ItemPosition(1, 1), new ItemPosition(0, 1) }, state.Positions);
	}

	[Fact]
	public void HandlerModeTransitionsKeepOnlyTheFirstPositionAndClearWhenDisabled()
	{
		var projection = new PositionalViewSelector(new TestListView { Adapter = new MutableAdapter(3) });
		var state = new HandlerSelection();
		var items = new[] { new ItemPosition(0, 2), new ItemPosition(0, 1), new ItemPosition(0, 0) };
		state.Apply(items, SelectionMode.Multiple, projection, (_, _) => { });
		var updates = new List<(ItemPosition, bool)>();
		state.Apply(items, SelectionMode.Single, projection, (position, selected) => updates.Add((position, selected)));
		Assert.Equal(new[] { (new ItemPosition(0, 1), false), (new ItemPosition(0, 0), false) }, updates);
		Assert.Equal(new ItemPosition(0, 2), Assert.Single(state.Positions));
		updates.Clear();
		state.Apply(items, SelectionMode.None, projection, (position, selected) => updates.Add((position, selected)));
		Assert.Equal((new ItemPosition(0, 2), false), Assert.Single(updates));
		Assert.Empty(state.Positions);
		Assert.False(state.Contains(new ItemPosition(0, 2)));
		state.Apply(new[] { new ItemPosition(0, 99), new ItemPosition(0, 0) }, SelectionMode.Single, projection, (_, _) => Assert.Fail("Invalid first selection"));
		Assert.Empty(state.Positions);
	}

	[Fact]
	public void HandlerResetCopiesReconciledSelectionAndClearDropsTheEntireSnapshot()
	{
		var state = new HandlerSelection();
		var other = new HandlerSelection();
		var reconciled = new[] { new ItemPosition(1, 2), new ItemPosition(0, 1) };
		state.Reset(reconciled);
		reconciled[0] = new ItemPosition(0, 0);
		Assert.Equal(new[] { new ItemPosition(1, 2), new ItemPosition(0, 1) }, state.Positions);
		Assert.True(state.Contains(new ItemPosition(1, 2)));
		Assert.False(state.Contains(new ItemPosition(0, 0)));
		Assert.Empty(other.Positions);
		state.Clear();
		Assert.Empty(state.Positions);
		Assert.False(state.Contains(new ItemPosition(1, 2)));
		var projection = new PositionalViewSelector(new TestListView { Adapter = new MutableAdapter(2) });
		var updates = 0;
		state.Apply(new[] { new ItemPosition(0, 1) }, SelectionMode.Multiple, projection, (_, selected) =>
		{
			Assert.True(selected);
			updates++;
		});
		Assert.Equal(1, updates);
		state.Apply(null, SelectionMode.Multiple, projection, (_, selected) =>
		{
			Assert.False(selected);
			updates++;
		});
		Assert.Equal(2, updates);
		Assert.Empty(state.Positions);
	}

	[Fact]
	public void FiveThousandItemSelectAllPublishesOnceAndOnlyAppliesSelectionDifferences()
	{
		var adapter = new MutableAdapter(5000);
		var view = new TestListView { Adapter = adapter, SelectionMode = SelectionMode.Multiple };
		var projection = new PositionalViewSelector(view);
		var state = new HandlerSelection();
		var nativeUpdates = 0;
		var propertyChanges = 0;
		var events = 0;
		view.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName != nameof(view.SelectedItems))
				return;
			propertyChanges++;
			state.Apply(view.SelectedItems, view.SelectionMode, projection, (_, _) => nativeUpdates++);
		};
		view.OnSelectedItemsChanged += (_, _) => events++;
		var input = new SingleUsePositions(Enumerable.Range(0, 5000).Select(index => new ItemPosition(0, index)));
		view.SelectMany(input);
		Assert.Equal(1, propertyChanges);
		Assert.Equal(1, events);
		Assert.Equal(1, input.Enumerations);
		Assert.Equal(5000, input.Reads);
		Assert.Equal(5000, nativeUpdates);
		Assert.Equal(5000, view.SelectedItems.Count);
		for (var index = 0; index < 5000; index++)
			Assert.True(state.Contains(new ItemPosition(0, index)));

		var selection = view.SelectedItems;
		view.SelectMany(Enumerable.Range(0, 5000).Select(index => new ItemPosition(0, index)));
		Assert.Same(selection, view.SelectedItems);
		Assert.Equal(1, propertyChanges);
		Assert.Equal(1, events);
		state.Apply(selection, SelectionMode.Multiple, projection, (_, _) => nativeUpdates++);
		Assert.Equal(5000, nativeUpdates);
		view.SelectedItems = selection.Skip(1).Append(new ItemPosition(0, 4999)).ToArray();
		Assert.Equal(5001, nativeUpdates);
		Assert.False(state.Contains(new ItemPosition(0, 0)));
		Assert.Equal(0, adapter.ItemReads);
	}

	[Fact]
	public void LargeHandlerDifferencesReadThePublicListOnceWithoutLinearMembershipSearches()
	{
		var projection = new PositionalViewSelector(new TestListView { Adapter = new MutableAdapter(5001) });
		var state = new HandlerSelection();
		state.Reset(Enumerable.Range(0, 5000).Select(index => new ItemPosition(0, index)));
		var next = new ObservedPositions(Enumerable.Range(1, 5000).Select(index => new ItemPosition(0, index)));
		var updates = new List<(ItemPosition, bool)>();
		state.Apply(next, SelectionMode.Multiple, projection, (position, selected) => updates.Add((position, selected)));
		Assert.Equal(new[] { (new ItemPosition(0, 0), false), (new ItemPosition(0, 5000), true) }, updates);
		Assert.Equal(1, next.Enumerations);
		Assert.Equal(5000, next.Reads);
		Assert.Equal(0, next.ContainsCalls);
		Assert.False(state.Contains(new ItemPosition(0, 0)));
		Assert.True(state.Contains(new ItemPosition(0, 5000)));
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(0, 4999)]
	[InlineData(4999, 0)]
	[InlineData(-1, -1)]
	public void ItemPositionHashMatchesItsValueEquality(int section, int item)
	{
		var first = new ItemPosition(section, item);
		var equal = new ItemPosition(section, item);
		Assert.True(first.Equals(equal));
		Assert.Equal(HashCode.Combine(section, item), first.GetHashCode());
		Assert.Equal(first.GetHashCode(), equal.GetHashCode());
		Assert.Contains(equal, new HashSet<ItemPosition> { first });
	}

	public sealed class SelectionModel
	{
		public IList<ItemPosition> Items { get; set; }
	}

	sealed class ObservedPositions(IEnumerable<ItemPosition> source) : List<ItemPosition>(source), IList<ItemPosition>
	{
		public int Enumerations { get; private set; }
		public int Reads { get; private set; }
		public int ContainsCalls { get; private set; }
		bool ICollection<ItemPosition>.Contains(ItemPosition item)
		{
			ContainsCalls++;
			return Contains(item);
		}
		IEnumerator<ItemPosition> IEnumerable<ItemPosition>.GetEnumerator()
		{
			Enumerations++;
			using var iterator = base.GetEnumerator();
			while (iterator.MoveNext())
			{
				Reads++;
				yield return iterator.Current;
			}
		}
	}

	sealed class SingleUsePositions(IEnumerable<ItemPosition> source) : IEnumerable<ItemPosition>
	{
		public int Enumerations { get; private set; }
		public int Reads { get; private set; }
		public IEnumerator<ItemPosition> GetEnumerator()
		{
			if (++Enumerations != 1)
				throw new InvalidOperationException("The input can only be enumerated once.");
			return Read().GetEnumerator();
		}
		IEnumerable<ItemPosition> Read()
		{
			foreach (var item in source)
			{
				Reads++;
				yield return item;
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
