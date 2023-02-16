using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui
{
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

		List<IrElementContainer> recycledElements = new();

		int createdCount = 0;

		public UIElement GetElement(UI.Xaml.ElementFactoryGetArgs args)
		{
			if (args.Data is IrDataWrapper dataWrapper)
			{
				var info = dataWrapper.position;
				if (info == null)
					return null;

				var data = PositionalViewSelector.Adapter.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);

				var reuseId = PositionalViewSelector?.ViewSelector?.GetReuseId(info, data);

				IrElementContainer container;

				lock (lockObj)
				{
					container = recycledElements?.FirstOrDefault(re => re.ReuseId == reuseId);

					if (container == null)
					{
						createdCount++;
						container = new IrElementContainer(MauiContext, reuseId, PositionalViewSelector, data);
						System.Diagnostics.Debug.WriteLine($"Creating: {reuseId},count: {createdCount}");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"Reused: {reuseId}");
					}
				}

				var view = PositionalViewSelector?.ViewSelector?.CreateView(info, data);

				if (view is IPositionInfo viewWithPositionInfo)
					viewWithPositionInfo.Update(info);

				container.Update(info, data, view);

				PositionalViewSelector.ViewSelector.ViewAttached(info, view);

				PositionalViewSelector?.ViewSelector?.RecycleView(info, data, view);


				return container;
			}

			return null;
		}

		public void RecycleElement(UI.Xaml.ElementFactoryRecycleArgs args)
		{
			if (args.Element is IrElementContainer container && container != null)
			{
				PositionalViewSelector.ViewSelector.ViewDetached(container.PositionInfo, container.VirtualView);

				//PositionalViewSelector.ViewSelector.RecycleView(container.PositionInfo, container.Data, container.VirtualView);
				lock (lockObj)
					recycledElements.Add(container);

				System.Diagnostics.Debug.WriteLine($"Adding to Recycle: {container.ReuseId}");
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
}
