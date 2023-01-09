using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Microsoft.Maui;

internal static partial class VirtualListViewExtensions
{
    public static T? GetFirstDescendant<T>(this DependencyObject element) where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(element, i);

            if ((child as T ?? GetFirstDescendant<T>(child)) is T target)
                return target;
        }
        return null;
    }
}
