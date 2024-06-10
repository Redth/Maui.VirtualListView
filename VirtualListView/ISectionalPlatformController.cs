namespace Microsoft.Maui;

internal interface ISectionalPlatformController
{
    PositionInfo GetInfo(int sectionIndex, int itemIndex);
}