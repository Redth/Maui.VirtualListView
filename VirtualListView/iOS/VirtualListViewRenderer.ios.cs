using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	public partial class VirtualListViewRenderer : ViewRenderer<VirtualListView, UICollectionView>
	{
		public VirtualListViewRenderer()
		{
		}

		internal CvDataSource DataSource;
		CvLayout layout;
		CvDelegate cvdelegate;
		UICollectionView collectionView;

		internal PositionTemplateSelector TemplateSelector;
		internal IVirtualListViewAdapter Adapter;

		protected override void OnElementChanged(ElementChangedEventArgs<VirtualListView> e)
		{
			base.OnElementChanged(e);

			// Clean up old
			if (e.OldElement != null)
			{
				
			}

			// Setup new
			if (e.NewElement != null)
			{
				TemplateSelector = CreateTemplateSelector();
				Adapter = e.NewElement.Adapter;

				// Create the native control
				if (Control == null)
				{
					layout = new CvLayout();
					layout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;
					layout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
					layout.MinimumInteritemSpacing = 0f;
					layout.MinimumLineSpacing = 0f;

					DataSource = new CvDataSource(this);
					DataSource.IsSelectedHandler = (realSection, realIndex) =>
						e?.NewElement?.IsItemSelected(realSection, realIndex) ?? false;

					cvdelegate = new CvDelegate(this);
					cvdelegate.ScrollHandler = (x, y) =>
						Element?.RaiseScrolled(new ScrolledEventArgs(x, y));

					collectionView = new UICollectionView(this.Frame, layout);
					collectionView.AllowsSelection = e.NewElement.SelectionMode != SelectionMode.None;
					collectionView.AllowsMultipleSelection = e.NewElement.SelectionMode == SelectionMode.Multiple;
					collectionView.DataSource = DataSource;
					collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);
					collectionView.Delegate = cvdelegate;

					SetNativeControl(collectionView);

					collectionView.ReloadData();
				}
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
				collectionView.ReloadData();
			else if (e.PropertyName == VirtualListView.SelectionModeProperty.PropertyName)
			{
				collectionView.AllowsSelection = Element.SelectionMode != SelectionMode.None;
				collectionView.AllowsMultipleSelection = Element.SelectionMode == SelectionMode.Multiple;
			}
			else if (e.PropertyName == VirtualListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == VirtualListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				TemplateSelector = CreateTemplateSelector();

				DataSource.ResetTemplates(collectionView);

				collectionView.ReloadData();
				collectionView.CollectionViewLayout.InvalidateLayout();
			}
		}
	}
}
