using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;
using WSize = Windows.Foundation.Size;

namespace Xamarin.CommunityToolkit.UI.Views
{
	public class UwpControlWrapper : ContentControl
	{
		FrameworkElement FrameworkElement { get; set; }

		public VirtualViewCell ViewCell { get; set; }

		internal void CleanUp()
		{
			CleanupVisualElement(ViewCell.View);

			if (ViewCell.View != null)
				ViewCell.View.MeasureInvalidated -= OnMeasureInvalidated;
		}

		internal void CleanupVisualElement(VisualElement self)
		{
			if (self == null)
				throw new ArgumentNullException("self");

			IVisualElementRenderer renderer = Platform.GetRenderer(self);

			foreach (Element element in self.Descendants())
			{
				var visual = element as VisualElement;
				if (visual == null)
					continue;

				IVisualElementRenderer childRenderer = Platform.GetRenderer(visual);
				if (childRenderer != null)
				{
					childRenderer.Dispose();
					Platform.SetRenderer(visual, null);
				}
			}

			if (renderer != null)
			{
				renderer.Dispose();
				Platform.SetRenderer(self, null);
			}
		}


		IVisualElementRenderer renderer = null;

		public UwpControlWrapper()
			: base()
		{
			
		}

		internal void Setup(ViewCell viewCell)
		{
			viewCell.View.MeasureInvalidated += OnMeasureInvalidated;

			renderer = Platform.CreateRenderer(viewCell.View);
			Platform.SetRenderer(viewCell.View, renderer);

			FrameworkElement = renderer.ContainerElement;

			//Children.Add(renderer.ContainerElement);
			Content = renderer.ContainerElement;

			// make sure we re-measure once the template is applied
			if (FrameworkElement != null)
			{
				FrameworkElement.Loaded += (sender, args) =>
				{
					// If the view is a layout (stacklayout, grid, etc) we need to trigger a layout pass
					// with all the controls in a consistent native state (i.e., loaded) so they'll actually
					// have Bounds set
					(ViewCell.View as Layout)?.ForceLayout();
					InvalidateMeasure();
				};
			}
		}

		internal void Update(PositionInfo info)
		{
			ViewCell.Update(info);
			Content = renderer.ContainerElement;

			InvalidateMeasure();
		}

		void OnMeasureInvalidated(object sender, EventArgs e)
		{
			InvalidateMeasure();
		}

		protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
		{
			if (ViewCell?.View == null)
				return finalSize;

			ViewCell.View.IsInNativeLayout = true;
			Layout.LayoutChildIntoBoundingRegion(ViewCell.View, new Rectangle(0, 0, finalSize.Width, finalSize.Height));

			if (ViewCell.View.Width <= 0 || ViewCell.View.Height <= 0)
			{
				// Hide Panel when size _view is empty.
				// It is necessary that this element does not overlap other elements when it should be hidden.
				Opacity = 0;
			}
			else
			{
				Opacity = 1;
				FrameworkElement?.Arrange(new Windows.Foundation.Rect(ViewCell.View.X, ViewCell.View.Y, ViewCell.View.Width, ViewCell.View.Height));
			}
			ViewCell.View.IsInNativeLayout = false;

			return finalSize;
		}

		protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
		{
			if (ViewCell?.View == null)
				return new Windows.Foundation.Size(availableSize.Width, availableSize.Height);

			Size request = ViewCell.View.Measure(availableSize.Width, availableSize.Height, MeasureFlags.IncludeMargins).Request;

			if (request.Height < 0)
			{
				request.Height = availableSize.Height;
			}

			Windows.Foundation.Size result;
			if (ViewCell.View.HorizontalOptions.Alignment == LayoutAlignment.Fill && !double.IsInfinity(availableSize.Width) && availableSize.Width != 0)
			{
				result = new Windows.Foundation.Size(availableSize.Width, request.Height);
			}
			else
			{
				result = new Windows.Foundation.Size(request.Width, request.Height);
			}

			FrameworkElement?.Measure(availableSize);

			return result;
		}
	}
}
