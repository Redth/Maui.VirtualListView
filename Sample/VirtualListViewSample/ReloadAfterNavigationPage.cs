using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

/// <summary>
/// Scrolling up after a reload or a jump far down a list of self-sized rows. Tap "Row 150", then
/// either "Reload" or push and pop the detail page (this page reloads when it reappears, as many
/// apps do), then scroll up slowly. The visible rows must stay put while the rows above them are
/// measured.
/// </summary>
public sealed class ReloadAfterNavigationPage : ContentPage
{
	readonly VirtualListViewAdapter<Row> adapter;
	bool appeared;

	public ReloadAfterNavigationPage()
	{
		Title = "Reload after navigation";
		adapter = new VirtualListViewAdapter<Row>(Enumerable.Range(0, 400).Select(i => new Row(i)).ToList());
		var list = new VirtualListView
		{
			AutomationId = "reload-nav-list",
			Adapter = adapter,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { Padding = new Thickness(16, 8), LineBreakMode = LineBreakMode.WordWrap };
				label.SetBinding(Label.TextProperty, nameof(Row.Text));
				var box = new BoxView { Color = Colors.LightSteelBlue, Margin = new Thickness(16, 0, 16, 8) };
				box.SetBinding(HeightRequestProperty, nameof(Row.Height));
				return new VerticalStackLayout { Children = { label, box } };
			})
		};

		var down = new Button { Text = "Row 150", AutomationId = "reload-nav-down" };
		down.Clicked += (_, _) => list.ScrollToItem(new ItemPosition(0, 150), false);
		var push = new Button { Text = "Push page", AutomationId = "reload-nav-push" };
		push.Clicked += (_, _) => Navigation.PushAsync(new ContentPage
		{
			Title = "Detail",
			Content = new Button { Text = "Back", AutomationId = "reload-nav-back", Command = new Command(() => Navigation.PopAsync()) }
		});
		var reload = new Button { Text = "Reload", AutomationId = "reload-nav-reload" };
		reload.Clicked += (_, _) => adapter.InvalidateData();

		var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
		grid.Add(new HorizontalStackLayout { Spacing = 8, Padding = 8, Children = { down, push, reload } });
		grid.Add(list, 0, 1);
		Content = grid;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Coming back from the pushed page: reload, as a page refreshing its data would.
		if (appeared)
			adapter.InvalidateData();
		appeared = true;
	}

	public sealed class Row(int index)
	{
		// Much taller than UIKit's 44 pt estimate, and varying.
		public double Height { get; } = 60 + (index * 37 % 140);
		public string Text { get; } = $"Row {index}";
	}
}
