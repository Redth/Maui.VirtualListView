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
	internal class UwpControlWrapper : ContentControl
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


		IVisualElementRenderer renderer = null;

		public UwpControlWrapper(View view)
		{
			_view = view;
			_view.MeasureInvalidated += OnMeasureInvalidated;

			renderer = Platform.CreateRenderer(view);
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

		public void Update(PositionInfo info)
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


	public class MyDataTemplate : Windows.UI.Xaml.DataTemplate
	{
		public MyDataTemplate()
		{
			
		}
	}
	public class UwpListViewItem : SelectorItem
	{
		IVisualElementRenderer _renderer;
		public VirtualViewCell ViewCell { get; }

		public UwpListViewItem(VirtualViewCell viewCell)
		{
			ViewCell = viewCell;
			DefaultStyleKey = typeof(UwpListViewItem);
		}

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);

			if (oldContent != null && ViewCell?.View != null)
				ViewCell.View.MeasureInvalidated -= OnViewMeasureInvalidated;

			if (newContent != null && ViewCell?.View != null)
				ViewCell.View.MeasureInvalidated += OnViewMeasureInvalidated;
		}

		internal void Realize()
		{
			if (_renderer?.ContainerElement == null)
			{
				// If the content has never been realized (i.e., this is a new instance), 
				// or if we need to switch DataTemplates (because this instance is being recycled)
				// then we'll need to create the content from the template 
				
				_renderer = Platform.CreateRenderer(ViewCell.View);
				Platform.SetRenderer(ViewCell.View, _renderer);

				// We need to set IsNativeStateConsistent explicitly; otherwise, it won't be set until the renderer's Loaded 
				// event. If the CollectionView is in a Layout, the Layout won't measure or layout the CollectionView until
				// every visible descendant has `IsNativeStateConsistent == true`. And the problem that Layout is trying
				// to avoid by skipping layout for controls with not-yet-loaded children does not apply to CollectionView
				// items. If we don't set this, the CollectionView just won't get layout at all, and will be invisible until
				// the window is resized. 
				SetNativeStateConsistent(ViewCell.View);

				//RegisterPropertyChangedCallback(ContentProperty, new DependencyPropertyChangedCallback(
				//	(dobj, dp) =>
				//{
				//	Console.WriteLine("Content changed");
				//}));
			}

			InvalidateMeasure();
			
			Content = _renderer.ContainerElement;
		}

		void SetNativeStateConsistent(VisualElement visualElement)
		{
			visualElement.IsNativeStateConsistent = true;

			foreach (var child in visualElement.LogicalChildren)
			{
				if (!(child is VisualElement ve))
				{
					continue;
				}

				SetNativeStateConsistent(ve);
			}
		}

		
		void OnViewMeasureInvalidated(object sender, EventArgs e)
		{
			InvalidateMeasure();
		}

		protected override WSize MeasureOverride(WSize availableSize)
		{
			if (_renderer == null)
			{
				return base.MeasureOverride(availableSize);
			}

			var frameworkElement = Content as FrameworkElement;

			var formsElement = _renderer.Element;
			
			var (width, height) = formsElement.Measure(availableSize.Width, availableSize.Height,
				MeasureFlags.IncludeMargins).Request;

			width = Max(width, availableSize.Width);
			height = Max(height, availableSize.Height);

			formsElement.Layout(new Rectangle(0, 0, width, height));

			var wsize = new WSize(width, height);

			frameworkElement?.Measure(wsize);

			return base.MeasureOverride(wsize);
		}

		double Max(double requested, double available)
		{
			return Math.Max(requested, ClampInfinity(available));
		}

		double ClampInfinity(double value)
		{
			return double.IsInfinity(value) ? 0 : value;
		}
	}
}
