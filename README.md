# VirtualListView for Xamarin.Forms
This is an experiment in creating a ListView control for Xamarin Forms with virtualization as a first priority to support fast, template driven lists with speed as the priority.

## Sample

![iOS](Screenshots/XF.VirtualListView.iOS.gif) ![Android](Screenshots/XF.VirtualListView.Android.gif)

## Native controls
The implementation uses fast native controls in its renderers and optimizes for the native platform's recycling strategies.  Items are cached through the platform's recycling mechanisms so that they can be reused efficiently.  This also means the Forms representation of items are cached as well.  Each type of template (Item, Section Header, Section Footer) is cached individually so that they are reused efficiently.

Controls used on each platform:
  - iOS: UICollectionView
  - Android: RecyclerView
  - UWP: ListView (Virtualized)

## Adapter / Data Source

Instead of starting with a typical C# collection such as ObservableCollection, the VirtualListView takes the adapter approach that is common to iOS and Android has the concept of grouping built in (called Sections).

This pattern is optimal since it allows for easily creating adapters backed by direct access data stores such as databases.  Instead of trying to load data from the actual datastore, and trying to deal with cache invalidation for an in memory collection you can write your adapter directly against any type of storage.

To create an adapter for the VirtualListView, you need to implement the following interface:

```csharp
public interface IVirtualListViewAdapter
{
	int Sections { get; }

	object Section(int sectionIndex);

	int ItemsForSection(int sectionIndex);

	object Item(int sectionIndex, int itemIndex);
}
```

Here's a pseduo example of how to implement an adapter against a database with a bit of caching for optimal performance:

```csharp
public class DatabaseAdapter : IVirtualListViewAdapter
{
	public DatabaseAdapter()
	{
		Db = new Database(...);
	}

	public Database Db { get; }

	public void InvalidateCache()
	{
		cachedSectionSummaries.Clear();
	}

	Dictionary<int, GroupInfo> cachedSectionSummaries = new Dictionary<int, GroupInfo>();

	int? cachedNumberOfSections = null;

	public int Sections
		=> cachedNumberOfSections ??= Db.ExecuteScalar<int>("SELECT COUNT(GroupId) FROM Group");

	public object Item(int sectionIndex, int itemIndex)
	{
		var groupInfo = Section(sectionIndex);

		return Db.FindWithQuery<ItemInfo>("SELECT * FROM Item WHERE GroupId = ? LIMIT 1 OFFSET ?", groupInfo.Id, itemIndex);
	}

	public int ItemsForSection(int sectionIndex)
	{
		if (cachedSectionSummaries.ContainsKey(sectionIndex))
			return cachedSectionSummaries[sectionIndex].ItemCount;

		var groupInfo = Section(sectionIndex);

		if (groupInfo != null)
			cachedSectionSummaries.Add(sectionIndex, groupInfo);
		
		return groupInfo.ItemCount;
	}

	public object Section(int sectionIndex)
	{
		if (cachedSectionSummaries.ContainsKey(sectionIndex))
			return cachedSectionSummaries[sectionIndex];

		var sql = @"
				SELECT 
					g.GroupId,
					g.GroupName,
					Count(*) as ItemCount
				FROM Group g
					INNER JOIN Item i ON item.GroupId = g.GroupId
				GROUP BY g.GroupId
				ORDER BY g.GroupName
				LIMIT 1 OFFSET ?
		";

		var groupInfo = Db.FindWithQuery<GroupInfo>(sql, sectionIndex);

		if (groupInfo != null)
			cachedSectionSummaries.Add(sectionIndex, groupInfo);

		return groupInfo;
	}
}
```

Using this pattern as a base, it would be possible to use any collection as an adapter.  In the future it might be helpful to provide wrappers around this for ObservableCollection and other in memory collection types to make it easier to use for those cases.

## Templates

DataTemplates are available for Items along with an Item Template Selector (which is a custom class based on the adapter pattern to select templates).

Different templates can be specified for:
 - Global Header
 - Global Footer
 - Section Header
 - Section Footer
 - Item

In addition, it's possible to use Template Selectors which allow you to use different DataTemplates depending on the section / item index being displayed.

Template selectors are available for:
 - Section Headers and Footers
 - Items


For Item template selectors, sublcass the `AdapterItemDataTemplateSelector`:

```csharp
public class MyItemTemplateSelector
{
	PersonTemplate personTemplate = new PersonTemplate();
	GenericTemplate genericTemplate = new GenericTemplate();

	public override DataTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
	{
		var item = adapter.Item(sectionIndex, itemIndex);

		if (item is Person)
			return personTemplate;
		
		return genericTemplate;
	}
}
```

For section template selectors, subclass `AdapterSectionDataTemplateSelector`.

## ViewCells

All templates must contain a single `VirtualViewCell` child element.

The `VirtualViewCell` is a subclass of `ViewCell` but adds some additional bindable properties that are useful for adapting your views for things like separators and selection state:

  - int SectionIndex
  - int ItemIndex
  - bool IsGlobalHeader
  - bool IsGlobalFooter
  - bool IsSectionHeader
  - bool IsSectionFooter
  - bool IsItem
  - bool IsLastItemInSection
  - bool IsNotLastItemInSection
  - bool IsFirstItemInSection
  - bool IsNotFirstItemInSection
  - bool IsSelected

You can access these properties from your templates.  Here's an example of displaying an item separator using these properties, as well as changing the background color based on the selection state and a converter:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xct:VirtualViewCell
	xmlns:xct="clr-namespace:Xamarin.CommunityToolkit.UI.Views;assembly=VirtualListView"
	xmlns="http://xamarin.com/schemas/2014/forms" 
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	x:Class="VirtualListViewSample.GenericViewCell"
	x:Name="self">
  <xct:VirtualViewCell.View>
		<StackLayout
			Spacing="0"
			BackgroundColor="{Binding Source={x:Reference self}, Path=IsSelected, Converter={StaticResource selectedColorConverter}}">

			<BoxView
				HorizontalOptions="FillAndExpand"
				HeightRequest="1"
				BackgroundColor="#f8f8f8"
				IsVisible="{Binding Source={x:Reference self}, Path=IsNotFirstItemInSection}" />

			<Frame BackgroundColor="#f0f0f0" HasShadow="False" CornerRadius="14" Margin="10,5,10,5" Padding="10">
				<Label Text="{Binding TrackName}" FontSize="Subtitle" />
			</Frame>

		</StackLayout>
	</xct:VirtualViewCell.View>
</xct:VirtualViewCell>
```

Notice the `xct:VirtualViewCell` has a `x:Name="self"` name.  This allows you to reference the object and its bindable properties as the example shows inside the `BoxView`'s visibility: `IsVisible="{Binding Source={x:Reference self}, Path=IsNotFirstItemInSection}"`.

## Selection

There are 3 selection modes: None, Single, and Multiple.  Currently there is no bindable properties for selected items, but there is a `SelectedItemsChanged` event.

Only `Item` types are selectable.

In the future there will be bindable properties and maybe a way to cancel a selection event.


## Future

Looking ahead, there are a few goals:

1. UWP Support - this is currently a work in progress
2. Horizontal support - this should be relatively easy to implement
3. Bindable properties for item selection

Some current non-goals but considerations for even later:
- Grid / Column support
- Sticky section headers