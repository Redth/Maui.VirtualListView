using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal interface IPositionalViewSelector
{
    IVirtualListViewAdapter Adapter { get; }
    IVirtualListViewSelector ViewSelector { get; }
    bool HasGlobalHeader { get; }
    bool HasGlobalFooter { get; }
    int TotalCount { get; }
    int GetPosition(int sectionIndex, int itemIndex);
    PositionInfo GetInfo(int position);
}