using System.Collections.ObjectModel;

namespace Microsoft.Maui.Adapters;

public class ObservableCollectionAdapter<TItem> : VirtualListViewAdapterBase<object, TItem>, IDisposable
	where TItem : class
{
	public ObservableCollectionAdapter(ObservableCollection<TItem> items)
	{
		this.Items = items;
		this.Items.CollectionChanged += CollectionChanged;
	}

	protected readonly ObservableCollection<TItem> Items;

	bool disposedValue;

	void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		=> ((IVirtualListViewAdapter)this).InvalidateData();

	public override TItem GetItem(int sectionIndex, int itemIndex)
		=> Items[itemIndex];

	public override int GetNumberOfItemsInSection(int sectionIndex)
		=> Items.Count;

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				Items.CollectionChanged -= CollectionChanged;
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
