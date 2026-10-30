using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public sealed class PagingPage : ContentPage
{
	const int Threshold = 3;
	readonly List<SnapshotItem> rows = new();
	readonly SnapshotAdapter adapter = new(new VirtualListViewSnapshot(0, Array.Empty<SnapshotSection>()));
	readonly VirtualListView list;
	readonly Label status = new() { AutomationId = "paging-status", VerticalOptions = LayoutOptions.Center };
	readonly ActivityIndicator loadingIndicator = new() { AutomationId = "paging-loading", IsVisible = false };
	readonly Button retry = new() { Text = "Retry page", AutomationId = "paging-retry", IsVisible = false };
	DemoPagingSource source = new();
	CancellationTokenSource cancellation;
	long revision;
	long generation;
	bool loading;
	bool hasMore = true;
	string error;

	public PagingPage()
	{
		Title = "Paging";
		list = new VirtualListView
		{
			AutomationId = "paging-list",
			Adapter = adapter,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { Margin = 12, FontSize = 16 };
				label.SetBinding(Label.TextProperty, ".");
				return label;
			}),
			SectionHeaderTemplate = new DataTemplate(() => new Label { Text = "Demo results", Margin = 12, FontAttributes = FontAttributes.Bold }),
			SectionFooterTemplate = new DataTemplate(() => new Label { Text = "End of loaded section", Margin = 12 }),
			GlobalHeader = new Label { Text = "Headers and footers do not count toward the threshold.", Margin = 12 },
			GlobalFooter = new Label { Text = "More rows appear above this footer.", Margin = 12 },
			EmptyView = new Label { Text = "No rows loaded yet.", Margin = 12 }
		};
		list.RemainingItemsThresholdReached += async (_, _) => await LoadNextPageAsync();
		retry.Clicked += async (_, _) => await LoadNextPageAsync();
		var restart = new Button { Text = "Restart", AutomationId = "paging-restart" };
		restart.Clicked += async (_, _) =>
		{
			CancelLoad();
			cancellation = new CancellationTokenSource();
			source = new DemoPagingSource();
			hasMore = true;
			error = null;
			rows.Clear();
			Publish(reset: true);
			await LoadNextPageAsync();
		};
		var actions = new Grid
		{
			Margin = 12,
			ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) },
			ColumnSpacing = 8
		};
		actions.Add(restart, 0, 0);
		actions.Add(retry, 1, 0);
		var progress = new Grid
		{
			Margin = new Thickness(12, 0, 12, 12),
			ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) },
			ColumnSpacing = 8
		};
		progress.Add(loadingIndicator, 0, 0);
		progress.Add(status, 1, 0);
		var grid = new Grid
		{
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) }
		};
		grid.Add(new Label
		{
			Text = "Scroll to load more. This delayed demo API fails once and returns one empty page; use Retry to continue.",
			Margin = new Thickness(12, 12, 12, 0)
		}, 0, 0);
		grid.Add(actions, 0, 1);
		grid.Add(progress, 0, 2);
		grid.Add(list, 0, 3);
		Content = grid;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		cancellation = new CancellationTokenSource();
		UpdateStatus();
		if (rows.Count == 0)
			await LoadNextPageAsync();
		else if (error is null && hasMore)
			list.RemainingItemsThreshold = Threshold;
	}

	protected override void OnDisappearing()
	{
		CancelLoad();
		base.OnDisappearing();
	}

	void CancelLoad()
	{
		generation++;
		list.RemainingItemsThreshold = -1;
		cancellation?.Cancel();
		cancellation?.Dispose();
		cancellation = null;
		loading = false;
	}

	async Task LoadNextPageAsync()
	{
		if (loading || !hasMore || cancellation is null)
			return;
		loading = true;
		error = null;
		list.RemainingItemsThreshold = -1;
		var requestGeneration = generation;
		var token = cancellation.Token;
		UpdateStatus();
		try
		{
			var page = await source.ReadPageAsync(rows.Count, token);
			if (token.IsCancellationRequested || requestGeneration != generation)
				return;
			if (page.Items.Count == 0 && page.HasMore)
			{
				error = "The API returned no new rows but says more exist. Retry page to continue.";
				return;
			}
			var offset = rows.Count;
			rows.AddRange(page.Items.Select((text, index) => new SnapshotItem($"row-{offset + index}", 0, text)));
			hasMore = page.HasMore;
			Publish();
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
		}
		catch (IOException exception)
		{
			if (requestGeneration == generation)
				error = exception.Message + " Retry page to continue.";
		}
		finally
		{
			if (requestGeneration == generation)
			{
				loading = false;
				UpdateStatus();
				if (error is null && hasMore)
					list.RemainingItemsThreshold = Threshold;
			}
		}
	}

	void Publish(bool reset = false)
		=> adapter.Submit(new VirtualListViewSnapshot(++revision, new[]
		{
			new SnapshotSection("results", 0, null, rows)
		}), reset);

	void UpdateStatus()
	{
		loadingIndicator.IsVisible = loading;
		loadingIndicator.IsRunning = loading;
		retry.IsVisible = error is not null;
		retry.IsEnabled = !loading;
		status.Text = loading ? $"Loading next page... {rows.Count} rows loaded."
			: error ?? (hasMore ? $"{rows.Count} rows loaded. Scroll for more." : $"{rows.Count} rows loaded. End of results.");
	}

	sealed record DemoPage(IReadOnlyList<string> Items, bool HasMore);

	sealed class DemoPagingSource
	{
		bool failed;
		bool returnedEmpty;

		public async Task<DemoPage> ReadPageAsync(int offset, CancellationToken token)
		{
			await Task.Delay(750, token);
			if (offset == 3 && !failed)
			{
				failed = true;
				throw new IOException("Demo connection interrupted.");
			}
			if (offset == 13 && !returnedEmpty)
			{
				returnedEmpty = true;
				return new(Array.Empty<string>(), true);
			}
			var count = Math.Min(offset == 0 ? 3 : 10, 43 - offset);
			var items = Enumerable.Range(offset, count).Select(index => $"Demo row {index + 1:D2}").ToArray();
			return new(items, offset + count < 43);
		}
	}
}
