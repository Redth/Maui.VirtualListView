using System.Collections.ObjectModel;
using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public sealed class MeasurementPage : ContentPage
{
	const string LongText = "Long wrapping text should use the available width, including after a resize. "
		+ "The root margin and both levels of padding must remain visible. ";
	readonly VirtualListView list;
	readonly ObservableCollection<MeasurementItem> items = new();
	readonly MeasurementItem[] rows;
	readonly MeasurementItem header = new("HEADER\n" + LongText + "END HEADER");
	readonly MeasurementItem footer = new("FOOTER\n" + LongText + "END FOOTER");
	bool expanded;
	bool largeFont;
	bool narrow;
	int countMode;

	public MeasurementPage()
	{
		Title = "Measurement smoke";
		rows = Enumerable.Range(0, 60).Select(index => new MeasurementItem(
			$"Row {index:D2}\n" + (index % 2 == 0 ? "Short text. " : LongText) + $"END ROW {index:D2}")).ToArray();
		foreach (var row in rows)
			items.Add(row);

		list = new VirtualListView
		{
			AutomationId = "measurement-list",
			Adapter = new ObservableCollectionAdapter<MeasurementItem>(items),
			ItemTemplate = new DataTemplate(CreateCard),
			GlobalHeader = CardFor(header),
			GlobalFooter = CardFor(footer),
			ShowHeaderWhenEmpty = true,
			ShowFooterWhenEmpty = true,
			EmptyView = new Label { Text = "No rows. Header and footer must still size themselves.", Margin = 12 }
		};
		var axes = new HorizontalStackLayout
		{
			Children =
			{
				Button("Orientation", "measurement-orientation", ToggleOrientation),
				Button("Width", "measurement-width", () =>
				{
					narrow = !narrow;
					list.WidthRequest = narrow ? 280 : -1;
				}),
				Button("0 / 1 / 60", "measurement-count", () =>
				{
					countMode = (countMode + 1) % 3;
					items.Clear();
					foreach (var row in rows.Take(countMode == 0 ? 60 : countMode == 1 ? 0 : 1))
						items.Add(row);
				}),
				Button("0 / 1", "measurement-single", () =>
				{
					var addItem = items.Count == 0;
					items.Clear();
					if (addItem)
						items.Add(rows[0]);
				})
			}
		};
		var changes = new HorizontalStackLayout
		{
			Children =
			{
				Button("Expand text", "measurement-expand", () =>
				{
					expanded = !expanded;
					foreach (var row in AllContent())
						row.Expanded = expanded;
				}),
				Button("Font size", "measurement-font", () =>
				{
					largeFont = !largeFont;
					foreach (var row in AllContent())
						row.FontSize = largeFont ? 22 : 14;
				})
			}
		};
		var chrome = new HorizontalStackLayout
		{
			Children =
			{
				Button("Empty header", "measurement-empty-header", () => list.ShowHeaderWhenEmpty = !list.ShowHeaderWhenEmpty),
				Button("Empty footer", "measurement-empty-footer", () => list.ShowFooterWhenEmpty = !list.ShowFooterWhenEmpty),
				Button("Header visible", "measurement-header-visible", () => list.IsHeaderVisible = !list.IsHeaderVisible),
				Button("Footer visible", "measurement-footer-visible", () => list.IsFooterVisible = !list.IsFooterVisible)
			}
		};
		var grid = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
		grid.Add(new Label { Text = "Check every END marker, spacing, and chrome. Scroll back, change size, and repeat.", Margin = 8 }, 0, 0);
		grid.Add(axes, 0, 1);
		grid.Add(changes, 0, 2);
		grid.Add(chrome, 0, 3);
		grid.Add(list, 0, 4);
		Content = grid;
	}

	IEnumerable<MeasurementItem> AllContent() => rows.Append(header).Append(footer);

	void ToggleOrientation()
	{
		list.Orientation = list.Orientation == ListOrientation.Vertical ? ListOrientation.Horizontal : ListOrientation.Vertical;
		foreach (var row in AllContent())
			row.CardWidth = list.Orientation == ListOrientation.Horizontal ? 260 : -1;
	}

	static Button Button(string text, string id, Action action)
	{
		var button = new Button { Text = text, AutomationId = id, Padding = new Thickness(8, 4), FontSize = 12 };
		button.Clicked += (_, _) => action();
		return button;
	}

	static View CardFor(MeasurementItem item)
	{
		var view = CreateCard();
		view.BindingContext = item;
		return view;
	}

	static View CreateCard()
	{
		var label = new Label { LineBreakMode = LineBreakMode.WordWrap, TextColor = Colors.Black };
		label.SetBinding(Label.TextProperty, nameof(MeasurementItem.Text));
		label.SetBinding(Label.FontSizeProperty, nameof(MeasurementItem.FontSize));
		var root = new ContentView
		{
			Margin = new Thickness(12, 6),
			Padding = new Thickness(8, 10),
			BackgroundColor = Colors.LightBlue,
			Content = new VerticalStackLayout
			{
				Padding = 6,
				BackgroundColor = Colors.White,
				Children = { label }
			}
		};
		root.SetBinding(WidthRequestProperty, nameof(MeasurementItem.CardWidth));
		return root;
	}

	sealed class MeasurementItem : BindableObject
	{
		readonly string original;
		bool expanded;
		double fontSize = 14;
		double cardWidth = -1;
		public MeasurementItem(string text) => original = text;
		public string Text => expanded ? original + "\nExpanded content. " + LongText + "END EXPANSION" : original;
		public bool Expanded
		{
			set { expanded = value; OnPropertyChanged(nameof(Text)); }
		}
		public double FontSize
		{
			get => fontSize;
			set { fontSize = value; OnPropertyChanged(); }
		}
		public double CardWidth
		{
			get => cardWidth;
			set { cardWidth = value; OnPropertyChanged(); }
		}
	}
}
