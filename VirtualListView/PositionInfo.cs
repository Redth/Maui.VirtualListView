using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	public record PositionInfo
	{
		public static PositionInfo ForHeader()
			=> new () { Kind = PositionKind.Header };

		public static PositionInfo ForFooter()
			=> new() { Kind = PositionKind.Footer };

		public static PositionInfo ForSectionHeader(int sectionIndex, int itemsInSection = 0)
			=> new()
			{
				Kind = PositionKind.SectionHeader,
				ItemsInSection = itemsInSection
			};
		public static PositionInfo ForSectionFooter(int sectionIndex, int itemsInSection = 0)
			=> new()
			{
				Kind = PositionKind.SectionFooter,
				ItemsInSection = itemsInSection
			};

		public static PositionInfo ForItem(int sectionIndex, int itemIndex, bool selected = false, int numberOfSections = -1)
			=> new()
			{
				Kind = PositionKind.Item,
				SectionIndex = sectionIndex,
				ItemIndex = itemIndex,
				NumberOfSections = numberOfSections,
				IsSelected = selected
			};

		internal int Position { get; } = -1;

		public PositionKind Kind { get; init; } = PositionKind.Item;

		public int SectionIndex { get; internal set; } = -1;

		public int NumberOfSections { get; set; } = 0;

		public int ItemIndex { get; set; } = -1;

		public int ItemsInSection { get; set; } = 0;

		public bool IsSelected { get; set; } = false;
	}
}
