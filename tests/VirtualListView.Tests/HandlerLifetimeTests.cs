using Microsoft.Maui.Adapters;
using Microsoft.Maui.Dispatching;

namespace VirtualListView.Tests;

public class HandlerLifetimeTests
{
	[Fact]
	public void SubscriptionsPairAcrossReplacementDisconnectAndReconnect()
	{
		var first = new EventAdapter();
		var second = new EventAdapter();
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		var calls = 0;
		publication.Connect(dispatcher, first, () => calls++);
		publication.Connect(dispatcher, first, () => calls++);
		Assert.Equal(1, first.Subscribers);
		var stale = first.Notification;
		first.InvalidateData();
		publication.SetAdapter(second);
		stale.Invoke(first, EventArgs.Empty);
		first.InvalidateData();
		second.InvalidateData();
		dispatcher.Drain();
		Assert.Equal(1, calls);
		Assert.Equal(0, first.Subscribers);
		Assert.Equal(1, second.Subscribers);
		second.InvalidateData();
		publication.Disconnect();
		publication.Disconnect();
		Assert.Equal(0, second.Subscribers);
		publication.Connect(dispatcher, second, () => calls++);
		second.InvalidateData();
		dispatcher.Drain();
		Assert.Equal(2, calls);
		Assert.False(first.Disposed);
		Assert.False(second.Disposed);
	}

	[Fact]
	public async Task WorkerNotificationsAndReentrantPublicationsRunInOrderOnOwningDispatcher()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		var source = new EventAdapter();
		var values = new List<int>();
		publication.Connect(dispatcher, source, () =>
		{
			Assert.True(dispatcher.IsDraining);
			values.Add(1);
			publication.Post(() => values.Add(4));
		});
		await Task.Run(() =>
		{
			source.InvalidateData();
			publication.Post(() => values.Add(2));
			publication.Post(() => values.Add(3));
		});
		Assert.Empty(values);
		Assert.Equal(1, dispatcher.PendingCount);
		dispatcher.Drain();
		Assert.Equal(new[] { 1, 2, 3, 4 }, values);
	}

	[Fact]
	public void OldDispatcherCannotDrainWorkForAReconnectedControl()
	{
		var oldDispatcher = new TestDispatcher();
		var newDispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		var calls = 0;
		publication.Connect(oldDispatcher, null, () => { });
		publication.Post(() => calls += 100);
		publication.Disconnect();
		publication.Connect(newDispatcher, null, () => { });
		publication.Post(() => calls++);
		oldDispatcher.Drain();
		Assert.Equal(0, calls);
		newDispatcher.Drain();
		Assert.Equal(1, calls);
	}

	[Fact]
	public void CapturedPublisherRejectsLatePayloadsFromAnOldSourceOrConnection()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, new EventAdapter(), () => { });
		var old = publication.CapturePublisher();
		var calls = 0;
		old(() => calls += 100);
		publication.SetAdapter(new EventAdapter());
		old(() => calls += 100);
		var current = publication.CapturePublisher();
		current(() => calls++);
		dispatcher.Drain();
		Assert.Equal(1, calls);
		publication.Disconnect();
		publication.Connect(dispatcher, null, () => { });
		current(() => calls += 100);
		publication.Post(() => calls++);
		dispatcher.Drain();
		Assert.Equal(2, calls);
	}

	[Fact]
	public void DispatcherRejectionAndPublicationFailuresAreNotHidden()
	{
		var dispatcher = new TestDispatcher { Accept = false };
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		Assert.Throws<InvalidOperationException>(() => publication.Post(() => { }));
		dispatcher.Accept = true;
		publication.Post(() => throw new ApplicationException("native publication"));
		Assert.Throws<ApplicationException>(() => dispatcher.Drain());
		var calls = 0;
		publication.Post(() => calls++);
		dispatcher.Drain();
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task RefreshCompletionIsMarshalledAndOnlyEndsItsOwnRefreshOnce()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		Action first = null;
		Action second = null;
		var firstEnded = 0;
		var secondEnded = 0;
		refresh.Begin(callback => first = callback, () => firstEnded++);
		refresh.Begin(callback => second = callback, () => secondEnded++);
		Assert.Equal(1, firstEnded);
		await Task.Run(first);
		dispatcher.Drain();
		Assert.Equal(0, secondEnded);
		await Task.Run(() => { second(); second(); });
		Assert.Equal(0, secondEnded);
		dispatcher.Drain();
		Assert.Equal(1, secondEnded);
		refresh.End();
		Assert.Equal(1, secondEnded);
	}

	[Fact]
	public void DisconnectEndsOwnedRefreshAndLateCompletionCannotEndReconnectRefresh()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		Action old = null;
		var ended = 0;
		refresh.Begin(callback => old = callback, () => ended++);
		old();
		publication.Disconnect();
		refresh.End();
		publication.Connect(dispatcher, null, () => { });
		refresh.Begin(_ => { }, () => ended++);
		old();
		dispatcher.Drain();
		Assert.Equal(1, ended);
		refresh.End();
		Assert.Equal(2, ended);
	}

	[Fact]
	public void RefreshFailureEndsSpinnerAndPropagatesOriginalError()
	{
		var publication = new HandlerPublication();
		publication.Connect(new TestDispatcher(), null, () => { });
		var refresh = new OwnedRefresh(publication);
		var ended = 0;
		Assert.Throws<ApplicationException>(() => refresh.Begin(_ => throw new ApplicationException("refresh"), () => ended++));
		refresh.End();
		Assert.Equal(1, ended);
	}

	[Fact]
	public void RefreshWithoutAConsumerCompletesImmediately()
	{
		IVirtualListView view = new TestListView();
		var ended = 0;
		view.Refresh(() => ended++);
		Assert.Equal(1, ended);
	}

	sealed class EventAdapter : IVirtualListViewAdapter, IDisposable
	{
		public EventHandler Notification { get; private set; }
		public int Subscribers { get; private set; }
		public bool Disposed { get; private set; }
		public event EventHandler OnDataInvalidated
		{
			add { Notification += value; Subscribers++; }
			remove { Notification -= value; Subscribers--; }
		}
		public int GetNumberOfSections() => 1;
		public int GetNumberOfItemsInSection(int sectionIndex) => 0;
		public object GetItem(int sectionIndex, int itemIndex) => throw new NotSupportedException();
		public object GetSection(int sectionIndex) => null;
		public void InvalidateData() => Notification?.Invoke(this, EventArgs.Empty);
		public void Dispose() => Disposed = true;
	}

	sealed class TestDispatcher : IDispatcher
	{
		readonly Queue<Action> queue = new();
		public bool Accept { get; set; } = true;
		public bool IsDraining { get; private set; }
		public bool IsDispatchRequired => !IsDraining;
		public int PendingCount => queue.Count;
		public bool Dispatch(Action action)
		{
			if (!Accept)
				return false;
			queue.Enqueue(action);
			return true;
		}
		public void Drain()
		{
			IsDraining = true;
			try
			{
				while (queue.TryDequeue(out var action))
					action();
			}
			finally { IsDraining = false; }
		}
		public bool DispatchDelayed(TimeSpan delay, Action action) => throw new NotSupportedException();
		public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
	}
}
