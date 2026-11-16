using SelectionMode = Microsoft.Maui.SelectionMode;

namespace VirtualListView.Tests;

public class RowAccessibilityTests
{
	static PositionInfo Item(bool selected = false) => PositionInfo.ForItem(3, 0, 1, 4, 1, selected);

	public static TheoryData<PositionInfo, bool> Rows => new()
	{
		{ Item(), true },
		{ PositionInfo.ForSectionHeader(0, 0, 4), true },
		{ PositionInfo.ForSectionFooter(5, 0, 4), true },
		// Global chrome is page content whose controls stay individually reachable.
		{ PositionInfo.ForHeader(0), false },
		{ PositionInfo.ForFooter(9), false },
		{ PositionInfo.ForEmptyView(1), false },
	};

	[Theory]
	[MemberData(nameof(Rows))]
	public void OnlyItemsAndSectionChromeArePresentedAsRows(PositionInfo position, bool isRow)
		=> Assert.Equal(isRow, RowAccessibility.IsRow(position));

	[Fact]
	public void UnboundRowsArePlainContent()
	{
		Assert.False(RowAccessibility.IsRow(null));
		Assert.False(RowAccessibility.IsHeading(null));
		Assert.False(RowAccessibility.IsSelectable(null, SelectionMode.Multiple));
		Assert.False(RowAccessibility.IsSelected(null, SelectionMode.Multiple));
	}

	[Fact]
	public void OnlySectionHeadersAreHeadings()
	{
		Assert.True(RowAccessibility.IsHeading(PositionInfo.ForSectionHeader(0, 0)));
		Assert.False(RowAccessibility.IsHeading(PositionInfo.ForSectionFooter(5, 0)));
		Assert.False(RowAccessibility.IsHeading(PositionInfo.ForHeader(0)));
		Assert.False(RowAccessibility.IsHeading(PositionInfo.ForFooter(9)));
		Assert.False(RowAccessibility.IsHeading(Item()));
	}

	[Theory]
	[InlineData(SelectionMode.None, false)]
	[InlineData(SelectionMode.Single, true)]
	[InlineData(SelectionMode.Multiple, true)]
	public void ItemsAreSelectableOnlyWhileTheListAllowsSelection(SelectionMode mode, bool selectable)
	{
		Assert.Equal(selectable, RowAccessibility.IsSelectable(Item(), mode));
		Assert.False(RowAccessibility.IsSelectable(PositionInfo.ForSectionHeader(0, 0), mode));
		Assert.False(RowAccessibility.IsSelectable(PositionInfo.ForHeader(0), mode));
	}

	[Fact]
	public void SelectedStateIsNotReportedWhileSelectionIsOff()
	{
		Assert.True(RowAccessibility.IsSelected(Item(selected: true), SelectionMode.Single));
		Assert.False(RowAccessibility.IsSelected(Item(selected: false), SelectionMode.Single));
		Assert.False(RowAccessibility.IsSelected(Item(selected: true), SelectionMode.None));
	}

	[Fact]
	public void LabelsJoinNonBlankPartsInOrder()
	{
		Assert.Equal("Inbox, 3 unread", RowAccessibility.ComposeLabel(new[] { " Inbox ", null, "", "  ", "3 unread" }));
		Assert.Null(RowAccessibility.ComposeLabel(new string[] { null, " " }));
		Assert.Null(RowAccessibility.ComposeLabel(Array.Empty<string>()));
	}
}
