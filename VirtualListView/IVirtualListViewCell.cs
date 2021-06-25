using System.Collections.Generic;
using System.Text;

namespace Microsoft.Maui
{
	public interface IVirtualListViewCell<TTemplateView> : IView where TTemplateView : IView
	{
		int SectionIndex { get; }
		int ItemIndex { get; }
		bool IsGlobalHeader { get; }
		bool IsGlobalFooter { get; }
		bool IsSectionHeader { get; }
		bool IsSectionFooter { get; }
		bool IsItem { get; }
		bool IsLastItemInSection { get; }
		bool IsNotLastItemInSection { get; }
		bool IsFirstItemInSection { get; }
		bool IsNotFirstItemInSection { get; }
		PositionKind PositionKind { get; }
		bool IsSelected { get; }
	}
}
