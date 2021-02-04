using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(Xamarin.CommunityToolkit.UI.Views.VirtualListView), typeof(Xamarin.CommunityToolkit.UI.Views.VirtualListViewRenderer))]

namespace Xamarin.CommunityToolkit.UI.Views
{
	public class VirtualListViewRenderer : ViewRenderer<VirtualListView, ListView>
	{

		public VirtualListViewRenderer()
			: base()
		{
		}

		PositionTemplateSelector templateSelector;

		ListView listView;

		UwpDataSource dataSource;

		protected override void OnElementChanged(Xamarin.Forms.Platform.UWP.ElementChangedEventArgs<VirtualListView> e)
		{
			base.OnElementChanged(e);

			// Clean up old
			if (e.OldElement != null)
			{
				// Unsubscribe from event handlers and cleanup any resources
				listView.ChoosingItemContainer -= ListView_ChoosingItemContainer;
				dataSource.Adapter = null;
				dataSource.TemplateSelector = null;
				dataSource = null;

				templateSelector = null;
				listView = null;
			}

			// Setup new
			if (e.NewElement != null)
			{
				// Create the native control
				if (Control == null)
				{
					listView = new ListView();

					templateSelector = CreateTemplateSelector();

					dataSource = new UwpDataSource();
					dataSource.TemplateSelector = templateSelector;
					dataSource.Adapter = e.NewElement.Adapter;

					listView.ItemsSource = dataSource;

					var style = new Style(typeof(ListView));
					
					listView.Style = style;
					listView.ItemContainerStyle = new Style(typeof(UwpListViewItem));
					
					listView.ChoosingItemContainer += ListView_ChoosingItemContainer;
					listView.ContainerContentChanging += ListView_ContainerContentChanging;

					SetNativeControl(listView);
				}
			}
		}

		static void Unparent(UIElement child)
		{
			DependencyObject visualParent = null;

			var fe = child as FrameworkElement;
			if (fe != null && fe.Parent != null)
			{
				visualParent = fe.Parent;
			}
			if (visualParent == null)
			{
				visualParent = Windows.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
			}
			var parentContent = visualParent as ContentControl;
			var parentPresenter = visualParent as ContentPresenter;
			var parentBorder = visualParent as Border;
			var parentPanel = visualParent as Panel;
			if (parentPanel != null)
			{
				parentPanel.Children.Remove(child);
			}
			else if (parentContent != null)
			{
				parentContent.Content = null;
			}
			else if (parentBorder != null)
			{
				parentBorder.Child = null;
			}
			else if (parentPresenter != null)
			{
				parentPresenter.Content = null;
			}
		}

