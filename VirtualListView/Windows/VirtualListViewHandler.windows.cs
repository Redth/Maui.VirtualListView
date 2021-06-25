using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.Maui
{
	public class VirtualListViewHandler : ViewHandler<IVirtualListView, ItemsRepeater>
	{

		public VirtualListViewHandler()
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
					dataSource.Container = e.NewElement;

					listView.ItemsSource = dataSource;

					//listView.Style = new Style(typeof(ListView));
					//listView.ItemContainerStyle = new Style(typeof(VirtualItemContentControl));
					//listView.ItemTemplate = Application.Current.Resources["ItemsViewDefaultTemplate"] as DataTemplate;
					listView.ChoosingItemContainer += ListView_ChoosingItemContainer;
					listView.ContainerContentChanging += ListView_ContainerContentChanging;

					SetNativeControl(listView);
				}
			}
		}

		private void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer is ListViewItem lvi)
			{
				if (lvi.Tag is WrapperItem wrapper)
				{

				}
				
			}
		}

		DataTemplate itemTemplate = Application.Current.Resources["ItemsViewDefaultTemplate"] as DataTemplate;

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
						lvi.InvalidateMeasure();
					}
				}
			}
			else
			{
				var template = templateSelector.GetTemplate(Element.Adapter, args.ItemIndex);

				var viewCell = template.CreateContent() as VirtualViewCell;
				
				var listViewItem = new ListViewItem();
				listViewItem.ContentTemplate = itemTemplate;

				args.ItemContainer = listViewItem;
				args.ItemContainer.Tag = new WrapperItem
				{
					ViewCell = viewCell,
					ViewType = viewType

				};
			}
		}


		public class WrapperItem
		{
			internal VirtualViewCell ViewCell { get; set; }
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

			public Forms.Element Container { get; set; }
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

					var template = TemplateSelector?.GetTemplate(Adapter, index);

					if (info.Kind == PositionKind.Item)
						return new VirtualItemTemplateContext(
							template,
							Adapter.Item(info.SectionIndex, info.ItemIndex),
							Container);
					else
					{
						if (info.SectionIndex >= 0)
							return new VirtualItemTemplateContext(
								template,
								Adapter.Section(info.SectionIndex),
								Container);
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

	internal class VirtualItemTemplateContext
	{
		public Forms.DataTemplate FormsDataTemplate { get; }
		public object Item { get; }
		public Forms.BindableObject Container { get; }
		
		public VirtualItemTemplateContext(Forms.DataTemplate formsDataTemplate, object item, Forms.BindableObject container)
		{
			FormsDataTemplate = formsDataTemplate;
			Item = item;
			Container = container;
		}
	}
}
