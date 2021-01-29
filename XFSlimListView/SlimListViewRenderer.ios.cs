using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
					cachedNumberOfSections = Adapter.Sections;

				return cachedNumberOfSections.Value;
			}
		}

		public override nint NumberOfSections(UICollectionView collectionView)
			=> CachedNumberOfSections;

		public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
		{
			DataTemplate template = null;
			DataTemplate globalTemplate = null;
			PositionKind kind = PositionKind.Item;

			NSString reuseId = null;

			if (elementKind == CvConsts.ElementKindSectionHeader)
			{
				kind = PositionKind.SectionHeader;

				if (indexPath.Section == 0 && (TemplateSelector?.HasGlobalHeader ?? false))
				{
					globalTemplate = TemplateSelector.HeaderTemplate;
					kind = PositionKind.Header;
				}
				
				template = TemplateSelector?.SectionHeaderTemplateSelector?.SelectGroupTemplate(Adapter, indexPath.Section)
					?? TemplateSelector.SectionHeaderTemplate;

				reuseId = sectionHeaderIdManager.GetReuseId(collectionView, template);
			}
			else if (elementKind == CvConsts.ElementKindSectionFooter)
			{
				kind = PositionKind.SectionFooter;

				if (indexPath.Section >= CachedNumberOfSections - 1 && (TemplateSelector?.HasGlobalFooter ?? false))
				{
					globalTemplate = TemplateSelector.FooterTemplate;
					kind = PositionKind.Footer;
				}
				
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

			supplementary.EnsureFormsTemplate(template, globalTemplate, kind);

			var bindingContext = Adapter.Section(indexPath.Section);
			supplementary.UpdateFormsBindingContext(bindingContext);

			return supplementary;
		}


		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var section = indexPath.Section;
			var itemIndex = (int)indexPath.Item;

			var template = TemplateSelector?.ItemTemplateSelector?.SelectItemTemplate(Adapter, section, itemIndex)
					?? TemplateSelector.ItemTemplate;
			var item = Adapter.Item(section, itemIndex);
			var reuseId = itemIdManager.GetReuseId(collectionView, template);

			var positionInfo = new PositionInfo
			{
				Kind = PositionKind.Item,
				NumberOfSections = Adapter.Sections,
				ItemIndex = itemIndex,
				SectionIndex = section,
				ItemsInSection = Adapter.ItemsForSection(section)
			};

			var cell = collectionView.DequeueReusableCell(reuseId, indexPath)
				as SlimListViewCollectionViewCell;

			cell.IndexPath = indexPath;
			cell.EnsureFormsTemplate(template, positionInfo);
			cell.UpdateFormsBindingContext(item);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			return (nint)Adapter.ItemsForSection((int)section);
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

		CGSize GetSizeForSupplementary(SlimListViewCollectionReusableView view, nfloat collectionViewWidth)
		{
			if (view != null)
			{
				var formsSize = view.FormsView.Measure(collectionViewWidth, double.MaxValue - 1000, MeasureFlags.IncludeMargins);

				var height = formsSize.Request.Height;
				var width = (nfloat)Math.Max(collectionViewWidth, formsSize.Request.Width);

				if (view.FormsViewGlobal != null)
				{
					var formsGlobalSize = view.FormsViewGlobal.Measure(collectionViewWidth, double.MaxValue - 1000, MeasureFlags.IncludeMargins);
					height += formsGlobalSize.Request.Height;
					width = (nfloat)Math.Max(width, formsGlobalSize.Request.Width);
				}

				return new CGSize(width, height);
			}

			return new CGSize();
		}

		public override CGSize GetReferenceSizeForHeader(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			var header = collectionView.DataSource.GetViewForSupplementaryElement(collectionView,
				new NSString(CvConsts.ElementKindSectionHeader), NSIndexPath.FromItemSection(0, section))
				as SlimListViewCollectionReusableView;

			return GetSizeForSupplementary(header, collectionView.Frame.Width);
		}

		public override CGSize GetReferenceSizeForFooter(UICollectionView collectionView, UICollectionViewLayout layout, nint section)
		{
			var footer = collectionView.DataSource.GetViewForSupplementaryElement(collectionView,
				new NSString(CvConsts.ElementKindSectionHeader), NSIndexPath.FromItemSection(0, section))
				as SlimListViewCollectionReusableView;

			return GetSizeForSupplementary(footer, collectionView.Frame.Width);
		}
	}

	public class CvLayout : UICollectionViewFlowLayout
	{
		public CvLayout() : base()
		{
		}

		internal PositionTemplateSelector TemplateSelector { get; set; }

		// This is if we enable the code that's also commented out a bit further down
		// in the LayoutAttributesForElementsInRect method, see that for more info
		//
		//public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
		//{
		//	var layoutAttributes = base.LayoutAttributesForSupplementaryView(kind, indexPath);

		//	var x = SectionInset.Left;
		//	var y = layoutAttributes.Frame.Y;

		//	nfloat width;

		//	if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
		//		width = CollectionView.SafeAreaLayoutGuide.LayoutFrame.Width - SectionInset.Left - SectionInset.Right;
		//	else
		//		width = CollectionView.Bounds.Width - SectionInset.Left - SectionInset.Right;

		//	layoutAttributes.Frame = new CGRect(x, y, width, 1); // layoutAttributes.Frame.Height);

		//	return layoutAttributes;
		//}


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

				// If we specify more attributes for supplementary views here
				// We will get a call to requested our preferred attributes in our supplementary
				// view implementation, however the weird thing is when we do that,
				// we have to manage arranging that view inside the same bounds as
				// the cell that it is supplementary for, which also means the cell
				// would need to know if it should layout differently to save room for this
				// For now we are just implementing GetReferenceSizeForFooter and GetReferenceSizeForHeader
				// which, while unfortunately calculates (measures) the size for all sections at once,
				// it does handle laying it out for us properly
				// In the future it might be good to switch back to this for perf

				//if (indexPath.Item == 0 && (TemplateSelector?.HasSectionHeader ?? false)
				//	&& !(indexPath.Section == 0 && (TemplateSelector?.HasGlobalHeader ?? false)))
				//{
				//	var supLayoutAttributes = LayoutAttributesForSupplementaryView(CvConsts.ElementKindSectionHeader, layoutAttributes.IndexPath);

				//	supplementaryAttributeObjects.Add(supLayoutAttributes);
				//}
			}

			//if (supplementaryAttributeObjects.Any())
			//	return layoutAttributesObjects.Concat(supplementaryAttributeObjects).ToArray();

			return layoutAttributesObjects;
		}
	}

	public class SlimListViewCollectionViewCell : UICollectionViewCell
	{
		public object FormsBindingContext { get; private set; }
		public View FormsView { get; private set; }

		public NSIndexPath IndexPath { get; set; }

		public PositionData Position { get; set; }

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

			attr.Frame = new CGRect(attr.Frame.X, attr.Frame.Y, attr.Frame.Width, h);
			attr.Size = new CGSize(w, h);

			return attr;
		}

		public void EnsureFormsTemplate(DataTemplate template, PositionInfo positionInfo)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as View;

			if (Position == null)
			{
				foreach (var kvp in FormsView.Resources)
				{
					if (kvp.Value is PositionData pc)
					{
						Position = pc;
						break;
					}
				}
			}

			if (Position != null)
				Position.Update(positionInfo);

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

		public View FormsViewGlobal { get; private set; }

		UIContainerView containerView = null;

		UIContainerView containerViewGlobal = null;

		PositionKind Kind { get; set; } = PositionKind.SectionHeader;

		[Export("initWithFrame:")]
		public SlimListViewCollectionReusableView(CGRect frame) : base(frame)
		{
		}

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			var attr = base.PreferredLayoutAttributesFittingAttributes(layoutAttributes);

			FormsView.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);

			var formsSize = FormsView.Measure(attr.Bounds.Width, double.MaxValue - 100, MeasureFlags.IncludeMargins);

			var formsViewHeight = formsSize.Request.Height;
			var formsViewGlobalHeight = 0d;

			var globalFooter = Kind == PositionKind.Footer;

			if (FormsViewGlobal != null)
			{
				FormsViewGlobal.InvalidateMeasureNonVirtual(Xamarin.Forms.Internals.InvalidationTrigger.MeasureChanged);

				var formsSizeGlobal = FormsViewGlobal.Measure(attr.Bounds.Width, double.MaxValue - 100, MeasureFlags.IncludeMargins);

				formsViewGlobalHeight = formsSizeGlobal.Request.Height;
			}

			attr.Frame = new CGRect(attr.Frame.X, 0, attr.Bounds.Width, formsViewHeight + formsViewGlobalHeight);

			if (containerViewGlobal != null)
				containerViewGlobal.Frame = new CGRect(attr.Frame.X, globalFooter ? formsViewGlobalHeight : 0, attr.Bounds.Width, formsViewGlobalHeight);

			containerView.Frame = new CGRect(attr.Frame.X, globalFooter ? 0 : formsViewGlobalHeight, attr.Bounds.Width, formsViewHeight);

			return attr;
		}

		public void EnsureFormsTemplate(DataTemplate template, DataTemplate globalTemplate, PositionKind kind)
		{
			if (FormsView == null)
				FormsView = template.CreateContent() as View;

			if (globalTemplate != null)
			{
				if (FormsViewGlobal == null && globalTemplate != null)
					FormsViewGlobal = globalTemplate.CreateContent() as View;

				if (containerViewGlobal == null)
				{
					containerViewGlobal = new UIContainerView(FormsViewGlobal)
					{
						Frame = this.Frame,
						//AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
					};

					AddSubview(containerViewGlobal);
				}
			}

			if (containerView == null)
			{
				containerView = new UIContainerView(FormsView)
				{
					Frame = this.Frame,
					//AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
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

		NSString GetReuseId(int i, string idModifier = null)
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
