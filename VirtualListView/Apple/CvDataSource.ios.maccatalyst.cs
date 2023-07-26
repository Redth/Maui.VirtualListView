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

	public Func<int, int, bool> IsSelectedHandler { get; set; }

	readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
	readonly ReusableIdManager globalIdManager = new ReusableIdManager("Global");
	readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("SectionHeader"));
	readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("SectionFooter"));

	public override nint NumberOfSections(UICollectionView collectionView)
		=> Handler?.PositionalViewSelector?.GetNumberOfSections() ?? 0;

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
			Handler.VirtualView?.SelectItems(p);
		else
			Handler.VirtualView?.DeselectItems(p);
	}

	public override nint GetItemsCount(UICollectionView collectionView, nint section)
	{
		return Handler?.PositionalViewSelector?.GetNumberOfItemsForSection(section.ToInt32()) ?? 0;
	}
}