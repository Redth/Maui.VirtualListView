using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

namespace Xamarin.CommunityToolkit.UI.Views
{
	internal class UwpControlWrapper : SelectorItem
	{
		readonly View _view;

		FrameworkElement FrameworkElement { get; }

		public VirtualViewCell ViewCell { get; set; }

		internal void CleanUp()
		{
			CleanupVisualElement(_view);

			if (_view != null)
				_view.MeasureInvalidated -= OnMeasureInvalidated;
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


		public UwpControlWrapper(View view)
		{
			_view = view;
			_view.MeasureInvalidated += OnMeasureInvalidated;

			IVisualElementRenderer renderer = Platform.CreateRenderer(view);
			Platform.SetRenderer(view, renderer);

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
					(_view as Layout)?.ForceLayout();
					InvalidateMeasure();
				};
			}
		}

		void OnMeasureInvalidated(object sender, EventArgs e)
		{
			InvalidateMeasure();
		}

		protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
		{
			_view.IsInNativeLayout = true;
			Layout.LayoutChildIntoBoundingRegion(_view, new Rectangle(0, 0, finalSize.Width, finalSize.Height));

			if (_view.Width <= 0 || _view.Height <= 0)
			{
				// Hide Panel when size _view is empty.
				// It is necessary that this element does not overlap other elements when it should be hidden.
				Opacity = 0;
			}
			else
			{
				Opacity = 1;
				FrameworkElement?.Arrange(new Windows.Foundation.Rect(_view.X, _view.Y, _view.Width, _view.Height));
			}
			_view.IsInNativeLayout = false;

			return finalSize;
		}

		protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
		{
			Size request = _view.Measure(availableSize.Width, availableSize.Height, MeasureFlags.IncludeMargins).Request;

			if (request.Height < 0)
			{
				request.Height = availableSize.Height;
			}

			Windows.Foundation.Size result;
			if (_view.HorizontalOptions.Alignment == LayoutAlignment.Fill && !double.IsInfinity(availableSize.Width) && availableSize.Width != 0)
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
