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

### Variable content sizing

The native collection owns scrolling and row placement. Retained MAUI content is
measured with a finite cross-axis constraint (width for vertical lists, height for
horizontal lists), then arranged at the size accepted by the native row. MAUI
counts the root `Margin` and nested padding; do not add those margins again in a
native row wrapper. Android converts logical units to pixels at that boundary.
Orientation changes retain RecyclerView's existing native layout parameters,
including its ViewHolder association, instead of replacing that native state.

Content `MeasureInvalidated` notifications are coalesced on the owning dispatcher.
Text, font, style, viewport and orientation changes can remeasure compatible
content without replacing its MAUI tree or resetting the data source. Each slot
keeps only its latest compatible measurement; global header/footer content has
its own measurement, not the first data row's size.

On iOS and Mac Catalyst, content-only updates use UIKit's preferred-size
invalidation context rather than a structural batch or a generic item
invalidation. This preserves each row's own size when another row grows or
shrinks. Unrealized rows are measured when UIKit displays them.

The sample's **Measurement Smoke** page contains short/long wrapping rows,
multiline global header/footer, root margins and nested padding. Toggle width,
orientation, text expansion, nested **Child visibility**, font size and 0/1/60
rows, then scroll forward/back and resize or rotate the window/device. In
vertical mode check every `END`
marker, both padding levels and the 12-by-6 root margins. In horizontal mode
check variable row widths, finite viewport height and retained orientation
changes; content taller than that viewport still needs an application-appropriate
height or template. Repeat on iOS, Mac Catalyst, Android at multiple densities,
and Windows. Portable sizing tests and native reference builds do **not** prove
native text rendering, lifecycle behavior or scrolling performance.

For child visibility, expand and collapse row 0 repeatedly without refreshing
the adapter: `END DETAILS` must appear only while expanded, row 0 must return
to its original height, and unchanged neighbors must retain their own heights.
The **Targeted Item Refresh** page's **First row** button selects and scrolls
to row 0 for repeated long/short text and template changes. Check selection
and unchanged neighbors after both growth and collapse. With no selection,
refresh the offscreen item, then use **Offscreen row** to display section 1 /
item 10 and check its updated text and geometry.

## Setup

The library and sample target .NET 10 on Android, iOS, Mac Catalyst, and Windows.
The portable library and regression tests also target `net10.0`.
`global.json` selects stable SDK **10.0.403** (with patch-only roll-forward and no
preview SDKs), and `Directory.Build.props` pins all MAUI packages to **10.0.120**.
The unused `Microsoft.Maui.Controls.Compatibility` dependency is no longer needed.

