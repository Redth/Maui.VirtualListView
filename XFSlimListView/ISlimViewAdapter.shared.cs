using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

	public class ObservableListViewAdapter<T> : ISlimListViewAdapter
	{
		public ObservableCollection<T> Source { get; } = new ObservableCollection<T>();

		public int ItemCount =>
			Source.Count;

		public int Sections => 1;

		public object Section(int sectionIndex)
			=> null;

		public object Item(int sectionIndex, int itemIndex)
			=> Source[itemIndex];

		public int ItemsForSection(int sectionIndex)
			=> ItemCount;
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
