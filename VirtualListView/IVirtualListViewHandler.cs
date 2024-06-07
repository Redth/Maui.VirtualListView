namespace Microsoft.Maui;

public interface IVirtualListViewHandler
{
#if ANDROID || IOS || MACCATALYST || WINDOWS
	IReadOnlyList<IPositionInfo> FindVisiblePositions();
#endif
}
