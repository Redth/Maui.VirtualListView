namespace Microsoft.Maui
{
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

	}

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
}
