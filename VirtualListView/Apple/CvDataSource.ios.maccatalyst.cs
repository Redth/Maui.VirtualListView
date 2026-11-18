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
		State = new CvDataSourceState(handler.PositionalViewSelector);
	}

	VirtualListViewHandler Handler { get; }
	internal CvDataSourceState State { get; }
	readonly NativeViewRegistry<CvCell> cells = new();

	readonly ReusableIdManager itemIdManager = new ReusableIdManager("Item");
	readonly ReusableIdManager globalIdManager = new ReusableIdManager("Global");
	readonly ReusableIdManager sectionHeaderIdManager = new ReusableIdManager("SectionHeader", new NSString("SectionHeader"));
	readonly ReusableIdManager sectionFooterIdManager = new ReusableIdManager("SectionFooter", new NSString("SectionFooter"));

	public override nint NumberOfSections(UICollectionView collectionView)
		=> 1;
	
	public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
	{
		var info = State.Projection.GetInfo(indexPath.Item.ToInt32());

		object? data = null;

		var nativeReuseId = CvCell.ReuseIdUnknown;
		
		if (info is not null)
		{
			data = State.Projection.Adapter?.DataFor(info.Kind, info.SectionIndex, info.ItemIndex);
			
			var reuseId = info.Kind is PositionKind.Header or PositionKind.Footer or PositionKind.EmptyView
				? info.Kind.ToString() : State.Projection.ViewSelector?.GetReuseId(info, data);

			nativeReuseId = info.Kind switch
			{
				PositionKind.Item => itemIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionHeader => sectionHeaderIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.SectionFooter => sectionFooterIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Header => globalIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.Footer => globalIdManager.GetReuseId(collectionView, reuseId),
				PositionKind.EmptyView => globalIdManager.GetReuseId(collectionView, reuseId),
				_ => CvCell.ReuseIdUnknown,
			};
		}

		var nativeCell = collectionView.DequeueReusableCell(nativeReuseId, indexPath);
		if (nativeCell is not CvCell cell)
			return (UICollectionViewCell)nativeCell;
		
		if (info is not null)
		{
			if (info.Kind == PositionKind.Item && data is not null)
				ReclaimContent(cell, data);
			var track = cell.Content is null;
			cell.SetTapHandlerCallback(TapCellHandler);
			if (info.SectionIndex < 0 || info.ItemIndex < 0)
				info.IsSelected = false;
			else
				info.IsSelected = Handler?.IsItemSelected(info.SectionIndex, info.ItemIndex) ?? false;
		
			try { cell.Bind(Handler, info, data, State.Rows?.Rows[info.Position].Key); }
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

	// Set the AppContext switch to turn content affinity off.
	internal const string DisableContentAffinitySwitch = "Microsoft.Maui.VirtualListView.DisableContentAffinity";
	static readonly bool contentAffinity =
		!(AppContext.TryGetSwitch(DisableContentAffinitySwitch, out var disabled) && disabled);

	// Content affinity. After a reload or a size change UIKit sends every visible cell through the reuse
	// pool and hands them back in no particular order, so each row would be rebuilt for an item another
	// cell was showing a moment ago. If a cell waiting in the pool still shows the requested item, the
	// dequeued cell takes that content instead (and the pooled cell gets the dequeued cell's), and the
	// bind finds its item already in place.
	void ReclaimContent(CvCell cell, object data)
	{
		if (!contentAffinity || cell.Content?.Holds(data) == true)
			return;

		CvCell? donor = null;
		var reuseId = cell.ReuseIdentifier?.ToString();
		var collectionView = Handler.PlatformView;
		cells.ForEach(candidate =>
		{
			// A donor must be in UIKit's reuse pool: display ended, no index path, and hidden (UIKit hides
			// the cells it pools and un-hides them when it dequeues them, prefetching included).
			if (donor is null && !ReferenceEquals(candidate, cell)
				&& candidate.Content?.IsUndisplayedHolding(data) == true
				&& candidate.Hidden
				&& collectionView.IndexPathForCell(candidate) is null
				&& candidate.ReuseIdentifier?.ToString() == reuseId)
				donor = candidate;
		});
		if (donor is null)
			return;

		CvCell.ExchangeContent(cell, donor);
		cells.Exchange(cell, donor);
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
		return State.Count;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			cells.Drain(cell => cell.Release());
		base.Dispose(disposing);
	}

	internal void ResetTemplates() => cells.ForEach(cell => cell.Content?.Release());
	internal void PruneCells() => cells.ForEach(_ => { });
	internal void Rebind(KeyedUpdatePlan plan) => cells.ForEach(cell => Handler.RebindRetained(cell.Content, plan));
}