Install the SDK from [the .NET downloads](https://dotnet.microsoft.com/download/dotnet/10.0),
then install the matching stable workload set from the repository root:

```sh
# macOS: Android, iOS, and Mac Catalyst
dotnet workload install maui --version "$(dotnet --version)"
```

On Windows, use `dotnet workload install maui-windows --version 10.0.403` for
WinUI builds (or `maui` when also developing Android). This workload set uses
Android SDK 36.1 and JDK 21, and requires Xcode 27.1 for iOS. Use a Mac with the
matching Xcode for Apple sample builds, and Windows for the WinUI sample.
The sample keeps Android API 21 and Windows 10 version 1809 as its deployment
minima. The current .NET 10 Apple workload requires iOS 15.0 and Mac Catalyst
17.0, so those sample minima replace 14.2 and 14.0 respectively. Android target SDK
selection now follows the installed .NET 10 Android workload instead of an old
manifest override.
The iOS sample adopts the scene-based app lifecycle required by Xcode 27;
Mac Catalyst already uses its custom scene delegate.

Build one sample target at a time so MSBuild does not require the other hosts'
toolchains:

```sh
dotnet build Sample/VirtualListViewSample/VirtualListViewSample.csproj -c Release -p:TargetFrameworks=net10.0-android -p:AndroidPackageFormats=apk
dotnet build Sample/VirtualListViewSample/VirtualListViewSample.csproj -c Release -p:TargetFrameworks=net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:EnableCodeSigning=false
dotnet build Sample/VirtualListViewSample/VirtualListViewSample.csproj -c Release -p:TargetFrameworks=net10.0-maccatalyst -p:RuntimeIdentifier=maccatalyst-arm64 -p:EnableCodeSigning=false
```

On Windows:

```powershell
dotnet build Sample/VirtualListViewSample/VirtualListViewSample.csproj -c Release -p:TargetFrameworks=net10.0-windows10.0.19041 -p:RuntimeIdentifier=win-x64 -p:WindowsPackageType=None
```

CI runs the portable tests without native workloads, builds the libraries and
samples on their native hosts, and combines all five library targets and the
Windows resource index into one NuGet package. CI checks the package contents
and selected version, keeping branch builds separate from release versions.
The Apple jobs use the `xcode-27` hosted image with stable Xcode
27.1 selected explicitly; that runner image is currently in public preview.
All runs produce package artifacts without author signing or signing secrets.
Only published releases run the NuGet publish job.

### NuGet trusted publishing

The release-only publish job uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
instead of a stored API key. Only that job can request a GitHub OIDC token, which
`NuGet/login@v1` exchanges for a short-lived NuGet API key immediately before pushing
the package.

One-time setup:

1. In GitHub **Settings > Secrets and variables > Actions > Variables**, set the
   repository variable `NUGET_USER` to the nuget.org username used to publish
   (profile name, not email).
2. Sign in to that account on nuget.org and open **Trusted Publishing**. Add a
   GitHub policy with repository owner `Redth`, repository `Maui.VirtualListView`,
   and workflow file `main.yml` (filename only). Leave **Environment** empty;
   this workflow does not use a GitHub Actions environment.
3. Select the owner of `Redth.Maui.VirtualListView`, allow publishing new versions
   of existing packages, and scope the policy to `Redth.Maui.VirtualListView`.
4. Once the workflow change is merged, publish a GitHub release whose tag is the
   intended NuGet version, such as `1.2.3` or `1.2.3-preview.1` (no `v` prefix).
   The workflow builds, checks, and publishes that version.
5. After the first successful trusted publish, revoke the old API key on
   nuget.org and remove the unused GitHub secret `NUGET_ORG_API_KEY`.

The workflow no longer uses `CODESIGN_PFX`; the obsolete GitHub secret can be
removed after this change is merged. Before the first unsigned publish, check
the package's required signer under **Manage Packages** and the owners'
registered certificates on nuget.org. [Required author signing](https://learn.microsoft.com/en-us/nuget/nuget-org/package-signing-certificates)
can still block unsigned versions even when the push initially succeeds.
Removing an account's last registered certificate ends its required author
signing enrollment, but can affect other packages owned by that account.
Branch pushes and pull requests continue to produce unsigned artifacts without
requesting NuGet credentials or publishing packages.

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

### Targeted content refresh for custom adapters

Adapters derived from `VirtualListViewAdapterBase<TSection, TItem>` support
`InvalidateItems`. After changing resident data on the list's owning UI
dispatcher, pass **section/item coordinates**, not flat native row indices:

```csharp
var sections = new List<List<string>>
{
	new() { "Original", "Unchanged" },
	new() { "Also original" }
};
var adapter = new VirtualListViewSectionedAdapter<List<string>, string>(sections);
listView.Adapter = adapter;

sections[0][0] = "Updated content that may wrap onto more lines";
sections[1][0] = "Another updated item";
adapter.InvalidateItems(new ItemPosition(0, 0), new ItemPosition(1, 0));
```

Only the requested occurrences are rebound and have their templates reselected.
A compatible retained MAUI tree is reused; a different template replaces that
tree. Unrelated rows and section/global headers and footers are not rebound.
Offscreen items need no new view tree and read the updated data when realized.
Selection stays on the same positions, and the visible item and its relative
scroll offset are preserved unless a newer scroll action or reset supersedes
restoration. All platforms use the existing ordered update queue and native
content-update machinery; Windows sends per-row `Replace`, not a list reset.
There is no full-reset threshold for a content-only refresh.

This is **not a structural notification**: section/item counts, ordering,
section data, and chrome mapping must stay unchanged. Publish inserts, removals,
reorders, and section/chrome changes with `InvalidateData`, or use snapshots for
keyed structural edits. Detectable count/chrome mismatches throw rather than
silently resetting. Same-count reorders cannot be inferred from legacy adapters.
Null input, negative coordinates, and nonexistent items throw; an empty request
is a no-op, duplicate coordinates are refreshed once, and the input is copied.
No payload reads or notifications occur for a partially invalid batch.

The original `IVirtualListViewAdapter` interface is unchanged. The optional
`IItemRefreshVirtualListViewAdapter` exposes the convenience method for callers
holding an interface. Direct custom implementations can opt in by publishing
`ItemsInvalidatedEventArgs` through their existing `OnDataInvalidated` event.
They must validate coordinates before publishing, just as the base class does;
the handler also checks against its accepted projection before native updates.
Caching wrappers preserve targeted source events, forward explicit refreshes
when the source supports them, and never turn a refresh into `InvalidateData`.
Their count/section caches are cleared without owning or disposing the source.

Snapshot sources must instead submit a new snapshot with increased item content
revisions. Positional refresh is rejected for snapshot sources, including through
caching wrappers: it must not bypass the accepted immutable snapshot or its
revision ordering. Mutable legacy data is not captured by a notification:
keep its mutations and refresh calls on the owning UI dispatcher; use snapshots
for background publication. Normal bindings and automatic measurement
invalidation continue to work without calling this API. A refresh is for changed
adapter content or template choice, not a workaround for content resizing.

The sample's **Targeted Item Refresh** page updates selected rows' non-notifying
data, alternates short/long content, and toggles template choice. It includes
global/section chrome and a fixed offscreen fallback item. Check that selection,
unrelated rows, and the visible scroll anchor survive repeated updates, including
after scrolling away/back, on Android, iOS, Mac Catalyst, and Windows.

### `SnapshotAdapter`: optional keyed updates

Existing `IVirtualListViewAdapter` implementations still work unchanged. For
incremental updates, use `SnapshotAdapter` or implement the optional
`ISnapshotVirtualListViewAdapter`. Prepare resident data outside native callbacks:

```csharp
var adapter = new SnapshotAdapter(new VirtualListViewSnapshot(0, new[]
{
	new SnapshotSection("inbox", 0, "Inbox", new[]
	{
		new SnapshotItem("message-42", 0, new MessageRow("Original text"))
	})
}));
listView.Adapter = adapter;

// Keep the occurrence key; increase its content revision to rebind and resize.
adapter.Submit(new VirtualListViewSnapshot(1, new[]
{
	new SnapshotSection("inbox", 0, "Inbox", new[]
	{
		new SnapshotItem("message-41", 0, new MessageRow("Prepended text")),
		new SnapshotItem("message-42", 1, new MessageRow("Updated, longer text"))
	})
}));
// MessageRow is an application-defined immutable presentation model.
```

Snapshot constructors copy collections and order; they do not deep-clone payload
objects. Keep payloads immutable, or confine their `INotifyPropertyChanged`
mutations to the owning UI dispatcher. Increase an item's content revision when
its displayed content or size changes; increase a section's content revision when
its header/footer data changes. Snapshot revisions must strictly increase.
Negative/decreasing revisions, duplicate section/occurrence keys, invalid
notification indices/counts, and projected count overflow throw explicitly.

Identity is `(row kind, section occurrence key, item occurrence key)`, compared
by string value, never by row index or an unchecked native hash. A repeated item
within one section needs distinct occurrence keys. The same item key in another
section is a different occurrence, so a transfer between sections is a
remove/insert, not the same selected presentation. Chrome has its own row kinds
and cannot be selected. Source/connection generations separately reject late
callbacks and pending anchor restores.

Each event carries its captured snapshot in `SnapshotInvalidatedEventArgs`.
`SnapshotAdapter.Submit` can run on a worker; the handler accepts the exact
immutable input, projection, selection mapping, and native notifications in one
ordered UI transaction. It retains every intermediate edit, including reentrant
submissions. Native reads use the handler's accepted snapshot, not an adapter's
newer submitted state. Custom snapshot sources must serialize their event stream
and send the exact captured input in those event arguments. Plain
`OnDataInvalidated` remains an explicit reset fallback.

When resolving a selected `ItemPosition` in a callback, use the control's
`AcceptedSnapshot` and its section/item indices. It is the snapshot backing that
control, whereas `adapter.CaptureSnapshot()` may already contain a newer worker
submission. `AcceptedSnapshot` is null for legacy adapters or a disconnected
control.

Small edits use native insert/remove/move notifications on Android, collection
add/remove/move/replace notifications on Windows, and one unanimated UIKit batch.
Apple always uses **native section 0**, with item paths from the flat projection,
including global and section headers/footers. Same-key content revisions rebind
compatible retained templates and invalidate geometry, including for moved rows.
Unchanged bound content only updates its position/selection metadata when its
template stays compatible. No offscreen MAUI view tree is built to find identity
or anchors; Android stable native IDs remain disabled rather than hashing keys.

Resets are intentional for initial connection, legacy/custom adapters that do
not opt in, explicit `InvalidateData`, template/chrome mapping changes,
`Submit(snapshot, reset: true)` for a wholesale query replacement, and plans
requiring more than 128 sequential row edits. A reset on the same adapter still
maps surviving keys; a new adapter starts a new source generation and clears an
old keyed selection. A cache wrapper that only implements the legacy interface
also uses reset. Query/reset revisions still increase; an explicit query reset
may restart per-row content revisions.

Keyed selection is mapped back to the existing public `ItemPosition`,
`SelectedItem`, and `SelectedItems` API. Inserts and reorders keep the selected
occurrences; deleting a key removes its selection instead of selecting whatever
now occupies its old index. Legacy adapters keep their position-based behavior.
Membership and reconciliation use sets/dictionaries, not a scan of all selected
items for every visible row.

Before structural updates and supported content/viewport layout corrections, the
handler captures a visible item key and its offset relative to the viewport.
After native layout it restores that key; if deleted, it tries the next surviving
old item, then the previous one, at the same relative offset (clamped at scroll
limits). Rapid transactions share the pending keyed baseline rather than
mistaking a not-yet-laid-out native index for its new item. New touch/wheel/key or
programmatic scrolling supersedes pending restoration; source replacement and
disconnect invalidate it. UIKit/Windows offsets are logical units, Android
offsets are native pixels. This is not a saved absolute scroll offset. Native
self-sizing convergence and platform scroll/input behavior still need device
checks; these contracts are not performance certification.

Snapshots, keyed projection, and lookup dictionaries use O(N) identity/data
metadata. Diff preparation uses O(N log N + N * K) time, where K is at most 128
small edits before reset; intermediate Windows notification arrays have the same
bounded edit cost. This is a simple resident-snapshot path, not a million-row
database differ. Size metadata stays per retained native slot, not per source
item.
Every queued snapshot also retains its metadata until accepted; producers should
stay within their UI publication/memory budget rather than enqueue an unbounded
stream.

The sample's **Keyed Updates** page has deterministic prepend, insert, remove
visible row, reorder, same-key content expansion, section swap, three immediate
edits, scroll-to-middle, and resize controls. Select rows, scroll into a section,
record the top visible key/offset, then exercise each action. Check selection
labels and every section/global boundary. Delete the anchor and selected rows,
scroll again immediately after an edit, and repeat on all native targets.

An API 35 Android emulator smoke run exercised prepend, insert, reorder, section
swap, three immediate edits, same-key text expansion, deletion of the visible
anchor, resize, and deletion of a selected item. The tracked full row stayed at
the same native-pixel position (0 px delta) through the structural edits; after
anchor deletion the next surviving row took its place. Selection retained
`row-004` while the deleted selected key left it. This is a bounded functional
check, not a frame-time benchmark or iOS/Windows runtime certification. The
existing ObservableCollection demo also passed selected-row removal, append,
scroll-to-new-row, and new-row removal on that emulator.

### `ObservableCollectionAdapter`

Many developers are accustomed to using `ObservableCollection`.  While I recommend against using this if possible, there is a built in adapter that takes in an `ObservableCollection<TItem>` instance to help map it to the adapter pattern:

```csharp
var items = new ObservableCollection<string>();
items.Add("Item 1");
items.Add("Item 2");

var adapter = new ObservableCollectionAdapter<string>(items);

items.Add("Item 3");
```

Add/remove/replace/move notifications produce captured immutable snapshots and
use the incremental path. Each inserted occurrence gets its own generated key,
including repeated references to the same model; moving that occurrence retains
its key. Replacing/removing and reinserting it creates a new occurrence. No domain
ID is forced onto existing models. `Clear`/Reset and explicit `InvalidateData`
use reset and start new occurrence keys. Collection edits must be serialized;
mutating one `ObservableCollection` concurrently is not supported.

Item `PropertyChanged` is not a structural collection edit: normal bound MAUI
content handles it and retained layout reacts to measurement changes. The adapter
does not add per-item subscriptions or keep removed models alive. Dispose the
adapter to detach its collection subscription. Custom subclasses that change the
flat-list data shape must also override `CaptureSnapshot` consistently, or use a
legacy custom adapter for position-only/reset behavior.


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

For legacy adapters, the control captures section counts/chrome once per accepted invalidation in O(sections) time and memory, without materializing items or views. The optional snapshot path additionally retains O(N) identity metadata as described above. Total count and item-to-position lookup are O(1); position-to-section lookup is O(log sections). After changing custom adapter data or custom selector chrome rules, invalidate the adapter. Replacing the adapter, templates, header/footer, or visibility flags refreshes the projection automatically. Changes to a header/footer's own visibility are observed when the view implements `INotifyPropertyChanged` (as MAUI controls do); custom non-notifying views must explicitly invalidate.

Adapter counts must be nonnegative and the projected total, including chrome, must fit in an `int`. Invalid counts or overflow throw before replacing the previous projection; they are not converted into valid-looking rows.

## Templates

DataTemplates are available for Items along with an Item Template Selector (which is a custom class based on the adapter pattern to select templates).

When both `ItemTemplate` and `ItemTemplateSelector` are null, items use a retained
`Label` bound to the item itself (its text representation; null items show no
text). Setting or clearing either property refreshes the templates. This default
does not hide invalid custom templates or selectors: a configured selector must
return a valid view-producing template, or fall back to an explicit `ItemTemplate`.

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

On Windows, teardown clears the repeater source and replaces its factory with an
empty native template before disposing retained content; WinUI does not accept a
null native `ItemTemplate`. For native lifecycle regression checks, push and pop a
page containing a populated list repeatedly, both with a template and with neither
template property set. Also detach/reconnect the list's handler and reopen the
page, checking that rows render again. In applications using CommunityToolkit.Maui,
repeat popup open/close with the same configurations and normal automatic handler
disconnection. Portable tests do not exercise WinUI navigation or popup teardown.

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

`SelectedItems` remains an ordered `IList<ItemPosition>`: arrays and lists can
still be assigned or bound. To select a batch, set `SelectionMode` to `Multiple`
and call `SelectMany` on the control or its `IVirtualListView` interface:

```csharp
listView.SelectionMode = SelectionMode.Multiple;
listView.SelectMany(Enumerable.Range(0, 5000).Select(index => new ItemPosition(0, index)));
```

Existing selections keep their order; new unique positions are appended in input
order. The input is enumerated once and the resulting selection is published
once, with one `SelectedItems` property change and one `OnSelectedItemsChanged`
event. Empty or already-selected batches do not replace the collection or raise
an event. `SelectMany` does nothing in `None` or `Single` mode and rejects null
input. Call it on the owning UI dispatcher, just like the other selection APIs.
Membership and native selection differences use internal hash sets, not repeated
linear searches of the public list. Only changed, valid item positions update
native selection; offscreen items read the accepted state when realized.

## Refreshing

Set `IsRefreshEnabled = true` to enable native refresh on Android, iOS,
Mac Catalyst, and Windows. `RequestRefresh()` starts the native indicator and
invokes the same `RefreshCommand` / `OnRefresh` path as a pull gesture:

```csharp
listView.IsRefreshEnabled = true;
listView.OnRefresh += async (_, args) =>
{
    try
    {
        await ReloadDataAsync();
        adapter.InvalidateData();
    }
    finally
    {
        args.Complete();
    }
};

// For example, from a refresh button after the view is connected.
listView.RequestRefresh();
```

An eligible request is handled on the list's owning dispatcher. Disabled,
disconnected, or already-refreshing controls ignore programmatic requests; they
are not replayed after enabling, connecting, or completing the active refresh.
This includes requests while a gesture-started refresh is active. The method
does not enable refresh implicitly or cancel existing application work.

The command receives an `Action` completion callback, and the event receives
`RefreshEventArgs.Complete`. Complete in `finally` and use the application's
normal asynchronous error handling. Completion is marshalled to the owning
dispatcher and runs at most once for that refresh. Disabling refresh, changing
the adapter, or disconnecting ends its indicator; late or duplicate completion
cannot stop a newer refresh. Synchronous callback failures end the indicator and
propagate. A request without an executable command or event subscriber completes
immediately, so it need not display a visible animation.

Usually configure either the command or the event. When both are configured,
the existing contract is unchanged: the command runs if `CanExecute(null)` is
true, then the event runs, and the first valid completion ends the indicator.
There is no wait-for-all-consumers behavior.

Refresh requests do **not** invalidate adapter data automatically. Publish data
changes through the adapter's normal API (`InvalidateData`, `InvalidateItems`,
or a new snapshot). Conversely, those data notifications do not start a refresh
indicator or invoke refresh callbacks.

On iOS/Mac Catalyst, a programmatic request reveals `UIRefreshControl` at the
top, even if the list was scrolled. It retains the horizontal offset, respects
native top insets, and does not jump back to the previous position on completion.
Horizontal Apple lists use a separate vertical native refresh host so feedback
does not change the collection's height constraint or discard measured column
widths. Application/safe-area insets and real viewport changes still constrain
the collection normally. Changing Apple orientation ends active feedback before
replacing its native refresh host; it does not cancel application work. Each
native host gets its own refresh control so pull gestures continue to work after
orientation changes. On a real viewport resize, native size estimates are updated
against the incoming bounds before UIKit validates the layout.
Android uses `SwipeRefreshLayout`. Windows uses WinUI `RefreshContainer` and
supports touch pull-to-refresh; provide a refresh button for mouse/keyboard
users. Disabling Windows refresh leaves list scrolling and child controls
enabled. `RefreshAccentColor` changes the native indicator color.
Android and Windows keep the empty view inside the refresh host so it cannot
cover the native indicator.

The **SectionedAdapterPage** sample has a Refresh button, an enable switch, a
request counter, Full/Short/Empty content buttons, and a horizontal switch.
The End button scrolls to the last item for reveal checks; its opaque empty view
also exercises indicator layering. During native smoke checks, press Refresh
repeatedly, toggle refresh off during a request, pull to refresh, and leave/reopen the page.
Check that feedback stops and callback counts do not overlap. On Apple, also
request from mid-list and check for visible feedback without a completion-time
return jump. Repeat with empty/short content and horizontal orientation; horizontal
column widths and the user's horizontal scroll position must remain stable as
feedback starts and stops. Also scroll horizontally while feedback is active:
completion must retain that new position, not restore the request-time position.
Rotate the device in both list orientations and repeat the end-to-refresh check;
the resized cells must fit the viewport without native invalid-size diagnostics.
On Windows verify touch refresh, empty-view layering, and scrolling after toggling
refresh. Native tests must verify visible feedback, not only an internal flag.

## Empty View

If no section contains data items, an empty view can be displayed automatically, even when there are multiple sections. An empty first section does not hide data in later sections.

Empty means zero **data items across all sections**, regardless of section
headers or footers. Empty sections retain configured section chrome, but global
header and footer are hidden when empty by default. This includes the case where
section chrome previously kept a global footer visible.

Set `ShowHeaderWhenEmpty` and `ShowFooterWhenEmpty` independently to keep supplied
global content visible without data. Both default to `false` and also apply
before an adapter is assigned or when it reports zero sections.
`IsHeaderVisible=false` / `IsFooterVisible=false` always wins, as does hiding the
supplied view itself. The new settings do not affect nonempty lists.

When global chrome is shown while empty, `EmptyView` becomes a nonselectable
scroll-content row after any section chrome and before the global footer, rather
than an overlay. This keeps search/filter controls and footer content uncovered
on Android, iOS, Mac Catalyst, and Windows. It is measured like other content
(including margins and explicit size requests), and tall content can scroll.
When no global chrome is shown, the existing viewport empty view is retained.
Chrome alone never counts as data or suppresses the empty message.

```xaml
<vlv:VirtualListView ShowHeaderWhenEmpty="True" ShowFooterWhenEmpty="False">
  <vlv:VirtualListView.GlobalHeader>
    <SearchBar Placeholder="Filter results" />
  </vlv:VirtualListView.GlobalHeader>
  <vlv:VirtualListView.EmptyView>
    <Label Text="No matching results" Margin="16" />
  </vlv:VirtualListView.EmptyView>
</vlv:VirtualListView>
```

Custom view selectors that opt in to empty global chrome should handle
`PositionKind.EmptyView` by returning the supplied empty view, preserving its
binding context and using a distinct reuse ID. The built-in selector does this
automatically.

```xaml
<vlv:VirtualListView.EmptyView>
  <Grid>
    <Label HorizontalOptions="Center" VerticalOptions="Center" Text="EMPTY" />
  </Grid>
</vlv:VirtualListView.EmptyView>
```


## Scrolled

Scrolled notifications can be observed with `ScrolledCommand` which will pass a `ScrolledEventArgs` parameter, or the `OnScrolled` event with a parameter of the same type.  The event args contain the X/Y position scrolled.

## Near-end notifications and paging

Set `RemainingItemsThreshold` to receive `RemainingItemsThresholdReached`, or
bind `RemainingItemsThresholdReachedCommand` (no command parameter). The default
`-1` disables notifications; `0` means the last loaded data item is visible;
positive values notify when that many or fewer loaded data items remain after
the furthest visible item. Values below `-1` are rejected.

```xml
<vlv:VirtualListView
    Adapter="{Binding Adapter}"
    RemainingItemsThreshold="3"
    RemainingItemsThresholdReachedCommand="{Binding LoadNextPageCommand}" />
```

Only **data items across all sections** count. Global and section headers/footers,
empty sections, native flat row numbers and scroll pixels do not count. The check
uses the control's accepted projection and visible item coordinates, in either
orientation. `FindVisiblePositions()` exposes those coordinates for custom
application behavior; filter `Kind == PositionKind.Item` and do not assume the
returned positions are sorted.

Checks run after native layout/scroll callbacks on the owning UI dispatcher.
A nonempty initial list can notify after layout without any scroll gesture,
including when it fits inside the viewport. An empty list, or a viewport with
only headers/footers, has no visible data item and does not request a first page:
the application must load that explicitly.

Notifications are latched, not sent for every scroll callback. They re-arm when
the viewport leaves the threshold and returns, when accepted data-item counts
grow (so a short appended page can trigger again without scrolling), when the
threshold changes, or when the adapter/connection is replaced. Content-only
updates, same-count resets and removals do not by themselves re-arm. Seeing only
chrome does not re-arm. After failure or a page that adds no data, staying near
the same end does not automatically retry; provide an explicit retry action.
Changing `-1` back to an enabled threshold explicitly re-arms, even if the data
has not changed.

This is a **notification**, not a paging engine. There is no `CanLoadMore` or
control-owned in-flight state. The command's `CanExecute(null)` is honored, but a
rejected command is not automatically retried when it later becomes executable.
The command runs before the event; use one surface for loading, not both.
Neither awaits asynchronous work. Applications must guard concurrent requests,
handle cancellation/stale query results, display errors, and publish resident
data safely. Disable the threshold (`-1`) while loading or paused and at
end-of-data. Only re-enable after releasing the in-flight guard and publishing a
successful page; an empty page that still claims more results should pause for
explicit retry, not toggle the threshold in an automatic loop.

The sample's **Paging** page (`Sample/VirtualListViewSample/PagingPage.cs`) uses
delayed synthetic pages and a `SnapshotAdapter`, with UI-thread publication,
one in-flight request, generation/cancellation guards, loading/error/end states,
Restart and explicit Retry. Its first page has three rows; the next request fails
once, and the request after thirteen loaded rows returns one empty nonterminal
page. Both pause until Retry. Subsequent pages reach 43 rows and disable the
threshold. Try resizing, navigating away during a request, restarting while
loading, and scrolling through global/section chrome on each native target.


## Future

Looking ahead, there are a few goals:

1. Even Rows - by default every cell is assumed uneven and measured every time the context changes or the cell is recycled.  Adding an option to assume each template type is the same size will make performance even better, but will be an explicit opt-in
2. Supporting "size of content" constraints

Some current non-goals but considerations for even later:
- Grid / Column support
- Sticky section headers

## Portable regression tests

Run `dotnet test tests/VirtualListView.Tests/VirtualListView.Tests.csproj --configuration Release` with the .NET 10 SDK selected by `global.json`. The project compiles the actual portable production sources against the same MAUI 10 packages as the library and sample; it does not require native MAUI workloads or replace the projection/cache logic with test implementations. CI runs this command separately from native builds. It covers projection boundaries, empty groups/chrome, invalidation, wrapped-source events, and large indexed lookups. Native boundary doubles in this suite are not device/runtime proof; native handler compilation and device behavior still require their platform toolchains.
