using Microsoft.Maui.Adapters;
using System.ComponentModel;
using Microsoft.Maui.Dispatching;

namespace Microsoft.Maui;

public partial class VirtualListViewHandler : IVirtualListViewHandler
{
	#if ANDROID || IOS || MACCATALYST || WINDOWS
	public static new IPropertyMapper<IVirtualListView, VirtualListViewHandler> ViewMapper = new PropertyMapper<IVirtualListView, VirtualListViewHandler>(Handlers.ViewHandler.ViewMapper)
	{
		[nameof(IVirtualListView.Adapter)] = MapAdapter,
		[nameof(IVirtualListView.Header)] = MapHeader,
		[nameof(IVirtualListView.IsHeaderVisible)] = MapIsHeaderVisible,
		[nameof(IVirtualListView.Footer)] = MapFooter,
		[nameof(IVirtualListView.IsFooterVisible)] = MapIsFooterVisible,
		[nameof(IVirtualListView.ViewSelector)] = MapViewSelector,
		[nameof(IVirtualListView.SelectionMode)] = MapSelectionMode,
		[nameof(IVirtualListView.Orientation)] = MapOrientation,
		[nameof(IVirtualListView.RefreshAccentColor)] = MapRefreshAccentColor,
		[nameof(IVirtualListView.IsRefreshEnabled)] = MapIsRefreshEnabled,
		[nameof(IVirtualListView.EmptyView)] = MapEmptyView,
		[nameof(IVirtualListView.SelectedItems)] = MapSelectedItems,
		[nameof(IVirtualListView.VerticalScrollbarVisibility)] = MapVerticalScrollbarVisibility,
		[nameof(IVirtualListView.HorizontalScrollbarVisibility)] = MapHorizontalScrollbarVisibility,
	};

