using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace VirtualListViewSample
{
	public class ItemTemplateSelector : AdapterItemDataTemplateSelector
	{
		public DataTemplate PersonTemplate { get; set; }
		public DataTemplate AnimalTemplate { get; set; }

		public ItemTemplateSelector()
		{
			PersonTemplate = new DataTemplate(typeof(PersonView));
			//AnimalTemplate = new DataTemplate(typeof(AnimalView));
		}

		public override DataTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
		{
			var item = adapter.Item(sectionIndex, itemIndex);

			return PersonTemplate;
		}
	}
}
