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

		CvDataSource dataSource;
		UICollectionView collectionView;
		CvFlowDelegate flowDelegate;

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
					var layout = new UICollectionViewFlowLayout();
					layout.EstimatedItemSize = new CGSize(1f, 1f);
					layout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
					layout.MinimumInteritemSpacing = 0f;
					layout.MinimumLineSpacing = 0f;
					
					flowDelegate = new CvFlowDelegate();
					dataSource = new CvDataSource(e.NewElement.Adapter);
					collectionView = new UICollectionView(this.Frame, layout);
					collectionView.DataSource = dataSource;
					collectionView.Delegate = flowDelegate;
					collectionView.ContentInset = new UIEdgeInsets(0, 0, 0, 0);

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
				collectionView.CollectionViewLayout.InvalidateLayout();
			}
		}
	}

	internal static class CvConsts
	{
		public static NSString ElementKindSectionHeader
			=> new NSString("UICollectionElementKindSectionHeader");
		public static NSString ElementKindSectionFooter
			=> new NSString("UICollectionElementKindSectionFooter");
	}

	internal class CvFlowDelegate : UICollectionViewDelegateFlowLayout
	{
		public override CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
		{
			var cell = collectionView.DataSource.GetCell(collectionView, indexPath)
				as SlimListViewCollectionViewCell;

			if (cell != null)
			{
				var formsSize = cell.FormsView.Measure(collectionView.Frame.Width, double.MaxValue - 1000, MeasureFlags.IncludeMargins);

				return new CGSize(Math.Max(formsSize.Request.Width, collectionView.Frame.Width), formsSize.Request.Height);
			}

			return new CGSize();
		}

		public override CGSize GetReferenceSizeForHeader(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			var header = collectionView.DataSource.GetViewForSupplementaryElement(collectionView,
				new NSString(CvConsts.ElementKindSectionHeader), NSIndexPath.FromItemSection(0, section))
				as SlimListViewCollectionReusableView;

			if (header != null)
			{
				var formsSize = header.FormsView.Measure(collectionView.Frame.Width, double.MaxValue - 1000, MeasureFlags.IncludeMargins);

				return new CGSize(Math.Max(formsSize.Request.Width, collectionView.Frame.Width), formsSize.Request.Height);
			}

			return new CGSize();
		}

		public override CGSize GetReferenceSizeForFooter(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			var footer = collectionView.DataSource.GetViewForSupplementaryElement(collectionView,
				new NSString(CvConsts.ElementKindSectionFooter), NSIndexPath.FromItemSection(0, section))
				as SlimListViewCollectionReusableView;

			var formsSize = footer.FormsView.Measure(collectionView.Frame.Width, double.MaxValue - 1000, MeasureFlags.IncludeMargins);

			return new CGSize(Math.Max(formsSize.Request.Width, collectionView.Frame.Width), formsSize.Request.Height);
		}
	}

	internal class CvDataSource : UICollectionViewDataSource
	{
		public CvDataSource(ISlimListViewAdapter adapter)
		{
			Adapter = adapter;
		}

		public ISlimListViewAdapter Adapter { get; set; }
		internal PositionTemplateSelector TemplateSelector { get; set; }

		readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
		readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("UICollectionElementKindSectionHeader"));
		readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("UICollectionElementKindSectionFooter"));

		public override nint NumberOfSections(UICollectionView collectionView)
			=> Adapter?.Sections ?? 0;

		public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
		{
			DataTemplate template = null;
			NSString reuseId = null;

			if (elementKind == CvConsts.ElementKindSectionHeader)
			{
				template = TemplateSelector?.SectionHeaderTemplateSelector?.SelectGroupTemplate(Adapter, indexPath.Section)
					?? TemplateSelector.SectionHeaderTemplate;

				reuseId = sectionHeaderIdManager.GetReuseId(collectionView, template);
			}
			else if (elementKind == CvConsts.ElementKindSectionFooter)
			{
				template = TemplateSelector?.SectionFooterTemplateSelector?.SelectGroupTemplate(Adapter, indexPath.Section)
					?? TemplateSelector.SectionFooterTemplate;

				reuseId = sectionFooterIdManager.GetReuseId(collectionView, template);
			}

			if (template == null || reuseId == null)
				return new UICollectionReusableView();

			var supplementary =
					collectionView.DequeueReusableSupplementaryView(
						elementKind,
						reuseId,
						indexPath) as SlimListViewCollectionReusableView;

			supplementary.IsHeader = elementKind == CvConsts.ElementKindSectionHeader;

			supplementary.EnsureFormsTemplate(template);

			var bindingContext = Adapter.Section(indexPath.Section);
			supplementary.UpdateFormsBindingContext(bindingContext);

			return supplementary;
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
			//if (TemplateSelector.HeaderTemplate != null)
			//	return 1;
			//if (TemplateSelector.FooterTemplate != null)
			//	return 1;

			return (nint)Adapter.ItemsForSection((int)section);
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			itemIdManager.ResetTemplates(collectionView);
			sectionHeaderIdManager.ResetTemplates(collectionView);
			sectionFooterIdManager.ResetTemplates(collectionView);
		}
	}

	public class CvFlowLayout : UICollectionViewFlowLayout
	{
		public CvFlowLayout() : base()
		{
		}

		public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
			=> true;

		public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath path)
			=> base.LayoutAttributesForItem(path);

		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
			=> base.LayoutAttributesForElementsInRect(rect);
	}

	public class SlimListViewCollectionViewCell : UICollectionViewCell
	{
		public object FormsBindingContext { get; private set; }
		public View FormsView { get; private set; }

		UIContainerView containerView = null;

		[Export("initWithFrame:")]
		public SlimListViewCollectionViewCell(CGRect frame) : base(frame)
		{
		}

		public void EnsureFormsTemplate(DataTemplate template)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as View;

			if (containerView == null)
			{
				containerView = new UIContainerView(FormsView)
				{
					Frame = ContentView.Frame,
					AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth
				};

				ContentView.AddSubview(containerView);
			}
		}

		public void UpdateFormsBindingContext(object bindingContext)
		{
			if (FormsView != null)
			{
				FormsView.BindingContext = bindingContext;
				FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
			}
		}
	}

	public class SlimListViewCollectionReusableView : UICollectionReusableView
	{
		public object FormsBindingContext { get; private set; }
		public View FormsView { get; private set; }

		UIContainerView containerView = null;

		public bool IsHeader { get; set; } = false;

		[Export("initWithFrame:")]
		public SlimListViewCollectionReusableView(CGRect frame) : base(frame)
		{
		}

		public void EnsureFormsTemplate(DataTemplate template)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as View;

			if (containerView == null)
			{
				containerView = new UIContainerView(FormsView)
				{
					Frame = this.Frame,
					AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
				};

				AddSubview(containerView);
			}
		}

		public void UpdateFormsBindingContext(object bindingContext)
		{
			if (FormsView != null)
			{
				FormsView.BindingContext = bindingContext;
				FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
			}
		}
	}

	internal class ReusableIdManager
	{
		public ReusableIdManager(string uniquePrefix, NSString supplementaryKind = null)
		{
			UniquePrefix = uniquePrefix;
			SupplementaryKind = supplementaryKind;
			templates = new List<DataTemplate>();
			lockObj = new object();
		}

		public string UniquePrefix { get; }
		public NSString SupplementaryKind { get; }

		readonly List<DataTemplate> templates;
		readonly object lockObj;

		NSString GetReuseId(int i)
			=> new NSString($"_{UniquePrefix}_{nameof(SlimListView)}_{i}");

		public NSString GetReuseId(UICollectionView collectionView, DataTemplate template)
		{
			var viewType = 0;

			lock (lockObj)
			{
				viewType = templates.IndexOf(template);

				if (viewType < 0)
				{
					templates.Add(template);
					viewType = templates.Count - 1;

					if (SupplementaryKind != null)
						collectionView.RegisterClassForSupplementaryView(
							typeof(SlimListViewCollectionReusableView),
							SupplementaryKind,
							GetReuseId(viewType));
					else
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
				{
					if (SupplementaryKind != null)
						collectionView.RegisterClassForSupplementaryView(null, SupplementaryKind, GetReuseId(i));
					else
						collectionView.RegisterClassForCell(null, GetReuseId(i));
				}

				templates.Clear();
			}
		}
	}
}
