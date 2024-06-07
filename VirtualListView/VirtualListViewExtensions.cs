#nullable enable
using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal static class VirtualListViewExtensions
{
	public static object? DataFor(this IVirtualListViewAdapter vlva, PositionKind kind, int sectionIndex, int itemIndex)
		=> kind switch
		{
			PositionKind.Item => vlva.GetItem(sectionIndex, itemIndex),
			PositionKind.SectionHeader => vlva.GetSection(sectionIndex),
			PositionKind.SectionFooter => vlva.GetSection(sectionIndex),
			_ => default
		};
}
