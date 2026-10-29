using View = Microsoft.Maui.Controls.ContentView;

namespace VirtualListView.Tests;

public class ProjectionTests
{
	[Fact]
	public void MissingAdapterAndZeroSectionsNeverProjectGlobalChrome()
	{
		var view = new TestListView { GlobalHeader = new View(), GlobalFooter = new View() };
		var projection = new PositionalViewSelector(view);
		AssertEmpty(projection);
		view.Adapter = new MutableAdapter();
		projection.Invalidate();
		AssertEmpty(projection);
	}

	[Fact]
	public void EmptyFirstSectionDoesNotHideLaterDataOrShiftItsMapping()
	{
		var projection = new PositionalViewSelector(new TestListView
		{
			Adapter = new MutableAdapter(0, 2),
			GlobalHeader = new View(),
			GlobalFooter = new View()
		});

		Assert.True(projection.HasItems);
		Assert.Equal(4, projection.TotalCount);
		Assert.Equal(PositionKind.Header, projection.GetInfo(0).Kind);
		Assert.Equal(1, projection.GetPosition(1, 0));
		Assert.Equal(2, projection.GetPosition(1, 1));
		Assert.Equal(1, projection.GetInfo(1).SectionIndex);
		Assert.Equal(2, projection.GetInfo(1).NumberOfSections);
		Assert.Equal(PositionKind.Footer, projection.GetInfo(3).Kind);
		Assert.Equal(-1, projection.GetPosition(0, 0));
	}

	[Fact]
	public void AllEmptyGroupsRetainSectionChromeButNeitherGlobalByDefault()
	{
		var view = new TestListView
		{
			Adapter = new MutableAdapter(0, 0),
			SectionHeaderTemplate = new DataTemplate(() => new View()),
			SectionFooterTemplate = new DataTemplate(() => new View()),
			GlobalHeader = new View(),
			GlobalFooter = new View()
		};
		var projection = new PositionalViewSelector(view);

		Assert.False(projection.HasItems);
		Assert.False(projection.HasGlobalHeader);
		Assert.False(projection.HasGlobalFooter);
		Assert.Equal(new[]
		{
			PositionKind.SectionHeader, PositionKind.SectionFooter,
			PositionKind.SectionHeader, PositionKind.SectionFooter
		}, Enumerable.Range(0, projection.TotalCount).Select(p => projection.GetInfo(p).Kind));
		view.SectionHeaderTemplate = null;
		view.SectionFooterTemplate = null;
		projection.Invalidate();
		AssertEmpty(projection);
	}

