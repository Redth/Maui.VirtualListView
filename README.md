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
  - WinAppSDK: ItemsRepeaterScrollHost with IElementFactory

## Setup

To add the Virtual List View control to your project, you need to add `.UseVirtualListView()` to your app builder:

```csharp
		public static MauiApp Create()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.UseVirtualListView(); // <--- THIS
			return builder.Build();
		}
```

## Adapter / Data Source

Instead of starting with a typical C# collection such as `ObservableCollection`, the VirtualListView takes the adapter approach that is common to iOS and Android has the concept of grouping built in (called Sections).

This pattern is optimal since it allows for easily creating adapters backed by direct access data stores such as databases.  Instead of trying to load data from the actual datastore, and trying to deal with cache invalidation for an in memory collection you can write your adapter directly against any type of storage.

To create an adapter for the VirtualListView, you need to implement the following interface:

```csharp
public interface IVirtualListViewAdapter
{
	int Sections { get; }

	object Section(int sectionIndex);

	int ItemsForSection(int sectionIndex);

	object Item(int sectionIndex, int itemIndex);

	event EventHandler OnDataInvalidated;

	void InvalidateData();
}
```

There are a few implementations included in the box to use:

### `VirtualListViewAdapter`

This is a basic adapter backed by an `IList<TItem>`:

```csharp
var adapter = new VirtualListViewAdapter<string>(
	new [] {
		"Item 1",
		"Item 2",
		"Item 3",
		//...
	});
```

### `ObservableCollectionAdapter`

Many developers are accustomed to using `ObservableCollection`.  While I recommend against using this if possible, there is a built in adapter that takes in an `ObservableCollection<TItem>` instance to help map it to the adapter pattern:

```csharp
var items = new ObservableCollection<string>();
items.Add("Item 1");
items.Add("Item 2");

var adapter = new ObservableCollectionAdapter<string>(items);

items.Add("Item 3");
```

When using this adapter, the adapter will automatically invalidate itself (by calling `this.InvalidateData()` when the collection changes via the `CollectionView.CollectionChanged` event).


### Custom Adapter

For many scenarios, it is ideal to create your own adapter implementation.  You can implement the `IVirtualListViewAdapter` directly, or subclass `VirtualListViewAdapterBase<TSection, TItem>`.

Here's an example of a custom adapter for a flat list (no sections/grouping).  Notice that we cache commonly used data such as `ItemsForSection` and we will reset the cache anytime the data is invalidated:

```csharp
public class SQLiteAdapter : VirtualListViewAdapterBase<object, ItemInfo>
{
	public SQLiteAdapter() : base()
	{
		Db = new Database(...);
	}

	public Database Db { get; }

	int? cachedItemCount = null;

	// No sections/grouping, so disregard the sectionIndex
	public override int ItemsForSection(int sectionIndex)
		=> cachedItemCount ??= Db.ExecuteScalar<int>("SELECT COUNT(Id) FROM Items");

	public override string Item(int sectionIndex, int itemIndex)
		=> Db.FindWithQuery<ItemInfo>("SELECT * FROM Items ORDER BY Id LIMIT 1 OFFSET ?", itemIndex);

	public override void InvalidateData()
	{
		// Clear our item count cache
		// Also do this any time we may insert or delete data 
		cachedItemCount = null;
		base.InvalidateData();
	}
}
```

Here's an example of a more sophisticated adapter with grouping/sections. Again, notice we cache Section count and Item count per section:

