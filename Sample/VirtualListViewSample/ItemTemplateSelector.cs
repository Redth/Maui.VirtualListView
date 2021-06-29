using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public class ItemTemplateSelector : VirtualListViewItemTemplateSelector
	{
        public ItemTemplateSelector() : base()
		{
            PopTemplate = new DataTemplate(typeof(PopViewCell));
            FilmTemplate = new DataTemplate(typeof(FilmViewCell));
            GenericTemplate = new DataTemplate(typeof(GenericViewCell));
        }

        readonly DataTemplate PopTemplate;
        readonly DataTemplate FilmTemplate;
        readonly DataTemplate GenericTemplate;

        public override DataTemplate SelectTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
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
