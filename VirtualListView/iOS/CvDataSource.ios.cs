using System;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvDataSource : UICollectionViewDataSource
	{
		public CvDataSource(VirtualListViewRenderer renderer)
			: base()
		{
			Renderer = renderer;
		}

		VirtualListViewRenderer Renderer { get; }

 		public Func<int, int, bool> IsSelectedHandler { get; set; }

		readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
		readonly ReusableIdManager globalIdManager = new ReusableIdManager("Global");
		readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("SectionHeader"));
		readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("SectionFooter"));


		int? cachedNumberOfSections;
		internal int CachedNumberOfSections
		{
			get
			{
				if (!cachedNumberOfSections.HasValue)
				{
					cachedNumberOfSections = Renderer?.Adapter?.Sections ?? 0;
					if (Renderer?.TemplateSelector?.HasGlobalHeader ?? false)
						cachedNumberOfSections++;
					if (Renderer?.TemplateSelector?.HasGlobalFooter ?? false)
						cachedNumberOfSections++;
				}

				return cachedNumberOfSections.Value;
			}
		}

		public override nint NumberOfSections(UICollectionView collectionView)
			=> CachedNumberOfSections;

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var section = indexPath.Section;
			var itemIndex = (int)indexPath.Item;

			var (template, info) = Renderer.TemplateSelector.GetTemplateAndInfo(Renderer.Adapter, section, itemIndex);

			var reuseId = info.Kind switch
			{
				PositionKind.Item => itemIdManager.GetReuseId(collectionView, template),
				PositionKind.SectionHeader => sectionHeaderIdManager.GetReuseId(collectionView, template),
				PositionKind.SectionFooter => sectionFooterIdManager.GetReuseId(collectionView, template),
				PositionKind.Header => globalIdManager.GetReuseId(collectionView, template),
				PositionKind.Footer => globalIdManager.GetReuseId(collectionView, template),
				_ => itemIdManager.GetReuseId(collectionView, template)
			};

			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = IsSelectedHandler?.Invoke(info.SectionIndex, info.ItemIndex) ?? false;


			var cell = collectionView.DequeueReusableCell(reuseId, indexPath)
				as CvCell;

			cell.IndexPath = indexPath;
			cell.EnsureFormsTemplate(template, info);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			var realSection = section;

			if (Renderer.TemplateSelector.HasGlobalHeader)
			{
				if (section == 0)
					return 1;

				realSection--;
			}

			if (Renderer.TemplateSelector.HasGlobalFooter)
			{
				if (section >= CachedNumberOfSections - 1)
					return 1;
			}

			var itemsCount = Renderer.Adapter.ItemsForSection((int)realSection);

			if (Renderer.TemplateSelector.HasSectionHeader)
				itemsCount++;

			if (Renderer.TemplateSelector.HasSectionFooter)
				itemsCount++;

			return (nint)itemsCount;
		}

		public void ResetTemplates(UICollectionView collectionView)
		{
			itemIdManager.ResetTemplates(collectionView);
			sectionHeaderIdManager.ResetTemplates(collectionView);
			sectionFooterIdManager.ResetTemplates(collectionView);

			cachedNumberOfSections = null;
		}
	}
}