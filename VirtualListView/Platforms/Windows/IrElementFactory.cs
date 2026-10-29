using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Maui;

internal class IrElementFactory : IElementFactory, IDisposable
{
	readonly IMauiContext context;
	readonly PositionalViewSelector selector;
	readonly VirtualListViewHandler handler;
	readonly HashSet<IrElementContainer> active = new();
	readonly ReusableViewPool<IrElementContainer> pool = new(container => container.ContentOwner.Release());
	bool disposed;

	public IrElementFactory(IMauiContext context, PositionalViewSelector selector, VirtualListViewHandler handler)
	{
		this.context = context;
		this.selector = selector;
		this.handler = handler;
	}

	internal IEnumerable<IrElementContainer> ActiveElements => active;

	public UIElement GetElement(ElementFactoryGetArgs args)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		if (args.Data is not IrDataWrapper wrapper || wrapper.position is not PositionInfo info)
			throw new InvalidOperationException("The ItemsRepeater requested an invalid VirtualListView row.");

		var reuseId = selector.ViewSelector.GetReuseId(info, wrapper.data);
		var container = pool.Take(reuseId) ?? new IrElementContainer(context, reuseId, selector, handler);
		active.Add(container);
		container.UpdateOrientation();
		info.IsSelected = info.Kind == PositionKind.Item && handler.IsItemSelected(info.SectionIndex, info.ItemIndex);
		container.IsRecycled = false;
		try { container.ContentOwner.Bind(info, wrapper.data); }
		catch
		{
			active.Remove(container);
			container.ContentOwner.Release();
			throw;
		}
		return container;
	}

	public void RecycleElement(ElementFactoryRecycleArgs args)
	{
		if (args.Element is not IrElementContainer container || !active.Remove(container))
			return;
		container.IsRecycled = true;
		container.ContentOwner.Unbind();
		if (disposed)
			container.ContentOwner.Release();
		else
			pool.Return(container.ReuseId, container, active.Count);
	}

	public void ResetTemplates() => pool.Clear();

	public void Dispose()
	{
		if (disposed)
			return;
		disposed = true;
		var containers = active.ToArray();
		active.Clear();
		foreach (var container in containers)
		{
			container.IsRecycled = true;
			container.ContentOwner.Release();
		}
		pool.Dispose();
	}
}
