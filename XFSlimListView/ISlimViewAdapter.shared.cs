using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms;

namespace XFSlimListView
{
	public interface ISlimListViewAdapter
	{
		int Sections { get; }

		object Section(int sectionIndex);

		int ItemsForSection(int sectionIndex);

		object Item(int sectionIndex, int itemIndex);
	}

	public class ObservableListViewAdapter<T> : ISlimListViewAdapter, IDisposable
	{
		public ObservableListViewAdapter(ObservableCollection<T> source)
		{
			Source = source;

			Source.CollectionChanged += Source_CollectionChanged;
		}

		public event EventHandler OnDataUpdated;

		void Source_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
			=> OnDataUpdated?.Invoke(this, new EventArgs());

		public ObservableCollection<T> Source { get; }

		public int ItemCount =>
			Source.Count;

		public int Sections => 1;

		public object Section(int sectionIndex)
			=> null;

		public object Item(int sectionIndex, int itemIndex)
			=> Source[itemIndex];

		public int ItemsForSection(int sectionIndex)
			=> ItemCount;

		public void Dispose()
			=> Source.CollectionChanged -= Source_CollectionChanged;
	}

	public class GroupedListViewAdapter<TGroup, TItem> : ISlimListViewAdapter where TGroup : IList<TItem> where TItem : class
	{
		public List<TGroup> Source { get; } = new List<TGroup>();

		public int Sections
			=> Source?.Count ?? 0;

		public int ItemCount
			=> Source?.Sum(s => s.Count) ?? 0;

		public object Section(int sectionIndex)
			=> Source == default ? null : Source.ElementAtOrDefault(sectionIndex);

		public object Item(int sectionIndex, int itemIndex)
			=> Source?[sectionIndex]?[itemIndex];

		public int ItemsForSection(int sectionIndex)
			=> Source?[sectionIndex]?.Count ?? 0;
	}

	public abstract class AdapterItemDataTemplateSelector
	{
		public abstract DataTemplate SelectItemTemplate(ISlimListViewAdapter adapter, int sectionIndex, int itemIndex);
	}

	public abstract class AdapterSectionDataTemplateSelector
	{
		public abstract DataTemplate SelectGroupTemplate(ISlimListViewAdapter adapter, int sectionIndex);
	}
}
