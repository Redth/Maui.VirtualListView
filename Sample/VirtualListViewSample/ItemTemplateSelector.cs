using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using MauiCoreItemTemplateSelector = Microsoft.Maui.ItemTemplateSelector;

namespace VirtualListViewSample
{
	public class ItemTemplateSelector : MauiCoreItemTemplateSelector
	{
		public IViewTemplate PopTemplate { get; set; }
		//public DataTemplate HeavyTemplate { get; set; }
		public IViewTemplate FilmTemplate { get; set; }

		public IViewTemplate GenericTemplate { get; set; }

		public ItemTemplateSelector()
		{
			PopTemplate = new PopViewCell();
			GenericTemplate = new GenericViewCell();
			FilmTemplate = new FilmViewCell();
		}

		public override IViewTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
		{
			var item = adapter.Item(sectionIndex, itemIndex);

			if (item is TrackInfo trackInfo)
			{
				var genreId = trackInfo.GenreId;

				if (genreId == 9)
					return PopTemplate;
				//if (genreId == 1 || genreId == 3 || genreId == 4 || genreId == 5 || genreId == 13)
				//	return HeavyTemplate;
				else if (genreId == 10 || genreId == 19 || genreId == 18 || genreId == 21 || genreId == 22)
					return FilmTemplate;
			}

			return GenericTemplate;
		}
    }
}
