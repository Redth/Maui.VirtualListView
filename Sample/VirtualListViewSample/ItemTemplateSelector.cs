using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public class ItemTemplateSelector : ItemTemplateSelector
	{
		public IReplaceableView PopTemplate { get; set; }
		//public DataTemplate HeavyTemplate { get; set; }
		public IReplaceableView FilmTemplate { get; set; }

		public IReplaceableView GenericTemplate { get; set; }

		public ItemTemplateSelector()
		{
			PopTemplate = new DataTemplate(typeof(PopViewCell));
			GenericTemplate = new DataTemplate(typeof(GenericViewCell));
			FilmTemplate = new DataTemplate(typeof(FilmViewCell));
		}

		public override DataTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
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
