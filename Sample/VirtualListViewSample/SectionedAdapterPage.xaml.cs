using System.Collections.ObjectModel;

namespace VirtualListViewSample;

public partial class SectionedAdapterPage : ContentPage
{
	public SectionedAdapterPage()
	{
		InitializeComponent();

		Adapter = new SectionedAdapter(Sections);

		var rnd = new Random();

		for (int i = 0; i < 5; i++)
		{
			for (int j = 1; j <= rnd.Next(1, 7); j++)
			{
				Adapter.AddItem($"Section {i}", $"Item {j}");
			}
		}

		vlv.Adapter = Adapter;
	}


	public SectionedAdapter Adapter { get; set; }
	public ObservableCollection<Section> Sections = new();

	private void Button_Clicked(object sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(entrySection.Text) && !string.IsNullOrEmpty(entryItem.Text))
		{
#if IOS || MACCATALYST
			if (vlv.Handler.PlatformView is UIKit.UICollectionView cv && vlv.Handler is VirtualListViewHandler vlvHandler)
			{
				var h = 40;
				if (int.TryParse(entryItemSize.Text, out var p))
					h = p;
				
				var itemPosition = Adapter.AddItem(entrySection.Text, entryItem.Text, h);
				Adapter.InvalidateData();
				
				var indexPath = Foundation.NSIndexPath.FromItemSection(vlvHandler.PositionalViewSelector.GetPosition(itemPosition.SectionIndex, itemPosition.ItemIndex), 0);

				cv.InsertItems(new []{ indexPath });
				
				entryItem.Text = string.Empty;
			}
			#endif
		}
	}

	private void vlv_SelectedItemsChanged(object sender, SelectedItemsChangedEventArgs e)
	{
		if (e.NewSelection.Any())
		{
			var item = e.NewSelection.First();

			#if IOS || MACCATALYST
			if (vlv.Handler.PlatformView is UIKit.UICollectionView cv && vlv.Handler is VirtualListViewHandler vlvHandler)
			{
				var indexPath = Foundation.NSIndexPath.FromItemSection(vlvHandler.PositionalViewSelector.GetPosition(item.SectionIndex, item.ItemIndex), 0);
				Adapter.RemoveItem(item.SectionIndex, item.ItemIndex);
				Adapter.InvalidateData();
				
				cv.DeleteItems(new []{ indexPath });
			}
			#endif
			//vlv.DeleteItems(new [] { new ItemPosition(item.SectionIndex, item.ItemIndex)});
			vlv.ClearSelectedItems();
		}
	
	}
}
