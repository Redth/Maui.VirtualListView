using Microsoft.Maui.Adapters;

namespace VirtualListView.Tests;

sealed class MutableAdapter : VirtualListViewAdapterBase<object, object>, IDisposable
{
	public int[] Counts { get; set; }
	public object[] Sections { get; set; }
	public int SectionCountReads { get; private set; }
	public int ItemCountReads { get; private set; }
	public int SectionReads { get; private set; }
	public int ItemReads { get; private set; }
	public int Invalidations { get; private set; }
	public bool NotifyOnInvalidation { get; set; } = true;
	public bool IsDisposed { get; private set; }

	public MutableAdapter(params int[] counts)
	{
		Counts = counts;
		Sections = counts.Select(_ => new object()).ToArray();
	}

	public override int GetNumberOfSections()
	{
		SectionCountReads++;
		return Counts.Length;
	}

	public override int GetNumberOfItemsInSection(int sectionIndex)
	{
		ItemCountReads++;
		return Counts[sectionIndex];
	}

	public override object GetSection(int sectionIndex)
	{
		SectionReads++;
		return Sections[sectionIndex];
	}

	public override object GetItem(int sectionIndex, int itemIndex)
	{
		ItemReads++;
		return (sectionIndex, itemIndex);
	}

	public override void InvalidateData()
	{
		Invalidations++;
		if (NotifyOnInvalidation)
			base.InvalidateData();
	}

	public void Dispose() => IsDisposed = true;
}

sealed class CachedAdapter : CachingVirtualListViewAdapter<object, object>
{
	public CachedAdapter(IVirtualListViewAdapter source, int capacity = 1000) : base(source, capacity)
	{
	}
}

sealed class TestListView : Microsoft.Maui.Controls.VirtualListView, IVirtualListView
{
	public IVirtualListViewSelector Selector { get; set; }
	IVirtualListViewSelector IVirtualListView.ViewSelector => Selector ?? this;
}

sealed class CountingSelector : IVirtualListViewSelector
{
	public Func<int, bool> Header { get; set; } = _ => false;
	public Func<int, bool> Footer { get; set; } = _ => false;
	public int HeaderReads { get; private set; }
	public int FooterReads { get; private set; }

	public bool SectionHasHeader(int sectionIndex)
	{
		HeaderReads++;
		return Header(sectionIndex);
	}

	public bool SectionHasFooter(int sectionIndex)
	{
		FooterReads++;
		return Footer(sectionIndex);
	}

	public IView CreateView(PositionInfo position, object data) => throw new NotSupportedException();
	public void RecycleView(PositionInfo position, object data, IView view) => throw new NotSupportedException();
	public string GetReuseId(PositionInfo position, object data) => throw new NotSupportedException();
}
