namespace Microsoft.Maui;

public static class PositionInfoExtensions
{
	public static void Update(this IPositionInfo positionInfo, IPositionInfo newPositionInfo)
	{
		positionInfo.SectionIndex = newPositionInfo.SectionIndex;
		positionInfo.ItemsInSection = newPositionInfo.ItemsInSection;
		positionInfo.IsSelected = newPositionInfo.IsSelected;
		positionInfo.ItemIndex = newPositionInfo.ItemIndex;
		positionInfo.NumberOfSections = newPositionInfo.NumberOfSections;
		positionInfo.Kind = newPositionInfo.Kind;

		if (positionInfo is PositionInfo p && newPositionInfo is PositionInfo np)
		{
			p.Position = np.Position;
			p.ReuseId = np.ReuseId;
		}
	}
}

