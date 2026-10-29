using System.Runtime.CompilerServices;

namespace VirtualListView.Tests;

public class RetainedViewTests
{
	[Fact]
	public void PositionAndDataAreBoundBeforeNativeHandlerCreation()
	{
		var data = new object();
		var owner = new TestListView { ItemTemplate = new DataTemplate(() => new TrackedView()) };
		using var slot = new RetainedView(owner, view =>
		{
			Assert.Equal(3, ((IPositionInfo)view).ItemIndex);
			Assert.True(((IPositionInfo)view).IsSelected);
			Assert.Same(data, ((BindableObject)view).BindingContext);
			Assert.Same(owner, ((Element)view).Parent);
		}, () => { });
		slot.Bind(Item(3, true), data);
	}

	[Fact]
	public void NativeAttachFailureReleasesTheNewOwnedTreeAndPropagates()
	{
		var view = new TrackedView();
		var owner = new TestListView { ItemTemplate = new DataTemplate(() => view) };
		using var slot = new RetainedView(owner, _ => throw new ApplicationException("native attach"), () => { });
		Assert.Throws<ApplicationException>(() => slot.Bind(Item(0), new object()));
		Assert.Null(slot.View);
		Assert.Null(view.Parent);
		Assert.Equal(1, view.Disposals);
	}

	[Fact]
	public void CompatibleRebindKeepsTreeAndHandlersAndResetsPositionWithoutReuseCallback()
	{
		var selector = new TestSelector();
		var owner = new TestListView { Selector = selector };
		using var slot = CreateSlot(owner);
		slot.Bind(Item(0, true), new object());
		var view = Assert.IsType<TrackedView>(slot.View);
		var handler = new TrackingHandler(view);
		view.Handler = handler;
		Assert.Same(owner, view.Parent);
		slot.Bind(Item(1, false), new object());
		Assert.Same(view, slot.View);
		Assert.Same(handler, view.Handler);
		Assert.False(view.IsSelected);
		Assert.Equal(1, view.ItemIndex);
		Assert.Equal(1, selector.Created);
		Assert.Equal(0, handler.Disconnects);
		slot.Unbind();
		Assert.Same(owner, view.Parent);
		Assert.Same(handler, view.Handler);
		Assert.Equal(-1, view.ItemIndex);
		Assert.False(view.IsSelected);
	}

	[Fact]
	public void VisibilityDoesNotReleaseLogicalOwnershipBindingOrRequiredCallbacks()
	{
		var selector = new TestSelector();
		var context = new object();
		var owner = new TestListView { Selector = selector, BindingContext = context };
		using var slot = CreateSlot(owner);
		slot.Bind(Item(0), null);
		var view = Assert.IsType<TrackedView>(slot.View);
		Assert.Same(context, view.BindingContext);
		Assert.Equal(0, selector.Attached);
		slot.SetVisible(true);
		slot.SetVisible(true);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		slot.SetVisible(false);
		slot.SetVisible(false);
		Assert.Equal(1, selector.Attached);
		Assert.Equal(1, selector.Detached);
		Assert.Same(owner, view.Parent);
		Assert.Same(context, view.BindingContext);
		Assert.Equal(0, view.Disposals);
	}

	[Fact]
	public void IncompatibleTemplateReleasesOwnedTreeAndDescendantHandlersExactlyOnce()
	{
		var owner = new TestListView { ItemTemplate = new DataTemplate(() => new TrackedView()) };
		using var slot = CreateSlot(owner);
		slot.Bind(Item(0), new object());
		var old = Assert.IsType<TrackedView>(slot.View);
		var child = new ContentView();
		old.Content = child;
		var rootHandler = new TrackingHandler(old);
		var childHandler = new TrackingHandler(child);
		old.Handler = rootHandler;
		child.Handler = childHandler;
		owner.ItemTemplate = new DataTemplate(() => new TrackedView());
		slot.Bind(Item(1), new object());
		Assert.NotSame(old, slot.View);
		Assert.Null(old.Parent);
		Assert.Null(old.Handler);
		Assert.Null(child.Handler);
		Assert.Equal(1, rootHandler.Disconnects);
		Assert.Equal(1, rootHandler.Disposals);
		Assert.Equal(1, childHandler.Disconnects);
		Assert.Equal(1, old.Disposals);
		slot.Release();
		slot.Release();
		Assert.Equal(1, old.Disposals);
	}

	[Fact]
	public void SelectorReplacementDetachesThroughOriginalSelector()
	{
		var first = new TestSelector();
		var second = new TestSelector();
		var owner = new TestListView { Selector = first };
		using var slot = CreateSlot(owner);
		slot.Bind(Item(0), null);
		slot.SetVisible(true);
		owner.Selector = second;
		slot.Bind(Item(1), null);
		Assert.Equal(1, first.Detached);
		Assert.Equal(1, second.Attached);
		Assert.Equal(1, second.Created);
	}

