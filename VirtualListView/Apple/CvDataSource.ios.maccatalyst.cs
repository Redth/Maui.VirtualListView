#nullable enable
using Foundation;
using UIKit;

namespace Microsoft.Maui;

internal class CvDataSource : UICollectionViewDataSource
{
	public CvDataSource(VirtualListViewHandler handler)
		: base()
	{
		Handler = handler;
	}

	VirtualListViewHandler Handler { get; }

	readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
	readonly ReusableIdManager globalIdManager = new ReusableIdManager("Global");
	readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("SectionHeader"));
	readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("SectionFooter"));

	public override nint NumberOfSections(UICollectionView collectionView)
		=> Handler.Adapter.GetNumberOfSections();
	
	public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
	{
		var sectionIndex = indexPath.Section;
		var itemIndex = indexPath.Item.ToInt32();
		var info = Handler.PlatformController.GetInfo(sectionIndex, itemIndex);
		
		object? data = Handler.Adapter.DataFor(PositionKind.Item, sectionIndex, itemIndex);
		var reuseId = Handler.VirtualView.ViewSelector?.GetReuseId(info, data);
		var nativeReuseId = itemIdManager.GetReuseId(collectionView, reuseId);
		
		var nativeCell = collectionView.DequeueReusableCell(nativeReuseId, indexPath);
		if (nativeCell is not CvCell cell)
			return (UICollectionViewCell)nativeCell;
		
		cell.SetTapHandlerCallback(TapCellHandler);
		cell.Handler = Handler;
		cell.IndexPath = new WeakReference<NSIndexPath>(indexPath);

		cell.ReuseCallback = new WeakReference<Action<IView>>((rv) =>
		{
			if (info is not null && (cell.VirtualView?.TryGetTarget(out var cellView) ?? false))
				Handler.VirtualView.ViewSelector?.ViewDetached(info, cellView);
		});

		if (info is not null)
		{
			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = Handler.IsItemSelected(info.SectionIndex, info.ItemIndex);
		
			if (cell.NeedsView)
			{
				var view = Handler.ViewSelector?.CreateView(info, data);
				if (view is not null)
					cell.SetupView(view);
			}
			
			if (cell.NeedsView)
			{
				var view = Handler.VirtualView.ViewSelector?.CreateView(info, data);
				if (view is not null)
					cell.SetupView(view);
			}

			cell.UpdatePosition(info);

			if (cell.VirtualView.TryGetTarget(out var cellVirtualView))
			{
				Handler.VirtualView.ViewSelector?.RecycleView(info, data, cellVirtualView);

				Handler.VirtualView.ViewSelector?.ViewAttached(info, cellVirtualView);
			}
		}

		return cell;
	}

	// public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind,
	// 	NSIndexPath indexPath)
	// {
	// 	return base.GetViewForSupplementaryElement(collectionView, elementKind, indexPath);
	// }

	void TapCellHandler(CvCell cell)
	{
		var p = new ItemPosition(cell.PositionInfo.SectionIndex, cell.PositionInfo.ItemIndex);

		cell.PositionInfo.IsSelected = !cell.PositionInfo.IsSelected;

		if (cell.PositionInfo.IsSelected)
			Handler.VirtualView.SelectItem(p);
		else
			Handler.VirtualView.DeselectItem(p);
	}

	public override nint GetItemsCount(UICollectionView collectionView, nint section)
		=> Handler.Adapter.GetNumberOfItemsInSection(section.ToInt32());
}