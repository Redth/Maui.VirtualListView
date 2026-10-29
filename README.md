# VirtualListView for .NET MAUI
This is an experiment in creating a virtualized ListView control for .NET MAUI to support simple, fast, multi-templated, uneven item sized lists by not adding too many bells and whistles and using an adapter pattern data source.

[![Nuget: Redth.Maui.VirtualListView](https://img.shields.io/nuget/vpre/Redth.Maui.VirtualListView?logo=nuget&label=Redth.Maui.VirtualListView&color=004880&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FRedth.Maui.VirtualListView%2F)](https://www.nuget.org/packages/Redth.Maui.VirtualListView)


## Vroooom!

![VirtualListView-Maui-MacCatalyst](https://user-images.githubusercontent.com/271950/129656785-ad302f84-4439-4f96-9405-29e62ed84861.gif)

In the sample, each item (and header/footer) is measured as it is recycled.  Performance is pretty great considering!  In the future there will be an option to tell the ListView if your template(s) are a consistent size so that the measure can be skipped for even better performance.

## Native controls
The implementation uses fast native controls in its renderers and optimizes for the native platform's recycling strategies.  Items are cached through the platform's recycling mechanisms so that they can be reused efficiently.  This also means the MAUI representation of items are cached as well.  Each type of template (Item, Section Header, Section Footer) is cached individually so that they are reused efficiently.

Controls used on each platform:
  - iOS: UICollectionView
  - Android: RecyclerView
  - WinAppSDK: ItemsRepeater + ScrollHost with IElementFactory

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

Adapters can present data prepared from any storage system, but their synchronous methods run in native count, bind, template-selection, and layout paths. Keep these methods cheap and indexed: no SQL/HTTP, blocking waits, full-source enumeration, or business ordering. UI virtualization recycles views; it does not make synchronous storage access nonblocking or bound the amount of data you retain.

To create an adapter for the VirtualListView, you need to implement the following interface:

```csharp
public interface IVirtualListViewAdapter
{
	int GetNumberOfSections();

	object GetSection(int sectionIndex);

	int GetNumberOfItemsInSection(int sectionIndex);

	object GetItem(int sectionIndex, int itemIndex);

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

For a database-backed flat list, asynchronously prepare a bounded window outside the adapter, then publish its resident array and invalidate on the UI thread. The example uses your application's `ItemInfo` model and repository; `ReadWindowAsync` is an application-defined asynchronous query with stable ordering and a maximum result size, not a VirtualListView API.

```csharp
public sealed class WindowAdapter : VirtualListViewAdapterBase<object, ItemInfo>
{
	ItemInfo[] items = Array.Empty<ItemInfo>();

	public override int GetNumberOfItemsInSection(int sectionIndex)
		=> items.Length;

	public override ItemInfo GetItem(int sectionIndex, int itemIndex)
		=> items[itemIndex];

	// Call on the UI thread; do not mutate a published snapshot.
	public void Publish(IEnumerable<ItemInfo> preparedItems)
	{
		items = preparedItems.ToArray();
		base.InvalidateData();
	}
}

// In your page/view model, outside all native callbacks:
var adapter = new WindowAdapter();
listView.Adapter = adapter;
var prepared = await repository.ReadWindowAsync(offset: 0, limit: 250, cancellationToken);
await MainThread.InvokeOnMainThreadAsync(() => adapter.Publish(prepared));
```

Counts and item indices describe only the published window, not a fictitious database-wide count whose records would need synchronous fetching. Paging, seek semantics, filtering, cancellation, stale-result rejection, errors, and selection/scroll restoration belong to the application. Publish a new window only if its query/source generation is still current. Do not start loads from `GetItem`, count getters, template selectors, or measure callbacks. Use a genuinely asynchronous storage API; if the storage library is synchronous, perform the bounded query off the UI thread before publication.

For grouped data, prepare bounded section summaries and item arrays together. The same snapshot must supply section counts, item counts, and indexed data:

```csharp
public sealed record PreparedSection(GroupInfo Group, ItemInfo[] Items);

public sealed class SectionWindowAdapter : VirtualListViewAdapterBase<GroupInfo, ItemInfo>
{
	PreparedSection[] sections = Array.Empty<PreparedSection>();

	public override int GetNumberOfSections() => sections.Length;

	public override int GetNumberOfItemsInSection(int sectionIndex)
		=> sections[sectionIndex].Items.Length;

	public override GroupInfo GetSection(int sectionIndex)
		=> sections[sectionIndex].Group;

	public override ItemInfo GetItem(int sectionIndex, int itemIndex)
		=> sections[sectionIndex].Items[itemIndex];

	public void Publish(IEnumerable<PreparedSection> preparedSections)
	{
		sections = preparedSections
			.Select(section => new PreparedSection(section.Group, section.Items.ToArray()))
			.ToArray();
		base.InvalidateData();
	}
}
```

The repository should enforce bounds on both sections and items before publication. Keep item/group model revisions consistent with the snapshot as well; do not concurrently mutate it during native reads. Publish mutable source changes on the control's owning UI dispatcher. `ObservableCollectionAdapter` forwards collection notifications on the caller's thread, so its mutations follow the same rule.

The handler serializes native invalidations on that owning dispatcher and drops queued notifications from an old adapter or disconnected handler. This is a native-view safety boundary, **not** synchronization for your source: dispatching an event does not make concurrent edits to a backing list safe. Use immutable prepared data or serialize publication and native reads on the same dispatcher.

`CachingVirtualListViewAdapter` caches section objects and counts, not items or database queries. It clears all metadata caches when it or its wrapped source invalidates, and forwards one notification without recursively invalidating the source. Dispose the wrapper when you are finished with it to detach its source subscription; disposal does **not** dispose the shared source adapter.

The control captures section counts/chrome once per accepted invalidation in O(sections) time and memory, without materializing items or views. Total count and item-to-position lookup are O(1); position-to-section lookup is O(log sections). After changing custom adapter data or custom selector chrome rules, invalidate the adapter. Replacing the adapter, templates, header/footer, or visibility flags refreshes the projection automatically. Changes to a header/footer's own visibility are observed when the view implements `INotifyPropertyChanged` (as MAUI controls do); custom non-notifying views must explicitly invalidate.

Adapter counts must be nonnegative and the projected total, including chrome, must fit in an `int`. Invalid counts or overflow throw before replacing the previous projection; they are not converted into valid-looking rows.

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

Return stable `DataTemplate` instances from selectors; do not create a new template for every item. Compatibility uses template instance identity without private MAUI reflection. Global headers and footers use the supplied view's identity, so replacing a header with another instance of the same type replaces the displayed content.

### Retained content and cleanup

A native slot keeps its compatible MAUI tree and child handlers through ordinary rebinds. Binding updates position and selection before creating native child handlers. Temporary disappearance only changes visibility: it does not remove logical ownership or clear the retained tree's binding context. `IVirtualListViewSelector.ViewAttached` and `ViewDetached` describe actual display, not template creation or final destruction.

True recycling clears item binding/position state while retaining compatible content. Replacing an incompatible template, evicting pooled content, or disconnecting the handler detaches logical/native ownership and disconnects the owned child tree. Android and Windows pools keep at most two spare trees per type, with a total spare budget based on the visible/active slots (minimum four). Native Apple slots remain owned by UIKit; retired slots are pruned and all tracked content is released at teardown.

The application still owns supplied `GlobalHeader`, `GlobalFooter`, `EmptyView`, and adapters. The handler removes them from its native hosts but does not dispose their handlers or services. Supplied views keep their explicit binding contexts; otherwise they inherit the list's context through public MAUI logical-child APIs.

Refresh completion is owned by the refresh that started it. It runs on the owning dispatcher, completes at most once, and cannot stop a later refresh after source replacement or reconnect. Disabling refresh or disconnecting ends the current indicator. Synchronous refresh failures end the indicator and propagate rather than being silently swallowed.


For Item template selectors, sublcass the `AdapterItemDataTemplateSelector`:

```csharp
public class MyItemTemplateSelector
{
	PersonTemplate personTemplate = new PersonTemplate();
	GenericTemplate genericTemplate = new GenericTemplate();

	public override DataTemplate SelectItemTemplate(IVirtualListViewAdapter adapter, int sectionIndex, int itemIndex)
	{
		var item = adapter.GetItem(sectionIndex, itemIndex);

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
		<VerticalStackLayout
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

		</VerticalStackLayout>
	</xct:VirtualViewCell>
</xct:VirtualViewCell>
```

Notice the `IsVisible="{DynamicResource IsNotFirstItemInSection}"` references a resource which has been automatically populated by the `VirtualViewCell`.

## Selection

There are 3 selection modes: `None`, `Single`, and `Multiple`.  Only `Item` types are selectable.

There are `SelectedItem` and `SelectedItems` bindable properties.
There's an `OnSelectedItemsChanged` event fired whenever these change.

## Refreshing

Pull to refresh is enabled for iOS/MacCatalyst and Android.  WindowsAppSDK does not have the equivalent feature so there is no support for it.
You can use the `RefreshCommand` or subscribe to the `OnRefresh` event to perform your logic while the refresh indicator displays.
You must set `IsRefreshEnabled` to true to enable the gesture.  
You can also set the `RefreshAccentColor` to change the color of the refresh indicator.

## Empty View

If no section contains data items, an empty view can be displayed automatically, even when there are multiple sections. An empty first section does not hide data in later sections.

Chrome follows the same projection on all platforms: no adapter or zero sections produces no rows; empty sections retain configured section headers/footers; the global header requires at least one data item; the global footer requires at least one projected section row (data or section chrome). Chrome alone does not suppress the empty view. Global chrome must also be present, enabled by `IsHeaderVisible`/`IsFooterVisible`, and have `Visibility.Visible`.

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
2. Supporting "size of content" constraints

Some current non-goals but considerations for even later:
- Grid / Column support
- Sticky section headers

## Portable regression tests

Run `dotnet test tests/VirtualListView.Tests/VirtualListView.Tests.csproj --configuration Release` with .NET 8. The project compiles the actual portable production sources against MAUI 8 packages; it does not require native MAUI workloads or replace the projection/cache logic with test implementations. CI runs this command separately from native builds. It covers projection boundaries, empty groups/chrome, invalidation, wrapped-source events, and large indexed lookups. Native handler compilation and device behavior still require their platform toolchains.
