using System;
using Foundation;
using UIKit;

namespace Microsoft.Maui
{
	internal class CvDataSource : UICollectionViewDataSource
	{
		public CvDataSource(VirtualListViewHandler handler)
			: base()
		{
			Handler = handler;
		}

		VirtualListViewHandler Handler { get; }

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
					var n = Handler?.PositionalViewSelector?.Adapter?.Sections ?? -1;
					if (n >= 0)
						cachedNumberOfSections = n;
				}

				return cachedNumberOfSections ?? 0;
			}
		}

		public override nint NumberOfSections(UICollectionView collectionView)
			=> CachedNumberOfSections;

		public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
		{
			var section = indexPath.Section;
			var itemIndex = (int)indexPath.Item;

			//var (template, info) = Renderer.TemplateSelector.GetTemplateAndInfo(Renderer.Adapter, section, itemIndex);

			var info = Handler?.PositionalViewSelector?.GetInfo((int)indexPath.Section, (int)indexPath.Item);

			var reuseId = Handler?.PositionalViewSelector?.ViewSelector?.GetReuseId(info.Kind, info.SectionIndex, info.ItemIndex);

			var nativeReuseId = info.Kind switch
			{
				PositionKind.Item => itemIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionHeader => sectionHeaderIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionFooter => sectionFooterIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Header => globalIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Footer => globalIdManager.GetReuseId(collectionView, reuseId),
				_ => "UNKNOWN",
			};

			var view = Handler?.PositionalViewSelector?.ViewSelector?.ViewFor(info.Kind, info.SectionIndex, info.ItemIndex);

			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = IsSelectedHandler?.Invoke(info.SectionIndex, info.ItemIndex) ?? false;

			if (view is IPositionInfo positionInfoView)
				positionInfoView.SetPositionInfo(info);

			var cell = collectionView.DequeueReusableCell(nativeReuseId, indexPath) as CvCell;
			cell.IndexPath = indexPath;
			cell.Update(Handler.MauiContext, view, info);

			return cell;
		}

		public override nint GetItemsCount(UICollectionView collectionView, nint section)
		{
			var realSection = section;

			if (Handler?.PositionalViewSelector?.HasGlobalHeader ?? false)
			{
				if (section == 0)
					return 1;

				realSection--;
			}

			if (Handler?.PositionalViewSelector?.HasGlobalFooter ?? false)
			{
				if (section >= CachedNumberOfSections - 1)
					return 1;
			}

			var itemsCount = Handler?.PositionalViewSelector?.Adapter?.ItemsForSection((int)realSection) ?? 0;

			if (Handler?.PositionalViewSelector?.ViewSelector?.SectionHasHeader((int)realSection) ?? false)
				itemsCount++;

			if (Handler?.PositionalViewSelector?.ViewSelector?.SectionHasFooter((int)realSection) ?? false)
				itemsCount++;

			return (nint)itemsCount;
		}

		public void Reset(UICollectionView collectionView)
		{
			itemIdManager.ResetTemplates(collectionView);
			sectionHeaderIdManager.ResetTemplates(collectionView);
			sectionFooterIdManager.ResetTemplates(collectionView);

			cachedNumberOfSections = null;
		}
	}
}