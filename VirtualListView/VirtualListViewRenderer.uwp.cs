using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms.Platform.UWP;

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

					listView.ChoosingItemContainer += ListView_ChoosingItemContainer;


					SetNativeControl(listView);
				}
			}
		}

		Dictionary<int, List<VirtualViewCell>> cachedTemplates = new Dictionary<int, List<VirtualViewCell>>();

		private void ListView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
		{
			var viewType = dataSource.GetItemViewType(args.ItemIndex);

			var info = templateSelector.GetInfo(Element.Adapter, args.ItemIndex);


			// Can we reuse the container?
			if (args?.ItemContainer?.Tag is int tagViewType && tagViewType == viewType)
			{
				if (args.ItemContainer.Content is UwpControlWrapper container)
				{
					container.ViewCell.Update(info);
				}

			}
			else
			{
				var c = new ListViewItem();
				c.Tag = viewType;

				var template = templateSelector.GetTemplate(Element.Adapter, args.ItemIndex);

				var viewCell = template.CreateContent() as VirtualViewCell;

				var container = new UwpControlWrapper(viewCell.View);
				container.ViewCell = viewCell;
				container.ViewCell.Update(info);

				c.Content = container;

				args.ItemContainer = c;
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
				// TODO:
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
				
				// TODO:
			}
		}


		internal class UwpDataSource
		{
			public UwpDataSource()
			{
				templates = new List<Forms.DataTemplate>();
			}

			public IVirtualListViewAdapter Adapter { get; set; }
			public PositionTemplateSelector TemplateSelector { get; set; }


			readonly List<Xamarin.Forms.DataTemplate> templates;
			readonly object lockObj = new object();

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

		}

	}
}
