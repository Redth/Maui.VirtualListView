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

	[Fact]
	public void PublicRefreshRequestForwardsToTheHandlerWithoutDirectlyInvokingConsumers()
	{
		var view = new TestListView { IsRefreshEnabled = true };
		var calls = 0;
		view.OnRefresh += (_, _) => calls++;
		view.RequestRefresh();
		var handler = new RequestHandler(view);
		view.Handler = handler;
		var before = handler.Invocations;
		view.RequestRefresh();
		Assert.Equal(nameof(view.RequestRefresh), handler.Command);
		Assert.Null(handler.Arguments);
		Assert.Equal(before + 1, handler.Invocations);
		Assert.Equal(0, calls);
	}

	[Fact]
	public void DisabledAndDisconnectedRefreshRequestsAreNotReplayed()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		var refresh = new OwnedRefresh(publication);
		var requests = 0;
		var enabled = true;
		refresh.Request(() => enabled, () => requests++);
		publication.Connect(dispatcher, null, () => { });
		dispatcher.Drain();
		Assert.Equal(0, requests);
		refresh.Request(() => enabled, () => requests++);
		enabled = false;
		dispatcher.Drain();
		Assert.Equal(0, requests);
		enabled = true;
		dispatcher.Drain();
		Assert.Equal(0, requests);
		refresh.Request(() => enabled, () => requests++);
		dispatcher.Drain();
		Assert.Equal(1, requests);
	}

	[Fact]
	public void RepeatedAndReentrantRequestsDoNotRestartOrQueueAnActiveRefresh()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		var starts = 0;
		var finishes = 0;
		Action complete = null;
		void Start() => refresh.Begin(callback =>
		{
			starts++;
			complete = callback;
			refresh.Request(() => true, Start);
		}, () => finishes++);
		refresh.Request(() => true, Start);
		refresh.Request(() => true, Start);
		dispatcher.Drain();
		Assert.Equal(1, starts);
		refresh.Request(() => true, Start);
		complete();
		dispatcher.Drain();
		Assert.Equal(1, starts);
		Assert.Equal(1, finishes);
		refresh.Request(() => true, Start);
		dispatcher.Drain();
		Assert.Equal(2, starts);
		complete();
		dispatcher.Drain();
		Assert.Equal(2, finishes);
	}

	[Fact]
	public void ProgrammaticRequestsDuringAGestureRefreshAreIgnored()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		Action complete = null;
		refresh.Begin(callback => complete = callback, () => { });
		var requests = 0;
		refresh.Request(() => true, () => requests++);
		dispatcher.Drain();
		Assert.Equal(0, requests);
		complete();
		dispatcher.Drain();
		refresh.Request(() => true, () => requests++);
		dispatcher.Drain();
		Assert.Equal(1, requests);
	}

	[Fact]
	public void QueuedRequestsCannotOutliveTheirSourceOrConnectionAndInvalidationDoesNotStartRefresh()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		var source = new EventAdapter();
		var invalidations = 0;
		publication.Connect(dispatcher, source, () => invalidations++);
		var refresh = new OwnedRefresh(publication);
		var requests = 0;
		source.InvalidateData();
		dispatcher.Drain();
		Assert.Equal(1, invalidations);
		Assert.Equal(0, requests);
		refresh.Request(() => true, () => requests++);
		publication.SetAdapter(new EventAdapter());
		dispatcher.Drain();
		Assert.Equal(0, requests);
		refresh.Request(() => true, () => requests++);
		publication.Disconnect();
		refresh.End();
		publication.Connect(dispatcher, source, () => invalidations++);
		dispatcher.Drain();
		Assert.Equal(0, requests);
		refresh.Request(() => true, () => requests++);
		dispatcher.Drain();
		Assert.Equal(1, requests);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public void RequestedRefreshPreservesConsumerDispatchAndFirstCompletionWins(
		bool hasCommand, bool canExecute, bool hasEvent)
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		var view = new TestListView { IsRefreshEnabled = true };
		var completions = new List<Action>();
		var consumers = new List<string>();
		if (hasCommand)
			view.RefreshCommand = new Microsoft.Maui.Controls.Command(parameter =>
			{
				consumers.Add("command");
				completions.Add(Assert.IsType<Action>(parameter));
			}, parameter =>
			{
				Assert.Null(parameter);
				return canExecute;
			});
		if (hasEvent)
			view.OnRefresh += (_, args) =>
			{
				consumers.Add("event");
				completions.Add(args.Complete);
			};
		var starts = 0;
		var finishes = 0;
		refresh.Request(() => view.IsRefreshEnabled, () => refresh.Begin(callback =>
		{
			starts++;
			((IVirtualListView)view).Refresh(callback);
		}, () => finishes++));
		dispatcher.Drain();
		Assert.Equal(1, starts);
		var expected = new List<string>();
		if (hasCommand && canExecute)
			expected.Add("command");
		if (hasEvent)
			expected.Add("event");
		Assert.Equal(expected, consumers);
		Assert.Equal(completions.Count == 0 ? 1 : 0, finishes);
		foreach (var completion in completions)
		{
			completion();
			completion();
			dispatcher.Drain();
			Assert.Equal(1, finishes);
		}
		Assert.Equal(1, finishes);
	}

	[Fact]
	public async Task RequestedRefreshAndItsCompletionUseTheOwningDispatcher()
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		Action complete = null;
		var finishes = 0;
		await Task.Run(() => refresh.Request(() =>
		{
			Assert.True(dispatcher.IsDraining);
			return true;
		}, () => refresh.Begin(callback =>
		{
			Assert.True(dispatcher.IsDraining);
			complete = callback;
		}, () =>
		{
			Assert.True(dispatcher.IsDraining);
			finishes++;
		})));
		Assert.Null(complete);
		dispatcher.Drain();
		await Task.Run(() => { complete(); complete(); });
		Assert.Equal(0, finishes);
		dispatcher.Drain();
		Assert.Equal(1, finishes);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RequestedConsumerFailureEndsFeedbackAndDoesNotBlockLaterRequests(bool commandFails)
	{
		var dispatcher = new TestDispatcher();
		var publication = new HandlerPublication();
		publication.Connect(dispatcher, null, () => { });
		var refresh = new OwnedRefresh(publication);
		var view = new TestListView();
		var failure = new ApplicationException("consumer");
		if (commandFails)
			view.RefreshCommand = new Microsoft.Maui.Controls.Command(() => throw failure);
		else
			view.OnRefresh += (_, _) => throw failure;
		var finishes = 0;
		refresh.Request(() => true, () => refresh.Begin(
			callback => ((IVirtualListView)view).Refresh(callback), () => finishes++));
		Assert.Same(failure, Assert.Throws<ApplicationException>(dispatcher.Drain));
		Assert.Equal(1, finishes);
		var requests = 0;
		refresh.Request(() => true, () => requests++);
		dispatcher.Drain();
		Assert.Equal(1, requests);
	}

	sealed class RequestHandler : IViewHandler
	{
		public RequestHandler(IView view) => VirtualView = view;
		public string Command { get; private set; }
		public object Arguments { get; private set; }
		public int Invocations { get; private set; }
		public bool HasContainer { get; set; }
		public object ContainerView => null;
		public object PlatformView => null;
		public IView VirtualView { get; private set; }
		IElement IElementHandler.VirtualView => VirtualView;
		public IMauiContext MauiContext => null;
		public Size GetDesiredSize(double width, double height) => Size.Zero;
		public void PlatformArrange(Rect frame) { }
		public void SetMauiContext(IMauiContext context) { }
		public void SetVirtualView(IElement view) => VirtualView = (IView)view;
		public void UpdateValue(string property) { }
		public void Invoke(string command, object args)
		{
			Command = command;
			Arguments = args;
			Invocations++;
		}
		public void DisconnectHandler() { }
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
