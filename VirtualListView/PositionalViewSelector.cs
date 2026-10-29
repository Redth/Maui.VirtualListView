using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal class PositionalViewSelector
{
	public readonly IVirtualListView VirtualListView;
	public IVirtualListViewAdapter Adapter => acceptedAdapter ?? VirtualListView?.Adapter;
	public IVirtualListViewSelector ViewSelector => VirtualListView?.ViewSelector;

	IVirtualListViewAdapter acceptedAdapter;
	Projection projection;

	public PositionalViewSelector(IVirtualListView virtualListView, IVirtualListViewAdapter acceptedAdapter = null)
	{
		VirtualListView = virtualListView;
		this.acceptedAdapter = acceptedAdapter
			?? (virtualListView.Adapter as ISnapshotVirtualListViewAdapter)?.CaptureSnapshot();
		Invalidate();
	}

	internal void Accept(PositionalViewSelector next)
	{
		acceptedAdapter = next.acceptedAdapter;
		projection = next.projection;
	}

	public int TotalCount => projection.TotalCount;
	public bool HasItems => projection.HasItems;
	public bool HasGlobalHeader => projection.HasGlobalHeader;
	public bool HasGlobalFooter => projection.HasGlobalFooter;

	// Publish only complete metadata. Native count/bind/seek calls never rescan the source.
	public void Invalidate()
		=> projection = CreateProjection();

	internal void ValidateItemRefresh()
	{
		var next = CreateProjection();
		if (projection.TotalCount != next.TotalCount || projection.HasGlobalHeader != next.HasGlobalHeader
			|| projection.HasGlobalFooter != next.HasGlobalFooter || !projection.Sections.SequenceEqual(next.Sections))
			throw new InvalidOperationException("Item refresh cannot change counts or chrome. Publish structural changes with InvalidateData first.");
	}

	Projection CreateProjection()
	{
		var adapter = Adapter;
		if (adapter is null)
			return new Projection(Array.Empty<Section>(), false, false, false, 0);

		var sectionCount = adapter.GetNumberOfSections();
		if (sectionCount < 0)
			throw new InvalidOperationException("The adapter returned a negative section count.");

		var sections = new Section[sectionCount];
		var selector = ViewSelector;
		var hasItems = false;
		var position = 0;
		for (var s = 0; s < sections.Length; s++)
		{
			var count = adapter.GetNumberOfItemsInSection(s);
			if (count < 0)
				throw new InvalidOperationException($"The adapter returned a negative item count for section {s}.");

			var hasHeader = selector?.SectionHasHeader(s) ?? false;
			var hasFooter = selector?.SectionHasFooter(s) ?? false;
			var end = checked(position + count + (hasHeader ? 1 : 0) + (hasFooter ? 1 : 0));
			sections[s] = new Section(position, end, count, hasHeader, hasFooter);
			position = end;
			hasItems |= count > 0;
		}

		// Preserve empty-group chrome, but suppress a global header without data and
		// a global footer without any projected section content.
		var globalHeader = hasItems && VirtualListView.IsHeaderVisible
			&& VirtualListView.Header?.Visibility == Visibility.Visible;
		var globalFooter = position > 0 && VirtualListView.IsFooterVisible
			&& VirtualListView.Footer?.Visibility == Visibility.Visible;
		var total = checked(position + (globalHeader ? 1 : 0) + (globalFooter ? 1 : 0));
		return new Projection(sections, hasItems, globalHeader, globalFooter, total);
	}

	public int GetPosition(int sectionIndex, int itemIndex)
	{
		var snapshot = projection;
		if (sectionIndex < 0 || sectionIndex >= snapshot.Sections.Length)
			return -1;

		var section = snapshot.Sections[sectionIndex];
		if (itemIndex < 0 || itemIndex >= section.ItemCount)
			return -1;

		return section.Start + (snapshot.HasGlobalHeader ? 1 : 0)
			+ (section.HasHeader ? 1 : 0) + itemIndex;
	}

	public PositionInfo GetInfo(int position)
	{
		var snapshot = projection;
		if (position < 0 || position >= snapshot.TotalCount)
			return null;

		if (snapshot.HasGlobalHeader && position == 0)
			return PositionInfo.ForHeader(position);

		if (snapshot.HasGlobalFooter && position == snapshot.TotalCount - 1)
			return PositionInfo.ForFooter(position);

		var sectionPosition = position - (snapshot.HasGlobalHeader ? 1 : 0);
		var low = 0;
		var high = snapshot.Sections.Length;
		// Find the first section ending after this position, skipping zero-row groups.
		while (low < high)
		{
			var middle = low + (high - low) / 2;
			if (snapshot.Sections[middle].End <= sectionPosition)
				low = middle + 1;
			else
				high = middle;
		}

		var section = snapshot.Sections[low];
		var offset = sectionPosition - section.Start;
		if (section.HasHeader && offset == 0)
			return PositionInfo.ForSectionHeader(position, low, section.ItemCount);

		var itemIndex = offset - (section.HasHeader ? 1 : 0);
		if (itemIndex < section.ItemCount)
			return PositionInfo.ForItem(position, low, itemIndex, section.ItemCount, snapshot.Sections.Length);

		return PositionInfo.ForSectionFooter(position, low, section.ItemCount);
	}

	readonly record struct Section(int Start, int End, int ItemCount, bool HasHeader, bool HasFooter);

	sealed record Projection(Section[] Sections, bool HasItems, bool HasGlobalHeader, bool HasGlobalFooter, int TotalCount);
}