	[Fact]
	public void ReplacingGlobalWithSameClrTypeUsesNewInstanceWithoutDisposingEither()
	{
		var first = new TrackedView { BindingContext = new object() };
		var second = new TrackedView { BindingContext = new object() };
		var firstHandler = new TrackingHandler(first);
		first.Handler = firstHandler;
		var owner = new TestListView { GlobalHeader = first };
		using var slot = CreateSlot(owner);
		slot.Bind(PositionInfo.ForHeader(0), null);
		var firstId = owner.GetReuseId(PositionInfo.ForHeader(0), null);
		var firstContext = first.BindingContext;
		Assert.Same(first, slot.View);
		owner.GlobalHeader = second;
		Assert.Null(slot.View);
		Assert.Null(first.Parent);
		slot.Bind(PositionInfo.ForHeader(0), null);
		Assert.Same(second, slot.View);
		Assert.NotEqual(firstId, owner.GetReuseId(PositionInfo.ForHeader(0), null));
		slot.Release();
		Assert.Same(firstContext, first.BindingContext);
		Assert.Same(firstHandler, first.Handler);
		Assert.Equal(0, firstHandler.Disconnects);
		Assert.Equal(0, first.Disposals);
		Assert.Equal(0, second.Disposals);
	}

	[Fact]
	public void SuppliedViewMovesBetweenSlotsWithoutTwoNativeOrLogicalOwners()
	{
		var supplied = new TrackedView();
		var owner = new TestListView { GlobalHeader = supplied, GlobalFooter = supplied };
		IView headerNative = null;
		IView footerNative = null;
		using var header = new RetainedView(owner, view => headerNative = view, () => headerNative = null);
		using var footer = new RetainedView(owner, view => footerNative = view, () => footerNative = null);
		header.Bind(PositionInfo.ForHeader(0), null);
		footer.Bind(PositionInfo.ForFooter(1), null);
		Assert.Null(header.View);
		Assert.Null(headerNative);
		Assert.Same(supplied, footerNative);
		Assert.Same(owner, supplied.Parent);
		Assert.Single(((IVisualTreeElement)owner).GetVisualChildren());
		owner.GlobalHeader = null;
		Assert.Same(supplied, footer.View);
		footer.Release();
		Assert.Null(supplied.Parent);
		Assert.Equal(0, supplied.Disposals);
	}

	[Fact]
	public void ClearingAnOldPropertyCannotReleaseAViewMovedToAnotherList()
	{
		var supplied = new TrackedView();
		var first = new TestListView { GlobalHeader = supplied };
		var second = new TestListView { GlobalHeader = supplied };
		using var oldSlot = CreateSlot(first);
		using var newSlot = CreateSlot(second);
		oldSlot.Bind(PositionInfo.ForHeader(0), null);
		newSlot.Bind(PositionInfo.ForHeader(0), null);
		first.GlobalHeader = null;
		Assert.Null(oldSlot.View);
		Assert.Same(supplied, newSlot.View);
		Assert.Same(second, supplied.Parent);
		Assert.Equal(0, supplied.Disposals);
	}

	[Fact]
	public void EmptyViewUsesPublicLogicalOwnershipAndIsNeverTemplateOwned()
	{
		var owner = new TestListView { BindingContext = new object() };
		var first = new TrackedView();
		var second = new TrackedView();
		owner.EmptyView = first;
		Assert.Same(owner, first.Parent);
		Assert.Same(owner.BindingContext, first.BindingContext);
		owner.EmptyView = second;
		Assert.Null(first.Parent);
		Assert.Same(owner, second.Parent);
		owner.EmptyView = null;
		Assert.Null(second.Parent);
		Assert.Equal(0, first.Disposals);
		Assert.Equal(0, second.Disposals);
	}

	[Fact]
	public void TemplateIdsAreInstanceBasedAndDoNotDependOnPrivateMembersOrHashes()
	{
		var first = new DataTemplate(() => new ContentView());
		var second = new DataTemplate(() => new ContentView());
		Assert.Equal(first.GetDataTemplateId(), first.GetDataTemplateId());
		Assert.NotEqual(first.GetDataTemplateId(), second.GetDataTemplateId());
		Assert.NotEqual(new SameHash().GetStableIdentity(), new SameHash().GetStableIdentity());
		var weak = CreateWeakTemplate();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		Assert.False(weak.TryGetTarget(out _));
	}

