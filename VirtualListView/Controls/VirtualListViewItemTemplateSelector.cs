namespace Microsoft.Maui.Controls;

public abstract class VirtualListViewItemTemplateSelector
{
	public abstract DataTemplate SelectTemplate(object item, int sectionIndex, int itemIndex);
}

public abstract class VirtualListViewSectionTemplateSelector
{
	public abstract DataTemplate SelectTemplate(object section, int sectionIndex);
}
