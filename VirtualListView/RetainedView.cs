#nullable enable
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace Microsoft.Maui;

// A native slot owns a compatible tree across binds; visibility does not own it.
internal sealed class RetainedView : IDisposable
{
	static readonly ConditionalWeakTable<IView, RetainedView> suppliedOwners = new();
	readonly IVirtualListView owner;
	readonly Action<IView> attachNative;
	readonly Action detachNative;
	IVirtualListViewSelector? selector;
	string? reuseId;
	PositionKind? kind;
	bool ownsView;
	bool visible;

	public RetainedView(IVirtualListView owner, Action<IView> attachNative, Action detachNative)
	{
		this.owner = owner;
		this.attachNative = attachNative;
		this.detachNative = detachNative;
	}

	public IView? View { get; private set; }
	public PositionInfo? Position { get; private set; }

	internal static void ReleaseSupplied(IVirtualListView owner, IView view, PositionKind kind)
	{
		if (suppliedOwners.TryGetValue(view, out var slot) && ReferenceEquals(slot.owner, owner) && slot.kind == kind)
			slot.Release();
	}

	public void Bind(PositionInfo position, object? data)
	{
		var nextSelector = owner.ViewSelector;
		var nextId = nextSelector.GetReuseId(position, data);
		var supplied = position.Kind is PositionKind.Header or PositionKind.Footer;
		var suppliedView = position.Kind == PositionKind.Header ? owner.Header : owner.Footer;
		var wasVisible = visible;
		var created = false;
		SetVisible(false);
		if (View is not null && (!ReferenceEquals(selector, nextSelector) || reuseId != nextId || kind != position.Kind
			|| (supplied && !ReferenceEquals(View, suppliedView))))
			Release();

		if (View is null)
		{
			var view = nextSelector.CreateView(position, data);
			if (view is null)
				throw new InvalidOperationException($"The view selector returned no view for {position.Kind}.");
			if (supplied)
			{
				if (suppliedOwners.TryGetValue(view, out var previous) && !ReferenceEquals(previous, this))
					previous.Release();
				suppliedOwners.Remove(view);
				suppliedOwners.Add(view, this);
			}
			View = view;
			ownsView = !supplied;
			selector = nextSelector;
			reuseId = nextId;
			kind = position.Kind;
			created = true;
		}

		try
		{
			if (created && owner is Element parent)
				parent.AddLogicalView(View);
			Position = position;
			if (View is IPositionInfo viewPosition)
				viewPosition.Update(position);
			nextSelector.RecycleView(position, data, View);
			if (created)
				attachNative(View);
			if (wasVisible)
				SetVisible(true);
		}
		catch
		{
			Release();
			throw;
		}
	}

	public void SetVisible(bool value)
	{
		if (visible == value || View is null || Position is null)
			return;
		visible = value;
		if (value)
			selector?.ViewAttached(Position, View);
		else
			selector?.ViewDetached(Position, View);
	}

	public void Unbind()
	{
		SetVisible(false);
		Position = null;
		if (ownsView && View is BindableObject bindable)
			bindable.BindingContext = null;
		if (View is IPositionInfo position)
			position.Update(new PositionInfo { SectionIndex = -1, ItemIndex = -1 });
	}

	public void Release()
	{
		SetVisible(false);
		var view = View;
		View = null;
		Position = null;
		selector = null;
		reuseId = null;
		kind = null;
		if (view is null)
			return;

		detachNative();
		if (!ownsView)
			suppliedOwners.Remove(view);
		if (owner is Element parent)
			parent.RemoveLogicalView(view);
		if (ownsView)
		{
			if (view is BindableObject bindable)
				bindable.BindingContext = null;
			DisconnectTree(view);
		}
	}

	internal static void DisconnectTree(IView view)
	{
		var elements = new[] { (IElement)view }.Concat(view is IVisualTreeElement tree
			? tree.GetVisualTreeDescendants().OfType<IElement>() : Enumerable.Empty<IElement>())
			.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
		foreach (var element in elements.Reverse())
		{
			var handler = element.Handler;
			// Controls clears and disconnects its previous handler in this setter.
			if (element is Element)
				element.Handler = null;
			else
			{
				handler?.DisconnectHandler();
				element.Handler = null;
			}
			if (handler is IDisposable disposable)
				disposable.Dispose();
			if (element is IDisposable ownedElement)
				ownedElement.Dispose();
		}
	}

	public void Dispose() => Release();
}

// Keep cleanup reachable without keeping a retired native slot alive.
internal sealed class NativeViewRegistry<T> where T : class
{
	readonly List<(WeakReference<T> Native, RetainedView Content)> views = new();

	public void Add(T view, RetainedView content)
	{
		Prune();
		var index = views.FindIndex(entry => entry.Native.TryGetTarget(out var existing) && ReferenceEquals(existing, view));
		if (index >= 0)
		{
			if (ReferenceEquals(views[index].Content, content))
				return;
			views[index].Content.Release();
			views.RemoveAt(index);
		}
		views.Add((new WeakReference<T>(view), content));
	}

	public void Drain(Action<T> release)
	{
		var entries = views.ToArray();
		views.Clear();
		foreach (var entry in entries)
		{
			if (entry.Native.TryGetTarget(out var view))
				release(view);
			entry.Content.Release();
		}
	}

	public void ForEach(Action<T> action)
	{
		Prune();
		foreach (var entry in views.ToArray())
			if (entry.Native.TryGetTarget(out var view))
				action(view);
	}

	void Prune()
	{
		foreach (var entry in views.Where(entry => !entry.Native.TryGetTarget(out _)).ToArray())
		{
			views.Remove(entry);
			entry.Content.Release();
		}
	}
}
