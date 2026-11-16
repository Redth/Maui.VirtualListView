#nullable enable
namespace Microsoft.Maui;

// How a list row presents itself to assistive technology. Each platform host maps this policy onto
// its native accessibility tree (UIKit accessibility elements, Android node info, UI Automation peers),
// reading the row's current PositionInfo when the screen reader asks rather than caching it, because
// keyed updates and recycling move positions underneath a native row without rebinding it.
internal static class RowAccessibility
{
	// Items and section headers/footers are presented as one element, the way native table rows are.
	// The global header, footer and empty view are arbitrary page content (search bars, banners, ...),
	// so their own controls stay individually reachable.
	public static bool IsRow(PositionInfo? position)
		=> position?.Kind is PositionKind.Item or PositionKind.SectionHeader or PositionKind.SectionFooter;

	public static bool IsHeading(PositionInfo? position)
		=> position?.Kind == PositionKind.SectionHeader;

	// Only data items take part in selection, and only while the list allows it.
	public static bool IsSelectable(PositionInfo? position, SelectionMode mode)
		=> position?.Kind == PositionKind.Item && mode != SelectionMode.None;

	public static bool IsSelected(PositionInfo? position, SelectionMode mode)
		=> IsSelectable(position, mode) && position!.IsSelected;

	// Joins the spoken parts of a row's content into one label, skipping blanks.
	public static string? ComposeLabel(IEnumerable<string?> parts)
	{
		var label = string.Join(", ", parts.Select(part => part?.Trim()).Where(part => !string.IsNullOrEmpty(part)));
		return label.Length == 0 ? null : label;
	}
}
