# VirtualListView for .NET MAUI
This is an experiment in creating a virtualized ListView control for .NET MAUI to support simple, fast, multi-templated, uneven item sized lists by not adding too many bells and whistles and using an adapter pattern data source.

## Vroooom!

![VirtualListView-Maui-MacCatalyst](https://user-images.githubusercontent.com/271950/129656785-ad302f84-4439-4f96-9405-29e62ed84861.gif)

In the sample, each item (and header/footer) is measured as it is recycled.  Performance is pretty great considering!  In the future there will be an option to tell the ListView if your template(s) are a consistent size so that the measure can be skipped for even better performance.

## Native controls
The implementation uses fast native controls in its renderers and optimizes for the native platform's recycling strategies.  Items are cached through the platform's recycling mechanisms so that they can be reused efficiently.  This also means the MAUI representation of items are cached as well.  Each type of template (Item, Section Header, Section Footer) is cached individually so that they are reused efficiently.

Controls used on each platform:
  - iOS: UICollectionView
  - Android: RecyclerView
  - UWP: ListView (Virtualized)

## Adapter / Data Source

Instead of starting with a typical C# collection such as ObservableCollection, the VirtualListView takes the adapter approach that is common to iOS and Android has the concept of grouping built in (called Sections).

This pattern is optimal since it allows for easily creating adapters backed by direct access data stores such as databases.  Instead of trying to load data from the actual datastore, and trying to deal with cache invalidation for an in memory collection you can write your adapter directly against any type of storage.

To add the Virtual List View control to your project, you need to add `.UseVirtualListView()` to your app builder:

```csharp
		public static MauiApp Create()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseVirtualListView(); // <---
			return builder.Build();
		}
```


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
		=> (Section(sectionIndex) as GroupInfo)?.ItemCount ?? 0;

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

The `VirtualViewCell` adds some additional bindable properties that are useful for adapting your views for things like separators and selection state:

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
	xmlns:xct="clr-namespace:Microsoft.Maui.Controls;assembly=VirtualListView"
	xmlns="http://xamarin.com/schemas/2014/forms" 
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	x:Class="VirtualListViewSample.GenericViewCell"
	x:Name="self">
  <xct:VirtualViewCell>
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
	</xct:VirtualViewCell>
</xct:VirtualViewCell>
```

Notice the `xct:VirtualViewCell` has a `x:Name="self"` name.  This allows you to reference the object and its bindable properties as the example shows inside the `BoxView`'s visibility: `IsVisible="{Binding Source={x:Reference self}, Path=IsNotFirstItemInSection}"`.

## Selection

There are 3 selection modes: None, Single, and Multiple.  Currently there is no bindable properties for selected items, but there is a `SelectedItemsChanged` event.

Only `Item` types are selectable.

In the future there will be bindable properties and maybe a way to cancel a selection event.


## Future

Looking ahead, there are a few goals:

1. WinAppSdk Support - this is currently a work in progress
2. Even Rows - by default every cell is assumed uneven and measured every time the context changes or the cell is recycled.  Adding an option to assume each template type is the same size will make performance even better, but will be an explicit opt-in
3. Horizontal support - this should be relatively easy to implement
4. Bindable properties for item selection

Some current non-goals but considerations for even later:
- Grid / Column support
- Sticky section headers