	public static CommandMapper<IVirtualListView, VirtualListViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(IVirtualListView.ScrollToItem)] = MapScrollToItem,
	};

	public static void MapScrollToItem(VirtualListViewHandler handler, IVirtualListView view, object parameter)
	{
		handler.RecordScrollIntent();
		if (parameter is ItemPosition itemPosition)
		{
			handler.Publish(() => handler.PlatformScrollToItem(itemPosition, true));
		}
		else if (parameter is object[] parameters)
		{
			if (parameters?[0] is ItemPosition p)
			{
				var animated = true;
				if (parameters?[1] is bool a)
					animated = a;

				handler.Publish(() => handler.PlatformScrollToItem(p, animated));
			}
		}
	}

	public VirtualListViewHandler() : base(ViewMapper, CommandMapper)
	{

	}

	internal PositionalViewSelector PositionalViewSelector { get; private set; }
	readonly HandlerPublication publication = new();
	IDispatcher contentDispatcher;
	OwnedRefresh ownedRefresh;
	internal bool IsConnected => publication.IsConnected;

	void ConnectLifetime()
	{
		var dispatcher = VirtualView is Controls.BindableObject bindable
			? bindable.Dispatcher : MauiContext.Services.GetRequiredService<IDispatcher>();
		contentDispatcher = dispatcher;
		acceptedSource = VirtualView.Adapter;
		publication.Connect(dispatcher, VirtualView.Adapter, ApplyDataInvalidation, ApplySnapshot);
		ownedRefresh ??= new OwnedRefresh(publication);
		MapHeader(this, VirtualView);
		MapFooter(this, VirtualView);
	}

	void DisconnectLifetime()
	{
		publication.Disconnect();
		contentDispatcher = null;
		ownedRefresh?.End();
		DisconnectProjectionObservers();
		PositionalViewSelector = null;
		previousSelections = Array.Empty<ItemPosition>();
		selectedPositions.Clear();
		acceptedRows = null;
		acceptedSource = null;
		pendingAnchor = null;
		anchorState.Invalidate();
	}

	internal void Publish(Action action) => publication.Post(action);
	internal Action<Action> CapturePublisher() => publication.CapturePublisher();
	internal bool DispatchContentLayout(Action action)
	{
		var dispatcher = contentDispatcher;
		// A disconnected slot is retired; its pending measurement is no longer needed.
		return dispatcher is null || dispatcher.Dispatch(action);
	}

	// This updates geometry for the currently bound slot, never source/projection state.
	internal void InvalidateContentLayout(PositionInfo position) => Publish(() =>
	{
		if (PositionalViewSelector?.GetInfo(position.Position) is not null)
			PreserveAnchorDuring(() => PlatformInvalidateContentLayout(position));
	});

	public void InvalidateData() => Publish(ApplyDataInvalidation);

	void ApplyDataInvalidation()
	{
		if (VirtualView.Adapter is ISnapshotVirtualListViewAdapter snapshots)
		{
			ApplySnapshot(acceptedRows?.Snapshot ?? snapshots.CaptureSnapshot(), true);
			return;
		}
		acceptedRows = null;
		anchorState.Invalidate();
		PositionalViewSelector?.Accept(new PositionalViewSelector(VirtualView));
		PlatformInvalidateData();
	}

	KeyedRows acceptedRows;
	internal VirtualListViewSnapshot AcceptedSnapshot => acceptedRows?.Snapshot;
	IVirtualListViewAdapter acceptedSource;
	KeyedAnchor pendingAnchor;
	Size? arrangedSize;
	readonly KeyedAnchorState anchorState = new();
	internal void RecordScrollIntent()
	{
		anchorState.Supersede();
		pendingAnchor = null;
	}

	KeyedAnchor CaptureAnchor()
	{
		// Native layout may still show the previous transaction. Keep its keyed
		// baseline across ordered edits/corrections until restoration completes.
		var visible = PlatformVisibleAnchor();
		return anchorState.Capture(acceptedRows, visible.Index, visible.Offset, pendingAnchor);
	}

	void RestoreAnchor(KeyedAnchor anchor)
	{
		if (anchor is null)
			return;
		pendingAnchor = anchor;
		var publish = CapturePublisher();
		var attempts = 0;
		void Restore()
		{
			var index = anchorState.Resolve(anchor, acceptedRows);
			if (index >= 0 && !PlatformRestoreAnchor(index, anchor.Offset))
			{
				if (++attempts >= 3)
					throw new InvalidOperationException("The native layout could not restore the accepted keyed anchor.");
				PlatformAfterLayout(() => publish(Restore));
				return;
			}
			if (ReferenceEquals(pendingAnchor, anchor))
				pendingAnchor = null;
		}
		PlatformAfterLayout(() => publish(Restore));
	}

	internal void PreserveAnchorDuring(Action update)
	{
		var anchor = CaptureAnchor();
		update();
		RestoreAnchor(anchor);
	}

	public override void PlatformArrange(Rect frame)
	{
		var changed = arrangedSize != frame.Size;
		arrangedSize = frame.Size;
		if (IsConnected && changed)
			PreserveAnchorDuring(() => base.PlatformArrange(frame));
		else
			base.PlatformArrange(frame);
	}

	void ApplySnapshot(VirtualListViewSnapshot snapshot, bool reset)
	{
		var before = acceptedRows;
		if (before is not null && !ReferenceEquals(before.Snapshot, snapshot))
			SnapshotValidation.Validate(before.Snapshot, snapshot, reset);
		else if (before is not null && !reset)
			return;
		var nextProjection = new PositionalViewSelector(VirtualView, snapshot);
		var after = new KeyedRows(nextProjection, snapshot);
		var plan = new KeyedUpdatePlan(before, after, reset);
		var anchor = CaptureAnchor();
		var selection = before?.ReconcileSelection(previousSelections, after);
		PositionalViewSelector.Accept(nextProjection);
		acceptedRows = after;
		if (selection is not null)
		{
			previousSelections = selection;
			selectedPositions = selection.ToHashSet();
			if (VirtualView.SelectionMode == SelectionMode.Single)
				VirtualView.SelectedItem = selection.Length == 0 ? null : selection[0];
			else
				VirtualView.SelectedItems = selection;
		}
		PlatformApplyUpdates(plan);
		RestoreAnchor(anchor);
	}

	internal void BindRetained(RetainedView content, PositionInfo info, object data, PresentationKey? key = null)
	{
		info.IsSelected = info.Kind == PositionKind.Item && IsItemSelected(info.SectionIndex, info.ItemIndex);
		content.Bind(info, data);
		content.Key = key ?? (acceptedRows is not null && info.Position >= 0 && info.Position < acceptedRows.Rows.Length
			? acceptedRows.Rows[info.Position].Key : null);
	}

	internal void RebindRetained(RetainedView content, KeyedUpdatePlan plan)
	{
		if (content?.Position is not PositionInfo old)
			return;
		var key = content.Key;
		if (key is null && plan.Before is not null && old.Position >= 0 && old.Position < plan.Before.Rows.Length)
			key = plan.Before.Rows[old.Position].Key;
		if (key is not null && plan.After.Indices.TryGetValue(key.Value, out var index))
		{
			var row = plan.After.Rows[index];
			row.Position.IsSelected = row.Key.Kind == PositionKind.Item
				&& IsItemSelected(row.Position.SectionIndex, row.Position.ItemIndex);
			if (plan.Before is not null && plan.Before.Indices.TryGetValue(key.Value, out var oldIndex)
				&& plan.Before.Rows[oldIndex].Revision == row.Revision
				&& ReferenceEquals(plan.Before.Rows[oldIndex].Data, row.Data)
				&& content.TryUpdatePosition(row.Position, row.Data))
			{
				content.Key = row.Key;
				return;
			}
			BindRetained(content, row.Position, row.Data, row.Key);
		}
		else
			content.Unbind();
	}

	void BeginRefresh(Action started, Action finished)
		=> ownedRefresh.Begin(completion =>
		{
			started();
			VirtualView.Refresh(completion);
		}, finished);

	void EndRefresh() => ownedRefresh?.End();

	bool ShouldShowEmptyView => !(PositionalViewSelector?.HasItems ?? false);

	INotifyPropertyChanged observedHeader;
	INotifyPropertyChanged observedFooter;

	public static void MapHeader(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler.observedHeader is not null)
			handler.observedHeader.PropertyChanged -= handler.GlobalChromePropertyChanged;

		handler.observedHeader = handler.IsConnected ? virtualListView.Header as INotifyPropertyChanged : null;
		if (handler.observedHeader is not null)
			handler.observedHeader.PropertyChanged += handler.GlobalChromePropertyChanged;

		handler.InvalidateData();
	}

	public static void MapFooter(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler.observedFooter is not null)
			handler.observedFooter.PropertyChanged -= handler.GlobalChromePropertyChanged;

		handler.observedFooter = handler.IsConnected ? virtualListView.Footer as INotifyPropertyChanged : null;
		if (handler.observedFooter is not null)
			handler.observedFooter.PropertyChanged += handler.GlobalChromePropertyChanged;

		handler.InvalidateData();
	}

	public static void MapViewSelector(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler.IsConnected)
			handler.PlatformResetTemplates();
		handler.InvalidateData();
	}

	void GlobalChromePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		// Controls exposes IsVisible while IView exposes Visibility.
		if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is "Visibility" or "IsVisible")
			Publish(() =>
			{
				if (ReferenceEquals(sender, observedHeader) || ReferenceEquals(sender, observedFooter))
					ApplyDataInvalidation();
			});
	}

	void DisconnectProjectionObservers()
	{
		if (observedHeader is not null)
			observedHeader.PropertyChanged -= GlobalChromePropertyChanged;
		if (observedFooter is not null)
			observedFooter.PropertyChanged -= GlobalChromePropertyChanged;
		observedHeader = null;
		observedFooter = null;
	}

	public static void MapAdapter(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler.EndRefresh();
		if (!ReferenceEquals(handler.acceptedSource, virtualListView.Adapter))
		{
			if (handler.acceptedRows is not null)
			{
				handler.previousSelections = Array.Empty<ItemPosition>();
				handler.selectedPositions.Clear();
				virtualListView.SelectedItems = Array.Empty<ItemPosition>();
				virtualListView.SelectedItem = null;
			}
			handler.acceptedRows = null;
			handler.acceptedSource = virtualListView.Adapter;
			handler.pendingAnchor = null;
			handler.anchorState.Invalidate();
		}
		if (handler.IsConnected)
			handler.publication.SetAdapter(virtualListView.Adapter);
		handler.InvalidateData();
	}

	public bool IsItemSelected(int sectionIndex, int itemIndex)
	{
		if (VirtualView is null)
			return false;

		if (VirtualView.SelectionMode == SelectionMode.None)
			return false;

		return selectedPositions.Contains(new ItemPosition(sectionIndex, itemIndex));
	}

	ItemPosition[] previousSelections = Array.Empty<ItemPosition>();
	HashSet<ItemPosition> selectedPositions = new();

	public static void MapSelectedItems(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		if (handler is null)
			return;

		handler.Publish(() => handler.ApplySelectedItems(virtualListView));
	}

	void ApplySelectedItems(IVirtualListView virtualListView)
	{
		var newSelections = virtualListView?.SelectedItems ?? Array.Empty<ItemPosition>();

		if (virtualListView.SelectionMode == SelectionMode.None)
			newSelections = Array.Empty<ItemPosition>();
		else if (virtualListView.SelectionMode == SelectionMode.Single && newSelections.Count > 1)
			newSelections = newSelections.Take(1).ToArray();

		// First deselect any previously selected items that aren't in the new set
		var nextSet = newSelections.Where(position =>
			PositionalViewSelector.GetPosition(position.SectionIndex, position.ItemIndex) >= 0).ToHashSet();
		foreach (var itemPosition in previousSelections)
		{
			if (!nextSet.Contains(itemPosition))
				PlatformUpdateItemSelection(itemPosition, false);
		}

		// Set all the new state selected to true
		foreach (var itemPosition in nextSet)
		{
			if (!selectedPositions.Contains(itemPosition))
				PlatformUpdateItemSelection(itemPosition, true);
		}

		// Keep track of the new state for next time it changes
		previousSelections = nextSet.ToArray();
		selectedPositions = nextSet;
	}

	public static void MapIsHeaderVisible(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.InvalidateData();
	}

	public static void MapIsFooterVisible(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.InvalidateData();
	}
	
	public static void MapVerticalScrollbarVisibility(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateVerticalScrollbarVisibility(virtualListView.VerticalScrollbarVisibility);
	}
	
	public static void MapHorizontalScrollbarVisibility(VirtualListViewHandler handler, IVirtualListView virtualListView)
	{
		handler?.UpdateHorizontalScrollbarVisibility(virtualListView.HorizontalScrollbarVisibility);
	}
#endif
}