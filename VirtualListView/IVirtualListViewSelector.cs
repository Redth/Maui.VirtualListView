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

		string GetReuseId(PositionKind kind, int sectionIndex, int itemIndex);
	}
}
