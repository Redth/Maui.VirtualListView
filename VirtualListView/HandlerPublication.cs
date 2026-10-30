#nullable enable
using Microsoft.Maui.Adapters;
using Microsoft.Maui.Dispatching;

namespace Microsoft.Maui;

// Serializes native-bound work, not mutations of an adapter's backing collection.
internal sealed class HandlerPublication
{
	readonly object sync = new();
	readonly Queue<(long Generation, Action Action)> pending = new();
	IDispatcher? dispatcher;
	IVirtualListViewAdapter? adapter;
	EventHandler? invalidated;
	Action? invalidate;
	Action<VirtualListViewSnapshot, bool>? acceptSnapshot;
	Action<ItemsInvalidatedEventArgs>? refreshItems;
	long generation;
	long scheduledDrain;
	long nextDrain;

	public bool IsConnected { get { lock (sync) return dispatcher is not null; } }

	public void Connect(IDispatcher owningDispatcher, IVirtualListViewAdapter? source, Action onInvalidated,
		Action<VirtualListViewSnapshot, bool>? onSnapshot = null, Action<ItemsInvalidatedEventArgs>? onItems = null)
	{
		if (IsConnected)
			return;

		lock (sync)
		{
			dispatcher = owningDispatcher;
			invalidate = onInvalidated;
			acceptSnapshot = onSnapshot;
			refreshItems = onItems;
			generation++;
		}
		SetAdapter(source);
	}

	public void SetAdapter(IVirtualListViewAdapter? source)
	{
		if (!IsConnected || ReferenceEquals(adapter, source))
			return;

		if (adapter is not null && invalidated is not null)
			adapter.OnDataInvalidated -= invalidated;

		long epoch;
		lock (sync)
		{
			epoch = ++generation;
			pending.Clear();
		}
		adapter = source;
		var snapshotAccepted = false;
		invalidated = (_, args) =>
		{
			lock (sync)
				if (dispatcher is null || epoch != generation)
					return;
			if (args is ItemsInvalidatedEventArgs items)
			{
				if (source is ISnapshotVirtualListViewAdapter)
					throw new InvalidOperationException("Snapshot sources must submit item content revisions, not positional refreshes.");
				Post(epoch, () =>
				{
					if (refreshItems is null)
						throw new InvalidOperationException("No item refresh consumer is connected.");
					refreshItems(items);
				});
			}
			else if (source is ISnapshotVirtualListViewAdapter snapshots && acceptSnapshot is not null)
			{
				var snapshot = (args as SnapshotInvalidatedEventArgs)?.Snapshot ?? snapshots.CaptureSnapshot();
				var reset = (args as SnapshotInvalidatedEventArgs)?.Reset ?? true;
				Post(epoch, () =>
				{
					snapshotAccepted = true;
					acceptSnapshot?.Invoke(snapshot, reset);
				});
			}
			else
				Post(epoch, () => invalidate?.Invoke());
		};
		if (IsConnected && adapter is not null)
		{
			adapter.OnDataInvalidated += invalidated;
			if (adapter is ISnapshotVirtualListViewAdapter snapshots && acceptSnapshot is not null)
			{
				var snapshot = snapshots.CaptureSnapshot();
				Post(epoch, () =>
				{
					// A subscription-time event already carries a complete snapshot.
					// Never apply an older initial capture after that ordered event.
					if (!snapshotAccepted)
					{
						snapshotAccepted = true;
						acceptSnapshot?.Invoke(snapshot, true);
					}
				});
			}
		}
	}

	public void Disconnect()
	{
		lock (sync)
		{
			generation++;
			dispatcher = null;
			invalidate = null;
			acceptSnapshot = null;
			refreshItems = null;
			pending.Clear();
			// A previously scheduled drain can still run, but cannot own the new queue.
			scheduledDrain = 0;
		}
		if (adapter is not null && invalidated is not null)
			adapter.OnDataInvalidated -= invalidated;
		adapter = null;
		invalidated = null;
	}

	public void Post(Action action)
	{
		long epoch;
		lock (sync)
			epoch = generation;
		Post(epoch, action);
	}

	public Action<Action> CapturePublisher()
	{
		long epoch;
		lock (sync)
			epoch = generation;
		return action => Post(epoch, action);
	}

	void Post(long epoch, Action action)
	{
		IDispatcher target;
		long ticket;
		lock (sync)
		{
			if (dispatcher is null || epoch != generation)
				return;
			pending.Enqueue((epoch, action));
			if (scheduledDrain != 0)
				return;
			ticket = scheduledDrain = ++nextDrain;
			target = dispatcher;
		}

		if (!target.IsDispatchRequired)
			Drain(target, ticket);
		else if (!target.Dispatch(() => Drain(target, ticket)))
		{
			lock (sync)
			{
				if (scheduledDrain != ticket || !ReferenceEquals(dispatcher, target))
					return;
				pending.Clear();
				scheduledDrain = 0;
			}
			throw new InvalidOperationException("The owning VirtualListView dispatcher rejected publication.");
		}
	}

	void Drain(IDispatcher target, long ticket)
	{
		try
		{
			while (true)
			{
				Action action;
				lock (sync)
				{
					if (!ReferenceEquals(dispatcher, target) || scheduledDrain != ticket)
						return;
					if (!pending.TryDequeue(out var work))
					{
						scheduledDrain = 0;
						return;
					}
					if (work.Generation != generation)
						continue;
					action = work.Action;
				}
				action();
			}
		}
		catch
		{
			lock (sync)
			{
				if (scheduledDrain == ticket)
				{
					pending.Clear();
					scheduledDrain = 0;
				}
			}
			throw;
		}
	}
}

internal sealed class OwnedRefresh
{
	readonly HandlerPublication publication;
	Action? finish;
	long generation;

	public OwnedRefresh(HandlerPublication publication) => this.publication = publication;

	public void Request(Func<bool> isEnabled, Action request)
		=> publication.Post(() =>
		{
			if (finish is null && isEnabled())
				request();
		});

	public void Begin(Action<Action> refresh, Action onFinished)
	{
		End();
		var epoch = ++generation;
		finish = onFinished;
		var publish = publication.CapturePublisher();
		try
		{
			refresh(() => publish(() =>
			{
				if (epoch == generation)
					End();
			}));
		}
		catch
		{
			End();
			throw;
		}
	}

	public void End()
	{
		generation++;
		var completion = finish;
		finish = null;
		completion?.Invoke();
	}
}
