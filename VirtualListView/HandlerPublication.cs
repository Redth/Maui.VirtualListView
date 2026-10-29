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
	long generation;
	long scheduledDrain;
	long nextDrain;

	public bool IsConnected { get { lock (sync) return dispatcher is not null; } }

	public void Connect(IDispatcher owningDispatcher, IVirtualListViewAdapter? source, Action onInvalidated)
	{
		if (IsConnected)
			return;

		lock (sync)
		{
			dispatcher = owningDispatcher;
			invalidate = onInvalidated;
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
		invalidated = (_, _) => Post(epoch, () => invalidate?.Invoke());
		if (IsConnected && adapter is not null)
			adapter.OnDataInvalidated += invalidated;
	}

	public void Disconnect()
	{
		lock (sync)
		{
			generation++;
			dispatcher = null;
			invalidate = null;
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
