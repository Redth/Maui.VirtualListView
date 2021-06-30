using System;

namespace Microsoft.Maui
{
	public interface IVirtualListViewSelector
	{
		IView ViewForItem(int sectionIndex, int itemIndex);

		IView ViewForSectionHeader(int sectionIndex);

		bool SectionHasHeader(int sectionIndex);

		IView ViewForSectionFooter(int sectionIndex);

		bool SectionHasFooter(int sectionIndex);

		string ReuseIdIdForItem(int sectionIndex, int itemIndex);

		string ReuseIdIdForSectionHeader(int sectionIndex);

		string ReuseIdIdForSectionFooter(int sectionIndex);
	}

	public static class VirtualListViewExtensions
	{
		public static IView ViewFor(this IVirtualListViewSelector vlvs,  PositionKind kind, int sectionIndex, int itemIndex)
			=> kind switch
			{
				PositionKind.Item => vlvs.ViewForItem(sectionIndex, itemIndex),
				PositionKind.SectionHeader => vlvs.ViewForSectionHeader(sectionIndex),
				PositionKind.SectionFooter => vlvs.ViewForSectionFooter(sectionIndex),
				_ => default
			};

		public static object DataFor(this IVirtualListViewAdapter vlva, PositionKind kind, int sectionIndex, int itemIndex)
			=> kind switch
			{
				PositionKind.Item => vlva.Item(sectionIndex, itemIndex),
				PositionKind.SectionHeader => vlva.Section(sectionIndex),
				PositionKind.SectionFooter => vlva.Section(sectionIndex),
				_ => default
			};
	}
}
