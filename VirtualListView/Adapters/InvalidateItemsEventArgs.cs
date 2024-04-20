namespace Microsoft.Maui.Adapters;

public class InvalidateItemsEventArgs : EventArgs
{
	public InvalidateItemsEventArgs(params ItemPosition[] items)
	{
		Items = items;
	}

	public ItemPosition[] Items { get; }
}