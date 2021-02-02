using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

					listView.ChoosingItemContainer += ListView_ChoosingItemContainer;


					SetNativeControl(listView);
				}
			}
		}

		private void ListView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
		{
			var viewType = dataSource.GetItemViewType(args.ItemIndex);

			var info = templateSelector.GetInfo(Element.Adapter, args.ItemIndex);


			// Can we reuse the container?
			if (args?.ItemContainer?.Tag is int tagViewType && tagViewType == viewType)
			{
				if (args.ItemContainer is UwpControlWrapper container)
				{
					container.ViewCell.Update(info);
				}
			}
			else
			{
				var template = templateSelector.GetTemplate(Element.Adapter, args.ItemIndex);

				var viewCell = template.CreateContent() as VirtualViewCell;

				var container = new UwpControlWrapper(viewCell.View);
				container.ViewCell = viewCell;
				container.ViewCell.Update(info);
				args.IsContainerPrepared = true;
				
				//c.Content = container;

				args.ItemContainer = container;
				args.ItemContainer.Tag = viewType;
			}
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
						return Adapter.Section(info.SectionIndex);
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
