using System.Collections.ObjectModel;
using Microsoft.Maui.Adapters;
using View = Microsoft.Maui.Controls.ContentView;

namespace VirtualListView.Tests;

public class AdapterCacheTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void WrapperInvalidationClearsEveryCacheAndNotifiesOnce(bool sourceNotifies)
	{
		var source = new MutableAdapter(1) { NotifyOnInvalidation = sourceNotifies };
		using var cache = new CachedAdapter(source);
		Assert.Equal(1, cache.GetNumberOfSections());
		Assert.Equal(1, cache.GetNumberOfItemsInSection(0));
		var oldSection = cache.GetSection(0);
		Assert.Same(oldSection, cache.GetSection(0));
		Assert.Equal(1, source.SectionReads);

		source.Counts = new[] { 3, 2 };
		source.Sections = new[] { new object(), new object() };
		var events = 0;
		cache.OnDataInvalidated += (_, _) =>
		{
			events++;
			Assert.Equal(2, cache.GetNumberOfSections());
			Assert.Equal(3, cache.GetNumberOfItemsInSection(0));
			Assert.Same(source.Sections[0], cache.GetSection(0));
		};
		cache.InvalidateData();
		Assert.Equal(1, source.Invalidations);
		Assert.Equal(1, events);
		Assert.NotSame(oldSection, cache.GetSection(0));
		Assert.Equal(2, source.SectionCountReads);
		Assert.Equal(2, source.ItemCountReads);
		Assert.Equal(2, source.SectionReads);
	}

	[Fact]
	public void SourceEventsClearCachesWithoutRecursivelyInvalidatingTheSource()
	{
		var source = new MutableAdapter(1);
		using var cache = new CachedAdapter(source);
		cache.GetNumberOfSections();
		cache.GetNumberOfItemsInSection(0);
		cache.GetSection(0);
		source.Counts = new[] { 4, 0 };
		source.Sections = new[] { new object(), new object() };
		var events = 0;
		cache.OnDataInvalidated += (_, _) =>
		{
			events++;
			Assert.Equal(2, cache.GetNumberOfSections());
			Assert.Equal(4, cache.GetNumberOfItemsInSection(0));
			Assert.Same(source.Sections[0], cache.GetSection(0));
			cache.InvalidateData();
		};
		source.InvalidateData();
		Assert.Equal(1, events);
		Assert.Equal(1, source.Invalidations);
	}

	[Fact]
	public void ForwardedSourceFailuresPropagateAndDoNotPermanentlySuppressEvents()
	{
		var source = new MutableAdapter(1);
		using var cache = new CachedAdapter(source);
		var wrapperEvents = 0;
		cache.OnDataInvalidated += (_, _) => wrapperEvents++;
		EventHandler failure = (_, _) => throw new InvalidOperationException("Source invalidation failed.");
		source.OnDataInvalidated += failure;
		Assert.Throws<InvalidOperationException>(() => cache.InvalidateData());
		Assert.Equal(0, wrapperEvents);
		source.OnDataInvalidated -= failure;
		source.Counts = new[] { 4, 0 };
		source.InvalidateData();
		Assert.Equal(1, wrapperEvents);
		Assert.Equal(2, cache.GetNumberOfSections());
		Assert.Equal(4, cache.GetNumberOfItemsInSection(0));
	}

	[Fact]
	public void NestedWrappersPublishOneInvalidationEach()
	{
		var source = new MutableAdapter(1);
		using var inner = new CachedAdapter(source);
		using var outer = new CachedAdapter(inner);
		var innerEvents = 0;
		var outerEvents = 0;
		inner.OnDataInvalidated += (_, _) => innerEvents++;
		outer.OnDataInvalidated += (_, _) => outerEvents++;
		outer.InvalidateData();
		Assert.Equal(1, source.Invalidations);
		Assert.Equal(1, innerEvents);
		Assert.Equal(1, outerEvents);
		source.InvalidateData();
		Assert.Equal(2, source.Invalidations);
		Assert.Equal(2, innerEvents);
		Assert.Equal(2, outerEvents);
	}

	[Fact]
	public void WrappedCollectionEventsRebuildProductionProjection()
	{
		var items = new ObservableCollection<object>();
		using var source = new ObservableCollectionAdapter<object>(items);
		using var cache = new CachedAdapter(source);
		var projection = new PositionalViewSelector(new TestListView
		{
			Adapter = cache, GlobalHeader = new View(), GlobalFooter = new View()
		});
		cache.OnDataInvalidated += (_, _) => projection.Invalidate();
		Assert.False(projection.HasItems);
		items.Add(new object());
		Assert.True(projection.HasItems);
		Assert.Equal(3, projection.TotalCount);
		items.Add(new object());
		Assert.Equal(4, projection.TotalCount);
		items.Clear();
		Assert.False(projection.HasItems);
		Assert.Equal(0, projection.TotalCount);
	}

	[Fact]
	public void DisposeDetachesOnlyThisWrapperAndNeverOwnsSourceDisposal()
	{
		var source = new MutableAdapter(1);
		var cache = new CachedAdapter(source);
		using var otherCache = new CachedAdapter(source);
		var events = 0;
		var otherEvents = 0;
		cache.OnDataInvalidated += (_, _) => events++;
		otherCache.OnDataInvalidated += (_, _) => otherEvents++;
		cache.Dispose();
		cache.Dispose();
		Assert.False(source.IsDisposed);
		source.InvalidateData();
		Assert.Equal(0, events);
		Assert.Equal(1, otherEvents);
		Assert.Throws<ObjectDisposedException>(() => cache.InvalidateData());
	}

	[Fact]
	public void CacheRetainsBoundedLruBehavior()
	{
		var source = new MutableAdapter(1, 2, 3);
		using var cache = new CachedAdapter(source, 2);
		cache.GetSection(0);
		cache.GetSection(1);
		cache.GetSection(0);
		cache.GetSection(2);
		cache.GetSection(1);
		Assert.Equal(4, source.SectionReads);
		cache.GetNumberOfItemsInSection(0);
		cache.GetNumberOfItemsInSection(1);
		cache.GetNumberOfItemsInSection(0);
		cache.GetNumberOfItemsInSection(2);
		cache.GetNumberOfItemsInSection(1);
		Assert.Equal(4, source.ItemCountReads);
	}

	[Fact]
	public void ConstructorRejectsInvalidSourceAndCapacity()
	{
		Assert.Throws<ArgumentNullException>(() => new CachedAdapter(null));
		Assert.Throws<ArgumentOutOfRangeException>(() => new CachedAdapter(new MutableAdapter(1), 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => new CachedAdapter(new MutableAdapter(1), -1));
	}
}