```csharp
public class SQLiteSectionedAdapter : VirtualListViewAdapterBase<GroupInfo, ItemInfo>
{
	public SQLiteSectionedAdapter() : base()
	{
		Db = new Database(...);
	}

	public Database Db { get; }

	Dictionary<int, GroupInfo> cachedSectionSummaries = new ();

	int? cachedNumberOfSections = null;

	public int Sections
		=> cachedNumberOfSections ??= Db.ExecuteScalar<int>("SELECT DISTINCT COUNT(GroupId) FROM Items");

	// No sections/grouping, so disregard the sectionIndex
	public override int ItemsForSection(int sectionIndex)
		=> cachedItemCount ??= Db.ExecuteScalar<int>("SELECT COUNT(Id) FROM Items");

	public GroupInfo Section(int sectionIndex)
	{
		if (cachedSectionSummaries.ContainsKey(sectionIndex))
			return cachedSectionSummaries[sectionIndex];

		var sql = @"
				SELECT DISTINCT g.GroupId, g.GroupName, Count(i.Id) as ItemCount
				FROM Items g
					INNER JOIN Items i ON i.GroupId = g.GroupId
				GROUP BY g.GroupId
				ORDER BY g.GroupName
				LIMIT 1 OFFSET ?
		";

		var groupInfo = Db.FindWithQuery<GroupInfo>(sql, sectionIndex);

		if (groupInfo != null)
			cachedSectionSummaries.Add(sectionIndex, groupInfo);

		return groupInfo;
	}

	public override string Item(int sectionIndex, int itemIndex)
		=> Db.FindWithQuery<ItemInfo>("SELECT * FROM Items WHERE GroupId=? ORDER BY Id LIMIT 1 OFFSET ?", sectionIndex, itemIndex);

	public override void InvalidateData()
	{
		// Clear our caches
		// Also do this any time we may insert or delete data 
		cachedItemCount = null;
		cachedNumberOfSections.Clear();

		base.InvalidateData();
	}
}
```



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

## Virtual ViewCells

All templates can contain a single `IView`, or alternatively you can use `VirtualViewCell` to wrap your view.

The `VirtualViewCell`'s `ResourceDictionary` will contain a set of values which are are useful for adapting your views for things like separators and selection state:

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

> NOTE: These are also available as properties on `VirtualViewCell` itself, since it implements `IPositionInfo`

You can access these properties from your templates.  Here's an example of displaying an item separator using these properties, as well as changing the background color based on the selection state and a converter:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xct:VirtualViewCell
	xmlns:xct="clr-namespace:Microsoft.Maui.Controls;assembly=VirtualListView"
	xmlns="http://xamarin.com/schemas/2014/forms" 
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	x:Class="VirtualListViewSample.GenericViewCell">
  <xct:VirtualViewCell>
		<StackLayout
			Spacing="0"
			BackgroundColor="{Binding Source={x:Reference self}, Path=IsSelected, Converter={StaticResource selectedColorConverter}}">

			<BoxView
				HorizontalOptions="FillAndExpand"
				HeightRequest="1"
				BackgroundColor="#f8f8f8"
				IsVisible="{DynamicResource IsNotFirstItemInSection}" <!-- Use the automatic property -->
 				/>

			<Border Background="#f0f0f0" StrokeShape="{RoundedRectangle CornerRadius=14}" Margin="10,5,10,5" Padding="10">
				<Label Text="{Binding TrackName}" />
			</Border>

		</StackLayout>
	</xct:VirtualViewCell>
</xct:VirtualViewCell>
```

Notice the `IsVisible="{DynamicResource IsNotFirstItemInSection}"` references a resource which has been automatically populated by the `VirtualViewCell`.

## Selection

There are 3 selection modes: None, Single, and Multiple.  Currently there is no bindable properties for selected items, but there is a `SelectedItemsChanged` event.

Only `Item` types are selectable.

In the future there will be bindable properties and maybe a way to cancel a selection event.


## Refreshing

Pull to refresh is enabled for iOS/MacCatalyst and Android.  WindowsAppSDK does not have the equivalent feature so there is no support for it.
You can use the `RefreshCommand` or subscribe to the `OnRefresh` event to perform your logic while the refresh indicator displays.
You must set `IsRefreshEnabled` to true to enable the gesture.  
You can also set the `RefreshAccentColor` to change the color of the refresh indicator.

## Empty View

If your adapter has <= 1 section and no items, an empty view can be displayed automatically:

```xaml
<vlv:VirtualListView.EmptyView>
  <Grid>
    <Label HorizontalOptions="Center" VerticalOptions="Center" Text="EMPTY" />
  </Grid>
</vlv:VirtualListView.EmptyView>
```


## Scrolled

Scrolled notifications can be observed with `ScrolledCommand` which will pass a `ScrolledEventArgs` parameter, or the `OnScrolled` event with a parameter of the same type.  The event args contain the X/Y position scrolled.


## Future

Looking ahead, there are a few goals:

1. Even Rows - by default every cell is assumed uneven and measured every time the context changes or the cell is recycled.  Adding an option to assume each template type is the same size will make performance even better, but will be an explicit opt-in
2. Bindable properties for item selection

Some current non-goals but considerations for even later:
- Grid / Column support
- Sticky section headers