		private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer is ListViewItem lvi)
			{
				if (lvi.Tag is WrapperItem wrapper)
				{

					var info = templateSelector.GetInfo(Element.Adapter, args.ItemIndex);

					wrapper.Control.Update(info);

					Unparent(wrapper.Control);

					lvi.Content = wrapper.Control;
					lvi.InvalidateMeasure();
				}
				
			}
		}

		private void ListView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
		{
			var viewType = dataSource.GetItemViewType(args.ItemIndex);

			var info = templateSelector.GetInfo(Element.Adapter, args.ItemIndex);


			// Can we reuse the container?
			if (args?.ItemContainer?.Tag is WrapperItem wrapper)
			{

				if (wrapper.ViewType == viewType)
				{
					if (args.ItemContainer is ListViewItem lvi)
					{
						wrapper.Control.Update(info);
						lvi.Content = wrapper.Control;
						lvi.InvalidateMeasure();
					}
				}
			}
			else
			{
				args.IsContainerPrepared = true;
				var template = templateSelector.GetTemplate(Element.Adapter, args.ItemIndex);

				var viewCell = template.CreateContent() as VirtualViewCell;

				var container = new UwpControlWrapper(viewCell.View);
				container.ViewCell = viewCell;
				container.Update(info);

				Unparent(container);


				//args.IsContainerPrepared = true;
				//c.Content = container;
				var listViewItem = new ListViewItem();
				listViewItem.Content = container;

				args.ItemContainer = listViewItem;
				args.ItemContainer.Tag = new WrapperItem
				{
					Control = container,
					ViewType = viewType

				};
				args.IsContainerPrepared = true;
			}
		}


		public class WrapperItem
		{
			internal UwpControlWrapper Control { get; set; }
			public int ViewType { get; set; }
		}

		PositionTemplateSelector CreateTemplateSelector()
			=> new PositionTemplateSelector
			{
				HeaderTemplate = Element.HeaderTemplate,
				FooterTemplate = Element.FooterTemplate,
				ItemTemplate = Element.ItemTemplate,
				ItemTemplateSelector = Element.ItemTemplateSelector,
				SectionFooterTemplate = Element.SectionFooterTemplate,
				SectionFooterTemplateSelector = Element.SectionFooterTemplateSelector,
				SectionHeaderTemplate = Element.SectionHeaderTemplate,
				SectionHeaderTemplateSelector = Element.SectionHeaderTemplateSelector
			};

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == VirtualListView.AdapterProperty.PropertyName)
			{
				dataSource.TemplateSelector = templateSelector;
				dataSource.Adapter = Element.Adapter;
				dataSource.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
			else if (e.PropertyName == VirtualListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				var templateSelector = CreateTemplateSelector();

				dataSource.TemplateSelector = templateSelector;
				dataSource.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}


		internal class UwpDataSource : IList, INotifyCollectionChanged
		{
			public UwpDataSource()
			{
				templates = new List<Forms.DataTemplate>();
			}

			public IVirtualListViewAdapter Adapter { get; set; }
			public PositionTemplateSelector TemplateSelector { get; set; }

			readonly List<Xamarin.Forms.DataTemplate> templates;
			readonly object lockObj = new object();

			public event NotifyCollectionChangedEventHandler CollectionChanged;

			internal void RaiseCollectionChanged(NotifyCollectionChangedEventArgs args)
				=> CollectionChanged?.Invoke(this, args);

			public int GetItemViewType(int position)
			{
				int viewType = 0;

				var template = TemplateSelector.GetTemplate(Adapter, position);

				lock (lockObj)
				{
					viewType = templates.IndexOf(template);

					if (viewType < 0)
					{
						templates.Add(template);
						viewType = templates.Count - 1;
					}
				}

				return viewType;
			}

			public bool IsFixedSize
				=> true;

			public bool IsReadOnly
				=> true;

			public int Count
				=> TemplateSelector?.GetTotalCount(Adapter) ?? 0;

			public bool IsSynchronized
				=> false;

			public object SyncRoot
				=> null;

			public object this[int index]
			{
				get
				{
					var info = TemplateSelector?.GetInfo(Adapter, index);

					if (info == null)
						return null;

					if (info.Kind == PositionKind.Item)
						return Adapter.Item(info.SectionIndex, info.ItemIndex);
					else
					{
						if (info.SectionIndex >= 0)
							return Adapter.Section(info.SectionIndex);
						else
							return null;
					}
						
				}
				set => throw new NotImplementedException();
			}

			public int Add(object value)
				=> throw new NotImplementedException();

			public void Clear()
				=> throw new NotImplementedException();

			public bool Contains(object value)
				=> throw new NotImplementedException();

			public int IndexOf(object value)
				=> throw new NotImplementedException();

			public void Insert(int index, object value)
				=> throw new NotImplementedException();

			public void Remove(object value)
				=> throw new NotImplementedException();

			public void RemoveAt(int index)
				=> throw new NotImplementedException();

			public void CopyTo(Array array, int index)
				=> throw new NotImplementedException();

			public IEnumerator GetEnumerator()
				=> throw new NotImplementedException();
		}

	}
}