	[Fact]
	public void EmptyChromeSettingsAreIndependentFalseDefaultsAndNotify()
	{
		var view = new TestListView();
		Assert.True(view.IsHeaderVisible);
		Assert.True(view.IsFooterVisible);
		Assert.False(view.ShowHeaderWhenEmpty);
		Assert.False(view.ShowFooterWhenEmpty);
		var changes = new List<string>();
		view.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
		view.SetValue(Microsoft.Maui.Controls.VirtualListView.ShowHeaderWhenEmptyProperty, true);
		Assert.True(((IVirtualListView)view).ShowHeaderWhenEmpty);
		Assert.False(((IVirtualListView)view).ShowFooterWhenEmpty);
		view.SetValue(Microsoft.Maui.Controls.VirtualListView.ShowFooterWhenEmptyProperty, true);
		Assert.True(((IVirtualListView)view).ShowFooterWhenEmpty);
		Assert.Equal(new[] { nameof(view.ShowHeaderWhenEmpty), nameof(view.ShowFooterWhenEmpty) }, changes);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void EmptyChromeRequiresBothItsOptInAndExistingVisibilityRegardlessOfSectionChrome(int sections)
	{
		for (var flags = 0; flags < 64; flags++)
		{
			var view = new TestListView
			{
				Adapter = sections < 0 ? null : new MutableAdapter(new int[sections]),
				GlobalHeader = new View { IsVisible = (flags & 16) != 0 },
				GlobalFooter = new View { IsVisible = (flags & 32) != 0 },
				ShowHeaderWhenEmpty = (flags & 1) != 0,
				ShowFooterWhenEmpty = (flags & 2) != 0,
				IsHeaderVisible = (flags & 4) != 0,
				IsFooterVisible = (flags & 8) != 0,
				SectionHeaderTemplate = new DataTemplate(() => new View()),
				SectionFooterTemplate = new DataTemplate(() => new View()),
				EmptyView = new View()
			};
			var projection = new PositionalViewSelector(view);
			var header = (flags & 21) == 21;
			var footer = (flags & 42) == 42;
			Assert.False(projection.HasItems);
			Assert.Equal(header, projection.HasGlobalHeader);
			Assert.Equal(footer, projection.HasGlobalFooter);
			Assert.Equal(header || footer, projection.HasInlineEmptyView);
			var expected = new List<PositionKind>();
			if (header) expected.Add(PositionKind.Header);
			for (var s = 0; s < Math.Max(0, sections); s++)
				expected.AddRange(new[] { PositionKind.SectionHeader, PositionKind.SectionFooter });
			if (header || footer) expected.Add(PositionKind.EmptyView);
			if (footer) expected.Add(PositionKind.Footer);
			Assert.Equal(expected, Enumerable.Range(0, projection.TotalCount).Select(p => projection.GetInfo(p).Kind));
			Assert.Equal(-1, projection.GetPosition(0, 0));
			Assert.Null(projection.GetInfo(projection.TotalCount));
		}
	}

	[Fact]
	public void EmptyViewIsAnOptionalRowBetweenGlobalsOnlyWhileEmpty()
	{
		var source = new MutableAdapter(0);
		var view = new TestListView
		{
			Adapter = source, GlobalHeader = new View(), GlobalFooter = new View(),
			ShowHeaderWhenEmpty = true, ShowFooterWhenEmpty = true
		};
		var projection = new PositionalViewSelector(view);
		Assert.Equal(2, projection.TotalCount);
		Assert.False(projection.HasInlineEmptyView);
		view.EmptyView = new View();
		projection.Invalidate();
		Assert.Equal(new[] { PositionKind.Header, PositionKind.EmptyView, PositionKind.Footer },
			Enumerable.Range(0, projection.TotalCount).Select(p => projection.GetInfo(p).Kind));
		Assert.True(projection.HasInlineEmptyView);
		Assert.Equal(-1, projection.GetInfo(1).SectionIndex);
		Assert.Equal(-1, projection.GetInfo(1).ItemIndex);
		source.Counts = new[] { 0, 1 };
		projection.Invalidate();
		Assert.True(projection.HasItems);
		Assert.False(projection.HasInlineEmptyView);
		Assert.Equal(PositionKind.Item, projection.GetInfo(1).Kind);
		Assert.Equal(1, projection.GetPosition(1, 0));
		source.Counts = new[] { 0, 0 };
		projection.Invalidate();
		Assert.True(projection.HasInlineEmptyView);
		view.GlobalHeader = null;
		view.GlobalFooter = null;
		projection.Invalidate();
		AssertEmpty(projection);
	}

	[Fact]
	public void MixedChromeAndEmptyGroupsRoundTripAgainstSlowReference()
	{
		var random = new Random(7301);
		for (var iteration = 0; iteration < 300; iteration++)
		{
			var counts = Enumerable.Range(0, random.Next(0, 30)).Select(_ => random.Next(0, 6)).ToArray();
			var headers = counts.Select(_ => random.Next(2) == 1).ToArray();
			var footers = counts.Select(_ => random.Next(2) == 1).ToArray();
			var selector = new CountingSelector { Header = s => headers[s], Footer = s => footers[s] };
			var view = new TestListView
			{
				Adapter = new MutableAdapter(counts),
				Selector = selector,
				GlobalHeader = new View { IsVisible = random.Next(2) == 1 },
				GlobalFooter = new View { IsVisible = random.Next(2) == 1 },
				IsHeaderVisible = random.Next(2) == 1,
				IsFooterVisible = random.Next(2) == 1,
				ShowHeaderWhenEmpty = random.Next(2) == 1,
				ShowFooterWhenEmpty = random.Next(2) == 1,
				EmptyView = random.Next(2) == 1 ? new View() : null
			};
			var expected = new List<(PositionKind Kind, int Section, int Item)>();
			var hasItems = counts.Any(c => c > 0);
			var header = (hasItems || view.ShowHeaderWhenEmpty) && view.IsHeaderVisible && view.GlobalHeader.Visibility == Visibility.Visible;
			var footer = (hasItems || view.ShowFooterWhenEmpty) && view.IsFooterVisible && view.GlobalFooter.Visibility == Visibility.Visible;
			if (header)
				expected.Add((PositionKind.Header, -1, -1));
			for (var s = 0; s < counts.Length; s++)
			{
				if (headers[s])
					expected.Add((PositionKind.SectionHeader, s, -1));
				for (var i = 0; i < counts[s]; i++)
					expected.Add((PositionKind.Item, s, i));
				if (footers[s])
					expected.Add((PositionKind.SectionFooter, s, -1));
			}
			var inlineEmpty = !hasItems && (header || footer) && view.EmptyView is not null;
			if (inlineEmpty)
				expected.Add((PositionKind.EmptyView, -1, -1));
			if (footer)
				expected.Add((PositionKind.Footer, -1, -1));

			var projection = new PositionalViewSelector(view);
			Assert.Equal(hasItems, projection.HasItems);
			Assert.Equal(inlineEmpty, projection.HasInlineEmptyView);
			Assert.Equal(expected.Count, projection.TotalCount);
			for (var p = 0; p < expected.Count; p++)
			{
				var info = projection.GetInfo(p);
				Assert.Equal(expected[p], (info.Kind, info.SectionIndex, info.ItemIndex));
				Assert.Equal(p, info.Position);
				if (info.SectionIndex >= 0)
					Assert.Equal(counts[info.SectionIndex], info.ItemsInSection);
				if (info.Kind == PositionKind.Item)
				{
					Assert.Equal(p, projection.GetPosition(info.SectionIndex, info.ItemIndex));
					Assert.Equal(info.ItemIndex == 0, info.IsFirstItemInSection);
					Assert.Equal(info.ItemIndex == counts[info.SectionIndex] - 1, info.IsLastItemInSection);
				}
			}
			Assert.Null(projection.GetInfo(-1));
			Assert.Null(projection.GetInfo(expected.Count));
			Assert.Null(projection.GetInfo(int.MaxValue));
			Assert.Equal(-1, projection.GetPosition(-1, 0));
			Assert.Equal(-1, projection.GetPosition(counts.Length, 0));
			for (var s = 0; s < counts.Length; s++)
			{
				Assert.Equal(-1, projection.GetPosition(s, -1));
				Assert.Equal(-1, projection.GetPosition(s, counts[s]));
			}
		}
	}

	[Fact]
	public void MetadataIsStableUntilInvalidatedAndRebuildsForEveryInputKind()
	{
		var source = new MutableAdapter(2);
		var view = new TestListView { Adapter = source, GlobalHeader = new View(), GlobalFooter = new View() };
		var projection = new PositionalViewSelector(view);
		Assert.Equal(4, projection.TotalCount);

		source.Counts = new[] { 0, 3 };
		Assert.Equal(4, projection.TotalCount);
		Assert.Equal(2, projection.GetInfo(1).ItemsInSection);
		projection.Invalidate();
		Assert.Equal(5, projection.TotalCount);
		Assert.Equal(1, projection.GetInfo(1).SectionIndex);

		view.SectionHeaderTemplate = new DataTemplate(() => new View());
		view.SectionFooterTemplate = new DataTemplate(() => new View());
		projection.Invalidate();
		Assert.Equal(9, projection.TotalCount);
		Assert.Equal(4, projection.GetPosition(1, 0));

		view.IsHeaderVisible = false;
		view.GlobalFooter = null;
		projection.Invalidate();
		Assert.Equal(7, projection.TotalCount);
		Assert.Equal(3, projection.GetPosition(1, 0));

		view.GlobalHeader = new View { IsVisible = false };
		view.GlobalFooter = new View { IsVisible = false };
		view.IsHeaderVisible = true;
		projection.Invalidate();
		Assert.Equal(7, projection.TotalCount);
		((View)view.GlobalHeader).IsVisible = true;
		((View)view.GlobalFooter).IsVisible = true;
		projection.Invalidate();
		Assert.Equal(9, projection.TotalCount);
		view.IsFooterVisible = false;
		projection.Invalidate();
		Assert.Equal(8, projection.TotalCount);

		view.Selector = new CountingSelector();
		view.Adapter = new MutableAdapter(1);
		projection.Invalidate();
		Assert.Equal(2, projection.TotalCount);
		Assert.Equal(1, projection.GetPosition(0, 0));
		view.Adapter = null;
		projection.Invalidate();
		AssertEmpty(projection);
	}

	[Fact]
	public void HotLookupsDoNotRescanSectionsOrMaterializeItems()
	{
		const int sectionCount = 100_000;
		var source = new MutableAdapter(Enumerable.Repeat(100, sectionCount).ToArray());
		var selector = new CountingSelector { Header = _ => true, Footer = _ => true };
		var projection = new PositionalViewSelector(new TestListView { Adapter = source, Selector = selector });
		Assert.Equal(sectionCount * 102, projection.TotalCount);
		Assert.Equal(1, source.SectionCountReads);
		Assert.Equal(sectionCount, source.ItemCountReads);

		for (var s = 0; s < sectionCount; s += 7)
		{
			var position = projection.GetPosition(s, 99);
			var info = projection.GetInfo(position);
			Assert.Equal(s, info.SectionIndex);
			Assert.Equal(99, info.ItemIndex);
			Assert.Equal(sectionCount * 102, projection.TotalCount);
		}

		Assert.Equal(1, source.SectionCountReads);
		Assert.Equal(sectionCount, source.ItemCountReads);
		Assert.Equal(sectionCount, selector.HeaderReads);
		Assert.Equal(sectionCount, selector.FooterReads);
		Assert.Equal(0, source.ItemReads);
		Assert.Equal(0, source.SectionReads);
		projection.Invalidate();
		Assert.Equal(2, source.SectionCountReads);
		Assert.Equal(sectionCount * 2, source.ItemCountReads);
	}

	[Fact]
	public void HugeLogicalItemCountsUseOnlySectionMetadataAndCheckedTotals()
	{
		var view = new TestListView { Adapter = new MutableAdapter(int.MaxValue) };
		var projection = new PositionalViewSelector(view);
		Assert.Equal(int.MaxValue, projection.TotalCount);
		Assert.Equal(int.MaxValue - 1, projection.GetPosition(0, int.MaxValue - 1));
		Assert.Equal(int.MaxValue - 1, projection.GetInfo(int.MaxValue - 1).ItemIndex);
		view.GlobalHeader = new View();
		Assert.Throws<OverflowException>(() => projection.Invalidate());
		Assert.Equal(int.MaxValue, projection.TotalCount);
	}

	[Fact]
	public void InvalidSourceCountsAreRejectedWithoutPublishingPartialMetadata()
	{
		var source = new MutableAdapter(1);
		var projection = new PositionalViewSelector(new TestListView { Adapter = source });
		source.Counts = new[] { -1 };
		Assert.Throws<InvalidOperationException>(() => projection.Invalidate());
		Assert.Equal(1, projection.TotalCount);
		Assert.Equal(0, projection.GetInfo(0).ItemIndex);
		Assert.Throws<InvalidOperationException>(() => new PositionalViewSelector(new TestListView
		{
			Adapter = new NegativeSectionAdapter()
		}));
	}

	[Theory]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.GlobalHeader), nameof(IVirtualListView.Header))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.GlobalFooter), nameof(IVirtualListView.Footer))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.ItemTemplate), nameof(IVirtualListView.ViewSelector))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.ItemTemplateSelector), nameof(IVirtualListView.ViewSelector))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.SectionHeaderTemplate), nameof(IVirtualListView.ViewSelector))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.SectionFooterTemplate), nameof(IVirtualListView.ViewSelector))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.SectionHeaderTemplateSelector), nameof(IVirtualListView.ViewSelector))]
	[InlineData(nameof(Microsoft.Maui.Controls.VirtualListView.SectionFooterTemplateSelector), nameof(IVirtualListView.ViewSelector))]
	public void ControlPropertiesNotifyTheirHandlerFacingAliases(string propertyName, string alias)
	{
		var view = new TestListView();
		var notifications = new List<string>();
		view.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
		var property = view.GetType().GetProperty(propertyName);
		object value = propertyName switch
		{
			nameof(view.ItemTemplateSelector) => new ItemSelector(),
			nameof(view.SectionHeaderTemplateSelector) or nameof(view.SectionFooterTemplateSelector) => new SectionSelector(),
			nameof(view.GlobalHeader) or nameof(view.GlobalFooter) => new View(),
			_ => new DataTemplate(() => new View())
		};
		property.SetValue(view, value);
		Assert.Contains(alias, notifications);
		Assert.Equal(1, notifications.Count(n => n == alias));
	}

	static void AssertEmpty(PositionalViewSelector projection)
	{
		Assert.Equal(0, projection.TotalCount);
		Assert.False(projection.HasItems);
		Assert.False(projection.HasGlobalHeader);
		Assert.False(projection.HasGlobalFooter);
		Assert.False(projection.HasInlineEmptyView);
		Assert.Null(projection.GetInfo(0));
		Assert.Null(projection.GetInfo(-1));
		Assert.Equal(-1, projection.GetPosition(0, 0));
	}

	sealed class NegativeSectionAdapter : Microsoft.Maui.Adapters.VirtualListViewAdapterBase<object, object>
	{
		public override int GetNumberOfSections() => -1;
		public override int GetNumberOfItemsInSection(int sectionIndex) => throw new NotSupportedException();
		public override object GetItem(int sectionIndex, int itemIndex) => throw new NotSupportedException();
	}

	sealed class ItemSelector : VirtualListViewItemTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, int sectionIndex, int itemIndex) => new(() => new View());
	}

	sealed class SectionSelector : VirtualListViewSectionTemplateSelector
	{
		public override DataTemplate SelectTemplate(object section, int sectionIndex) => new(() => new View());
	}
}
