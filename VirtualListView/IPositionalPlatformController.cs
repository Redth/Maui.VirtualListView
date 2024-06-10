namespace Microsoft.Maui;

internal interface IPositionalPlatformController
{
    int TotalCount { get; }
    int GetPosition(int sectionIndex, int itemIndex);
    PositionInfo GetInfo(int position);
}