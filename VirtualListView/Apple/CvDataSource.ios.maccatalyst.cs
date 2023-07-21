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

			var info = Handler?.PositionalViewSelector?.GetInfo((int)indexPath.Section, (int)indexPath.Item);

			var data = Handler?.PositionalViewSelector?.Adapter?.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);

			var reuseId = Handler?.PositionalViewSelector?.ViewSelector?.GetReuseId(info, data);

			var nativeReuseId = info.Kind switch
			{
				PositionKind.Item => itemIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionHeader => sectionHeaderIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionFooter => sectionFooterIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Header => globalIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Footer => globalIdManager.GetReuseId(collectionView, reuseId),
				_ => "UNKNOWN",
			};

			var cell = collectionView.DequeueReusableCell(nativeReuseId, indexPath) as CvCell;
			cell.TapHandler = TapCellHandler;
			cell.Handler = Handler;
			cell.IndexPath = indexPath;
			
			cell.ReuseCallback = rv =>
				Handler.VirtualView.ViewSelector.ViewDetached(info, cell.VirtualView);

			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = IsSelectedHandler?.Invoke(info.SectionIndex, info.ItemIndex) ?? false;

			if (cell.NeedsView)
			{
				var view = Handler?.PositionalViewSelector?.ViewSelector?.CreateView(info, data);
				cell.SwapView(view);
			}

			cell.PositionInfo = info;

			if (cell.VirtualView is IPositionInfo viewPositionInfo)
				viewPositionInfo.IsSelected = info.IsSelected;

			Handler?.PositionalViewSelector?.ViewSelector?.RecycleView(info, data, cell.VirtualView);

			Handler.VirtualView.ViewSelector.ViewAttached(info, cell.VirtualView);

			return cell;
		}

		void TapCellHandler(CvCell cell)
		{
			var p = new ItemPosition(cell.PositionInfo.SectionIndex, cell.PositionInfo.ItemIndex);

			cell.PositionInfo.IsSelected = !cell.PositionInfo.IsSelected;

			if (cell.PositionInfo.IsSelected)
				Handler.VirtualView?.SetSelected(p);
			else
				Handler.VirtualView?.SetDeselected(p);
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

			Handler?.PositionalViewSelector?.Reset();
		}
	}
}