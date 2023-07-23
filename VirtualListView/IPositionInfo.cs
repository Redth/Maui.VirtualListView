namespace Microsoft.Maui;

public interface IPositionInfo
{
	bool IsSelected { get; set; }

	PositionKind Kind { get; set; }

	int SectionIndex { get; set; }

	int NumberOfSections { get; set; }

	int ItemIndex { get; set; }
	int ItemsInSection { get; set; }

	public bool IsLastItemInSection => ItemIndex >= ItemsInSection - 1;
	public bool IsNotLastItemInSection => !IsLastItemInSection;
	public bool IsFirstItemInSection => ItemIndex == 0;
	public bool IsNotFirstItemInSection => !IsFirstItemInSection;

	public bool IsSectionHeader => Kind == PositionKind.SectionHeader;
	public bool IsSectionFooter => Kind == PositionKind.SectionFooter;

	public bool IsGlobalHeader => Kind == PositionKind.Header;
	public bool IsGlobalFooter => Kind == PositionKind.Footer;
}
