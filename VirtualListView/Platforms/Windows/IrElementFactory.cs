using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui;

internal class IrElementFactory : IElementFactory, IDisposable
{
	public IrElementFactory(IMauiContext context, PositionalViewSelector positionalViewSelector)
	{
		MauiContext = context;
		PositionalViewSelector = positionalViewSelector;
	}

	readonly object lockObj = new object();
	protected readonly IMauiContext MauiContext;

	protected readonly PositionalViewSelector PositionalViewSelector;

	internal record IrRecycledElement(string reuseId, UIElement element);

	Dictionary<string, Queue<IrElementContainer>> recycledElements = new();


	IrElementContainer GetRecycledElement(string reuseId)
	{
		lock (lockObj)
		{
			if (!recycledElements.TryGetValue(reuseId, out var queue))
			{
				queue = new Queue<IrElementContainer>();
				recycledElements[reuseId] = queue;
			}

			if (queue.TryDequeue(out var element))
				return element;
		}

		return null;
	}

	void AddRecycledElement(IrElementContainer element)
	{
		var reuseId = element.ReuseId;

		lock (lockObj)
		{
			if (!recycledElements.TryGetValue(reuseId, out var queue))
			{
				queue = new Queue<IrElementContainer>();
				recycledElements[reuseId] = queue;
			}

			queue.Enqueue(element);
		}
	}


	public UIElement GetElement(UI.Xaml.ElementFactoryGetArgs args)
	{
		if (args.Data is IrDataWrapper dataWrapper)
		{
			var info = dataWrapper.position;
			if (info == null)
				return null;

			var data = dataWrapper.data;
			var reuseId = PositionalViewSelector.ViewSelector?.GetReuseId(info, data);

			var container = GetRecycledElement(reuseId)
				?? new IrElementContainer(MauiContext, reuseId, PositionalViewSelector);

			var view = container.VirtualView ?? PositionalViewSelector.ViewSelector?.CreateView(info, data);

			container.Update(info, view);

			container.IsRecycled = false;
			PositionalViewSelector.ViewSelector?.RecycleView(info, data, view);

			PositionalViewSelector.ViewSelector?.ViewAttached(info, view);

			return container;
		}

		return null;
	}

	public void RecycleElement(UI.Xaml.ElementFactoryRecycleArgs args)
	{
		if (args.Element is IrElementContainer container && container != null)
		{
			container.IsRecycled = true;

			PositionalViewSelector.ViewSelector.ViewDetached(container.PositionInfo, container.VirtualView);

			AddRecycledElement(container);
		}
	}

	public void Reset()
	{
		lock (lockObj)
			recycledElements.Clear();
	}

	public void Dispose()
	{
		lock (lockObj)
			recycledElements.Clear();
	}
}
