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
	readonly NativeViewRegistry<CvCell> cells = new();

	readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
	readonly ReusableIdManager globalIdManager = new ReusableIdManager("Global");
	readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("SectionHeader"));
	readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("SectionFooter"));

	public override nint NumberOfSections(UICollectionView collectionView)
		=> 1;
	
	public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
	{
		var info = Handler?.PositionalViewSelector?.GetInfo(indexPath.Item.ToInt32());

		object? data = null;

		var nativeReuseId = CvCell.ReuseIdUnknown;
		
		if (info is not null)
		{
			data = Handler?.PositionalViewSelector?.Adapter?.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);
			
			var reuseId = info.Kind is PositionKind.Header or PositionKind.Footer
				? info.Kind.ToString() : Handler?.PositionalViewSelector?.ViewSelector?.GetReuseId(info, data);

			nativeReuseId = info.Kind switch
			{
				PositionKind.Item => itemIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionHeader => sectionHeaderIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionFooter => sectionFooterIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Header => globalIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Footer => globalIdManager.GetReuseId(collectionView, reuseId),
				_ => CvCell.ReuseIdUnknown,
			};
		}

		var nativeCell = collectionView.DequeueReusableCell(nativeReuseId, indexPath);
		if (nativeCell is not CvCell cell)
			return (UICollectionViewCell)nativeCell;
		
		if (info is not null)
		{
			var track = cell.Content is null;
			cell.SetTapHandlerCallback(TapCellHandler);
			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = Handler?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false;
		
			try { cell.Bind(Handler, info, data); }
			catch
			{
				cell.Release();
				throw;
			}
			if (track && cell.Content is RetainedView content)
				cells.Add(cell, content);
		}
		else
			cell.Release();

		return cell;
	}

	void TapCellHandler(CvCell cell)
	{
		var p = new ItemPosition(cell.PositionInfo.SectionIndex, cell.PositionInfo.ItemIndex);

		cell.PositionInfo.IsSelected = !cell.PositionInfo.IsSelected;

		if (cell.PositionInfo.IsSelected)
			Handler?.VirtualView?.SelectItem(p);
		else
			Handler?.VirtualView?.DeselectItem(p);
	}

	public override nint GetItemsCount(UICollectionView collectionView, nint section)
	{
		return Handler?.PositionalViewSelector?.TotalCount ?? 0;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			cells.Drain(cell => cell.Release());
		base.Dispose(disposing);
	}

	internal void ResetTemplates() => cells.ForEach(cell => cell.Content?.Release());
	internal void PruneCells() => cells.ForEach(_ => { });
}