	[Fact]
	public void PoolRetentionIsBoundedAcrossRareTypesAndDrainsOwnedContent()
	{
		var released = 0;
		var pool = new ReusableViewPool<object>(_ => released++);
		for (var i = 0; i < 10_000; i++)
			pool.Return(i.ToString(), new object(), 0);
		Assert.Equal(9_996, released);
		pool.Dispose();
		pool.Dispose();
		Assert.Equal(10_000, released);
		Assert.Null(pool.Take("9999"));
	}

	[Fact]
	public void PoolKeepsTwoCompatibleEntriesAndReleasesExcess()
	{
		var released = new List<object>();
		using var pool = new ReusableViewPool<object>(released.Add);
		var first = new object();
		var second = new object();
		var excess = new object();
		pool.Return("template", first, 20);
		pool.Return("template", second, 20);
		pool.Return("template", excess, 20);
		Assert.Same(excess, Assert.Single(released));
		Assert.Same(first, pool.Take("template"));
		Assert.Same(second, pool.Take("template"));
		Assert.Null(pool.Take("template"));
	}

	[Fact]
	public void NativeRegistryReleasesDeadSlotsWithoutRetainingNativeContainers()
	{
		var owner = new TestListView { ItemTemplate = new DataTemplate(() => new TrackedView()) };
		var registry = new NativeViewRegistry<object>();
		var (native, old) = RegisterDeadNative(registry, owner);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		Assert.False(native.TryGetTarget(out _));
		registry.ForEach(_ => { });
		Assert.Equal(1, old.Disposals);
		Assert.Null(old.Parent);
		Assert.Empty(((IVisualTreeElement)owner).GetVisualChildren());
		registry.Drain(_ => { });
		Assert.Equal(1, old.Disposals);
	}

	[Fact]
	public void BuiltInBindingPreservesApplicationSuppliedContextAndBindsNullItemAsNull()
	{
		var data = new object();
		var header = new ContentView { BindingContext = data };
		var owner = new TestListView { BindingContext = new object(), GlobalHeader = header };
		owner.RecycleView(PositionInfo.ForHeader(0), null, header);
		Assert.Same(data, header.BindingContext);
		var item = new ContentView { BindingContext = data };
		owner.RecycleView(Item(0), null, item);
		Assert.Null(item.BindingContext);
	}

	static RetainedView CreateSlot(IVirtualListView owner) => new(owner, _ => { }, () => { });
	static PositionInfo Item(int index, bool selected = false) => PositionInfo.ForItem(index, 0, index, 10, 1, selected);

	[MethodImpl(MethodImplOptions.NoInlining)]
	static WeakReference<DataTemplate> CreateWeakTemplate()
	{
		var template = new DataTemplate(() => new ContentView());
		_ = template.GetDataTemplateId();
		return new WeakReference<DataTemplate>(template);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static (WeakReference<object> Native, TrackedView View) RegisterDeadNative(NativeViewRegistry<object> registry, TestListView owner)
	{
		var native = new object();
		var slot = CreateSlot(owner);
		slot.Bind(Item(0), null);
		registry.Add(native, slot);
		return (new WeakReference<object>(native), (TrackedView)slot.View);
	}

	sealed class SameHash { public override int GetHashCode() => 1; }

	sealed class TrackedView : VirtualViewCell, IDisposable
	{
		public int Disposals { get; private set; }
		public void Dispose() => Disposals++;
	}

	sealed class TestSelector : IVirtualListViewSelector
	{
		public int Created { get; private set; }
		public int Attached { get; private set; }
		public int Detached { get; private set; }
		public bool SectionHasHeader(int index) => false;
		public bool SectionHasFooter(int index) => false;
		public IView CreateView(PositionInfo position, object data) { Created++; return new TrackedView(); }
		public string GetReuseId(PositionInfo position, object data) => "one-template";
		public void RecycleView(PositionInfo position, object data, IView view) { }
		public void ViewAttached(PositionInfo position, IView view) => Attached++;
		public void ViewDetached(PositionInfo position, IView view) => Detached++;
	}

	sealed class TrackingHandler : IViewHandler, IDisposable
	{
		public TrackingHandler(IView view) => VirtualView = view;
		public int Disconnects { get; private set; }
		public int Disposals { get; private set; }
		public bool HasContainer { get; set; }
		public object ContainerView => null;
		public object PlatformView => null;
		public IView VirtualView { get; private set; }
		IElement IElementHandler.VirtualView => VirtualView;
		public IMauiContext MauiContext => null;
		public Size GetDesiredSize(double width, double height) => Size.Zero;
		public void PlatformArrange(Rect frame) { }
		public void SetMauiContext(IMauiContext context) { }
		public void SetVirtualView(IElement view) => VirtualView = (IView)view;
		public void UpdateValue(string property) { }
		public void Invoke(string command, object args) { }
		public void DisconnectHandler() => Disconnects++;
		public void Dispose() => Disposals++;
	}
}
