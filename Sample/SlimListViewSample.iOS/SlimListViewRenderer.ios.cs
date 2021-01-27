using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Essentials;
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
		CvLayout layout;
		CvDelegate cvdelegate;
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
					var templateSelector = CreateTemplateSelector();

					layout = new CvLayout();
					layout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;
					layout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
					layout.MinimumInteritemSpacing = 0f;
					layout.MinimumLineSpacing = 0f;
					layout.TemplateSelector = templateSelector;

					dataSource = new CvDataSource(e.NewElement.Adapter);
					dataSource.TemplateSelector = templateSelector;

					cvdelegate = new CvDelegate();
					cvdelegate.TemplateSelector = templateSelector;

					collectionView = new UICollectionView(this.Frame, layout);
					collectionView.DataSource = dataSource;
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

			if (e.PropertyName == SlimListView.AdapterProperty.PropertyName)
				collectionView.ReloadData();
			else if (e.PropertyName == SlimListView.HeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.FooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.ItemTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionFooterTemplateSelectorProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateProperty.PropertyName
				|| e.PropertyName == SlimListView.SectionHeaderTemplateSelectorProperty.PropertyName)
			{
				var templateSelector = CreateTemplateSelector();

				dataSource.ResetTemplates(collectionView);
				dataSource.TemplateSelector = templateSelector;

				layout.TemplateSelector = templateSelector;

				cvdelegate.TemplateSelector = templateSelector;

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


		int? cachedNumberOfSections;
		internal int CachedNumberOfSections
		{
			get
			{
				if (!cachedNumberOfSections.HasValue)
				{
					var s = Adapter.Sections;

					if (TemplateSelector?.HeaderTemplate != null)
						s++;

					if (TemplateSelector?.FooterTemplate != null)
						s++;

					cachedNumberOfSections = s;
				}

				return cachedNumberOfSections.Value;
			}
		}

		public override nint NumberOfSections(UICollectionView collectionView)
			=> CachedNumberOfSections;

		public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
		{
			var section = indexPath.Section;

			var addedSections = 0;
			var realSection = section;

			// Empty section header for the global header section/cell
			if (TemplateSelector?.HasGlobalHeader ?? false)
			{
				if (section == 0)
					return new UICollectionReusableView();

				addedSections++;
				realSection--;
			}

			if (TemplateSelector?.HasGlobalFooter ?? false)
			{
				addedSections++;

				if (realSection >= CachedNumberOfSections - addedSections)
					return new UICollectionReusableView();
			}

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

			var bindingContext = Adapter.Section(realSection);
			supplementary.UpdateFormsBindingContext(bindingContext);

			return supplementary;
		}


		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var section = indexPath.Section;
			var adapterSection = section;
			var itemIndex = (int)indexPath.Item;

			DataTemplate template;
			object item;
			NSString reuseId;

			// If we had a section for the global header, all our sections are
			// off by one and actually + 1 from the data source's perspective
			if (TemplateSelector?.HeaderTemplate != null)
				adapterSection++;

			if (section == 0 && TemplateSelector?.HeaderTemplate != null)
			{
				template = TemplateSelector.HeaderTemplate;
				item = Adapter.Section(adapterSection);
				reuseId = itemIdManager.GetReuseId(collectionView, template);
			}
			else if (section == (CachedNumberOfSections - 1) && TemplateSelector?.FooterTemplate != null)
			{
				template = TemplateSelector.FooterTemplate;
				item = Adapter.Section(adapterSection);
				reuseId = itemIdManager.GetReuseId(collectionView, template);
			}
			else
			{
				template = TemplateSelector?.ItemTemplateSelector?.SelectItemTemplate(Adapter, adapterSection, itemIndex)
					?? TemplateSelector.ItemTemplate;
				item = Adapter.Item(adapterSection, itemIndex);
				reuseId = itemIdManager.GetReuseId(collectionView, template);
			}

			var cell = collectionView.DequeueReusableCell(reuseId, indexPath)
				as SlimListViewCollectionViewCell;

			cell.IndexPath = indexPath;
			cell.EnsureFormsTemplate(template);
			cell.UpdateFormsBindingContext(item);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			var s = section;
			var addedSections = 0;

			// Global header
			if (TemplateSelector?.HasGlobalHeader ?? false)
			{
				if (section == 0)
					return 1;

				addedSections++;
				s--;
			}

			// Global footer
			if (TemplateSelector?.HasGlobalFooter ?? false)
			{
				addedSections++;

				if (s >= CachedNumberOfSections - addedSections)
				{
					return 1;
				}
			}

			return (nint)Adapter.ItemsForSection((int)s);
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			itemIdManager.ResetTemplates(collectionView);
			sectionHeaderIdManager.ResetTemplates(collectionView);
			sectionFooterIdManager.ResetTemplates(collectionView);

			cachedNumberOfSections = null;
		}
	}


	public class CvDelegate : UICollectionViewDelegateFlowLayout
	{
		internal PositionTemplateSelector TemplateSelector { get; set; }

		public override CGSize GetReferenceSizeForHeader(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			if (section == 0 && (TemplateSelector?.HasGlobalHeader ?? false))
				return new CGSize();

			var footer = collectionView.DataSource.GetViewForSupplementaryElement(collectionView,
				new NSString(CvConsts.ElementKindSectionHeader), NSIndexPath.FromItemSection(0, section))
				as SlimListViewCollectionReusableView;

			if (footer != null)
			{
				var formsSize = footer.FormsView.Measure(collectionView.Frame.Width, double.MaxValue - 1000, MeasureFlags.IncludeMargins);

				return new CGSize(Math.Max(formsSize.Request.Width, collectionView.Frame.Width), formsSize.Request.Height);
			}

			return new CGSize();
		}

		public override CGSize GetReferenceSizeForFooter(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			var s = section;
			var addedSections = 0;

			if (TemplateSelector?.HasGlobalHeader ?? false)
			{
				addedSections++;
				s--;
			}

			// Global footer
			if (TemplateSelector?.HasGlobalFooter ?? false)
			{
				addedSections++;

				if (collectionView.DataSource is CvDataSource ds
					&& s >= (ds.CachedNumberOfSections - addedSections))
					return new CGSize();
			}

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
	}

	public class CvLayout : UICollectionViewFlowLayout
	{
		public CvLayout() : base()
		{
		}

		internal PositionTemplateSelector TemplateSelector { get; set; }

		public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
		{
			var layoutAttributes = base.LayoutAttributesForSupplementaryView(kind, indexPath);

			var x = SectionInset.Left;
			var y = layoutAttributes.Frame.Y;

			nfloat width;

			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
				width = CollectionView.SafeAreaLayoutGuide.LayoutFrame.Width - SectionInset.Left - SectionInset.Right;
			else
				width = CollectionView.Bounds.Width - SectionInset.Left - SectionInset.Right;

			layoutAttributes.Frame = new CGRect(x, y, width, 1); // layoutAttributes.Frame.Height);

			return layoutAttributes;
		}


		public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath path)
		{
			var layoutAttributes = base.LayoutAttributesForItem(path);

			var x = SectionInset.Left;

			nfloat width;

			if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
				width = CollectionView.SafeAreaLayoutGuide.LayoutFrame.Width - SectionInset.Left - SectionInset.Right;
			else
				width = CollectionView.Bounds.Width - SectionInset.Left - SectionInset.Right;

			layoutAttributes.Frame = new CGRect(x, layoutAttributes.Frame.Y, width, layoutAttributes.Frame.Height);

			return layoutAttributes;
		}

		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
		{
			var layoutAttributesObjects = base.LayoutAttributesForElementsInRect(rect);
			var supplementaryAttributeObjects = new List<UICollectionViewLayoutAttributes>();

			foreach (var layoutAttributes in layoutAttributesObjects)
			{
				var indexPath = layoutAttributes.IndexPath;

				if (layoutAttributes.RepresentedElementCategory == UICollectionElementCategory.Cell)
				{
					var newFrame = LayoutAttributesForItem(indexPath).Frame;

					layoutAttributes.Frame = newFrame;
				}

				// If it's 1st item in the section
				// and we use a section header
				// but not if it's the first section and we have a global header already
				if (indexPath.Item == 0 && (TemplateSelector?.HasSectionHeader ?? false)
					&& !(indexPath.Section == 0 && (TemplateSelector?.HasGlobalHeader ?? false)))
				{
					var supLayoutAttributes = LayoutAttributesForSupplementaryView(CvConsts.ElementKindSectionHeader, layoutAttributes.IndexPath);

					supplementaryAttributeObjects.Add(supLayoutAttributes);
				}
			}

			if (supplementaryAttributeObjects.Any())
				return layoutAttributesObjects.Concat(supplementaryAttributeObjects).ToArray();

			return layoutAttributesObjects;
		}
	}

	public class SlimListViewCollectionViewCell : UICollectionViewCell
	{
		public object FormsBindingContext { get; private set; }
		public View FormsView { get; private set; }

		public NSIndexPath IndexPath { get; set; }

		UIContainerView containerView = null;

		[Export("initWithFrame:")]
		public SlimListViewCollectionViewCell(CGRect frame) : base(frame)
		{
		}

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			var attr = base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);

			LayoutIfNeeded();

			FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);

			var formsSize = FormsView.Measure(attr.Frame.Width, double.MaxValue - 100, MeasureFlags.IncludeMargins);

			var w = formsSize.Request.Width;
			var h = formsSize.Request.Height;

			if (IndexPath.Section == 0 && )

			attr.Frame = new CGRect(attr.Frame.X, attr.Frame.Y, attr.Frame.Width, h);
			attr.Size = new CGSize(w, h);

			return attr;
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
				//FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);
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

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			var attr = base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);

			LayoutIfNeeded();

			FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);

			var formsSize = FormsView.Measure(attr.Bounds.Width, double.MaxValue - 100, MeasureFlags.IncludeMargins);

			var h = formsSize.Request.Height;

			attr.Frame = new CGRect(attr.Frame.X, 0, attr.Bounds.Width, h);

			containerView.Frame = attr.Frame;

			return attr;
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
