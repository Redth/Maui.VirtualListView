using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace VirtualListViewSample
{
	public class ItemTemplateSelector : AdapterItemDataTemplateSelector
	{
		public DataTemplate PopTemplate { get; set; }
		//public DataTemplate HeavyTemplate { get; set; }
		public DataTemplate FilmTemplate { get; set; }

		public DataTemplate GenericTemplate { get; set; }

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
