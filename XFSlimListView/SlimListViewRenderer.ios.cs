using System;
using System.Collections.Generic;
using System.ComponentModel;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(XFSlimListView.SlimListView), typeof(XFSlimListView.SlimListViewRenderer))]

namespace XFSlimListView
{
	public class SlimListViewRenderer : ViewRenderer<SlimListView, UICollectionView>
	{
		public SlimListViewRenderer()
		{
		}

		CollectionViewDataSource dataSource;
		UICollectionView collectionView;

		protected override void OnElementChanged(ElementChangedEventArgs<SlimListView> e)
		{
			base.OnElementChanged(e);

			// Clean up old
			if (e.OldElement != null)
			{
				
			}

			// Setup new
			if (e.NewElement != null)
			{
				// Create the native control
				if (Control == null)
				{
					var layout = UICollectionViewCompositionalLayout.GetLayout(
						new UICollectionLayoutListConfiguration(UICollectionLayoutListAppearance.Plain));

					Console.WriteLine("Creating collection view");

					dataSource = new CollectionViewDataSource(e.NewElement.Adapter);
					collectionView = new UICollectionView(this.Frame, layout);
					collectionView.DataSource = dataSource;

					dataSource.TemplateSelector = CreateTemplateSelector();

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

			if (e.PropertyName == SlimListView.AdapterProperty.PropertyName)
				collectionView.ReloadData();
			else if (e.PropertyName == SlimListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				dataSource.ResetTemplates(collectionView);
				dataSource.TemplateSelector = CreateTemplateSelector();
				collectionView.ReloadData();
			}
		}
	}

	internal class CollectionViewDataSource : UICollectionViewDataSource
	{
		public CollectionViewDataSource(ISlimListViewAdapter adapter)
		{
			Adapter = adapter;
		}

		public ISlimListViewAdapter Adapter { get; set; }
		internal PositionTemplateSelector TemplateSelector { get; set; }

		readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
		readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader");
		readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter");

		public override nint NumberOfSections(UICollectionView collectionView)
		{
			var n = Adapter?.Sections ?? 0;

			if (TemplateSelector.HeaderTemplate != null)
				n++;
			if (TemplateSelector.FooterTemplate != null)
				n++;

			return n;
		}

		public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
		{
			DataTemplate template = null;
			NSString reuseId = null;

			if (elementKind == "UICollectionElementKindSectionHeader")
			{
				template = TemplateSelector?.SectionHeaderTemplateSelector?.SelectGroupTemplate(Adapter, indexPath.Section)
					?? TemplateSelector.SectionHeaderTemplate;

				reuseId = sectionHeaderIdManager.GetReuseId(collectionView, template);
			}
			else if (elementKind == "UICollectionElementKindSectionHeader")
			{
				template = TemplateSelector?.SectionFooterTemplateSelector?.SelectGroupTemplate(Adapter, indexPath.Section)
					?? TemplateSelector.SectionFooterTemplate;

				reuseId = sectionFooterIdManager.GetReuseId(collectionView, template);
			}

			if (template == null || reuseId == null)
				return new UICollectionReusableView();

			var header =
					collectionView.DequeueReusableSupplementaryView(
						UICollectionElementKindSection.Header,
						reuseId,
						indexPath) as SlimListViewCollectionViewCell;

			header.EnsureFormsTemplate(template);

			var headerBindingContext = Adapter.Section(indexPath.Section);
			header.UpdateFormsBindingContext(headerBindingContext);

			return header;
		}


		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var sectionIndex = indexPath.Section;
			var itemIndex = (int)indexPath.Item;

			var item = Adapter.Item(sectionIndex, itemIndex);

			var template = TemplateSelector?.ItemTemplateSelector?.SelectItemTemplate(Adapter, sectionIndex, itemIndex)
				?? TemplateSelector.ItemTemplate;

			var reuseId = itemIdManager.GetReuseId(collectionView, template);

			var cell = collectionView.DequeueReusableCell(reuseId, indexPath)
				as SlimListViewCollectionViewCell;

			cell.EnsureFormsTemplate(template);
			cell.UpdateFormsBindingContext(item);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			if (TemplateSelector.HeaderTemplate != null)
				return 1;
			if (TemplateSelector.FooterTemplate != null)
				return 1;

			return (nint)Adapter.ItemsForSection((int)section);
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			itemIdManager.ResetTemplates(collectionView);
			sectionHeaderIdManager.ResetTemplates(collectionView);
			sectionFooterIdManager.ResetTemplates(collectionView);
		}
	}

	public class SlimListViewCollectionViewCell : UICollectionViewCell
	{
		public object FormsBindingContext { get; private set; }
		public Xamarin.Forms.View FormsView { get; private set; }

		UIContainerView containerView = null;

		[Export("initWithFrame:")]
		public SlimListViewCollectionViewCell(CGRect frame) : base(frame)
		{
		}

		public void EnsureFormsTemplate(Xamarin.Forms.DataTemplate template)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as Xamarin.Forms.View;

			if (containerView == null)
			{
				containerView = new UIContainerView(FormsView);
				AddSubview(containerView);
			}
		}

		public void UpdateFormsBindingContext(object bindingContext)
		{
			if (FormsView != null)
				FormsView.BindingContext = bindingContext;
		}
	}


	public class SlimListViewCollectionReusableView : UICollectionReusableView
	{
		public object FormsBindingContext { get; private set; }
		public Xamarin.Forms.View FormsView { get; private set; }

		UIContainerView containerView = null;

		[Export("initWithFrame:")]
		public SlimListViewCollectionReusableView(CGRect frame) : base(frame)
		{
		}

		public void EnsureFormsTemplate(Xamarin.Forms.DataTemplate template)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as Xamarin.Forms.View;

			if (containerView == null)
			{
				containerView = new UIContainerView(FormsView);
				AddSubview(containerView);
			}
		}

		public void UpdateFormsBindingContext(object bindingContext)
		{
			if (FormsView != null)
				FormsView.BindingContext = bindingContext;
		}
	}

	internal class ReusableIdManager
	{
		public ReusableIdManager(string uniquePrefix)
		{
			UniquePrefix = uniquePrefix;
			templates = new List<Xamarin.Forms.DataTemplate>();
			lockObj = new object();
		}

		public string UniquePrefix { get; }

		readonly List<Xamarin.Forms.DataTemplate> templates;
		readonly object lockObj;


		NSString GetReuseId(int i)
			=> new NSString($"_{UniquePrefix}_{nameof(SlimListView)}_{i}");

		public NSString GetReuseId(UICollectionView collectionView, Xamarin.Forms.DataTemplate template)
		{
			var viewType = 0;

			lock (lockObj)
			{
				viewType = templates.IndexOf(template);

				if (viewType < 0)
				{
					templates.Add(template);
					viewType = templates.Count - 1;

					collectionView.RegisterClassForCell(
						typeof(SlimListViewCollectionViewCell),
						GetReuseId(viewType));
				}
			}

			return GetReuseId(viewType);
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			lock (lockObj)
			{
				for (int i = 0; i < templates.Count; i++)
					collectionView.RegisterClassForCell(null, GetReuseId(i));

				templates.Clear();
			}
		}
	}
}
