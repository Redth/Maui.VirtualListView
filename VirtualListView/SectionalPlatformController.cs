#nullable enable

using Microsoft.Maui.Adapters;

namespace Microsoft.Maui;

internal class SectionalPlatformController : ISectionalPlatformController
{
    public readonly IVirtualListViewHandler Handler;
    public IVirtualListViewAdapter Adapter => Handler?.Adapter ?? new EmptyAdapter();
    public IVirtualListViewSelector ViewSelector => Handler?.ViewSelector ?? new EmptyViewSelector();

    public bool HasGlobalHeader =>
        (Handler.VirtualView?.IsHeaderVisible ?? false)
        && (Handler.VirtualView.Header?.Visibility ?? Visibility.Collapsed) == Visibility.Visible;

    public bool HasGlobalFooter =>
        (Handler.VirtualView?.IsFooterVisible ?? false)
        && (Handler.VirtualView?.Footer?.Visibility ?? Visibility.Collapsed) == Visibility.Visible;

    public SectionalPlatformController(IVirtualListViewHandler virtualListViewHandler)
    {
        Handler = virtualListViewHandler;
    }

    public PositionInfo GetInfo(int sectionIndex, int itemIndex)
    {
        var numberOfSections = Adapter.GetNumberOfSections();
        var itemsInSection = Adapter.GetNumberOfItemsInSection(sectionIndex);
        
        return PositionInfo.ForItem(sectionIndex, itemIndex, itemsInSection, numberOfSections);
    }
}