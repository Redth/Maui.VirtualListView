using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public sealed class ScrollPerformancePage : ContentPage
{
	readonly Row[] rows = Enumerable.Range(0, 200).Select(index => new Row(index)).ToArray();
	readonly ContentView host = new();
	readonly Label summary = new() { FontSize = 11, HeightRequest = 52, MaxLines = 3 };
	readonly Switch spellCheck = new() { IsToggled = true, AutomationId = "scroll-spell-check" };
	readonly Picker complexity = new()
	{
		Title = "Row content",
		Items = { "Label only", "Label + 1 Entry", "Label + 10 Entries" },
		SelectedIndex = 2,
		AutomationId = "scroll-complexity"
	};
	Counters counters;
	Action scrollToTop;
	bool virtualList = true;
	int entryCount;

	public ScrollPerformancePage()
	{
		Title = "Scrolling reproduction";
		var controls = new Grid
		{
			ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) },
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) },
			ColumnSpacing = 4,
			RowSpacing = 4
		};
		controls.Add(Button("VirtualListView", "scroll-virtual", () => ShowList(true)), 0, 0);
		controls.Add(Button("CollectionView", "scroll-collection", () => ShowList(false)), 1, 0);
		controls.Add(Button("Top", "scroll-top", () => scrollToTop()), 0, 1);
		controls.Add(Button("Snapshot", "scroll-snapshot", Snapshot), 1, 1);
		complexity.SelectedIndexChanged += (_, _) => ShowList(virtualList);
		spellCheck.Toggled += (_, _) => ShowList(virtualList);
		SemanticProperties.SetDescription(spellCheck, "Spell check for Entry fields");
		var chrome = new VerticalStackLayout
		{
			Padding = 8,
			Spacing = 4,
			Children =
			{
				new Label { Text = "200 resident rows. No database or images. Compare the same gestures in Release without a debugger.", FontSize = 12 },
				controls,
				complexity,
				new HorizontalStackLayout
				{
					Spacing = 8,
					Children =
					{
						new Label { Text = "Entry spell check", FontSize = 12, VerticalOptions = LayoutOptions.Center },
						spellCheck
					}
				},
				summary
			}
		};
		var grid = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
		grid.Add(chrome);
		grid.Add(host, 0, 1);
		Content = grid;
		ShowList(true);
	}

	void ShowList(bool useVirtualList)
	{
		virtualList = useVirtualList;
		entryCount = complexity.SelectedIndex switch { 0 => 0, 1 => 1, _ => 10 };
		var nextCounters = new Counters();
		var entries = entryCount;
		var checkSpelling = spellCheck.IsToggled;
		var template = new DataTemplate(() => new InstrumentedRow(nextCounters, entries, checkSpelling));
		View next = virtualList
			? new VirtualListView
			{
				AutomationId = "scroll-list",
				Adapter = new VirtualListViewAdapter<Row>(rows),
				ItemTemplate = template,
				SelectionMode = Microsoft.Maui.SelectionMode.None
			}
			: new CollectionView
			{
				AutomationId = "scroll-list",
				ItemsSource = rows,
				ItemTemplate = template,
				SelectionMode = Microsoft.Maui.Controls.SelectionMode.None,
				ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems
			};
		scrollToTop = next is VirtualListView list
			? () => list.ScrollToItem(new ItemPosition(0, 0), false)
			: () => ((CollectionView)next).ScrollTo(0, position: ScrollToPosition.Start, animate: false);
		var previous = host.Content;
		host.Content = next;
		previous?.Handler?.DisconnectHandler();
		counters = nextCounters;
		Snapshot();
	}

	void Snapshot()
	{
		var name = virtualList ? "VirtualListView" : "CollectionView";
		summary.Text = $"{name}; Entries={entryCount}; SpellCheck={(spellCheck.IsToggled ? 1 : 0)}; Created={counters.Created}; Bound={counters.Bound}; "
			+ $"Measured={counters.Measured}; Arranged={counters.Arranged}";
		Console.WriteLine($"[ScrollRepro] {summary.Text}");
	}

	static Button Button(string text, string id, Action action)
	{
		var button = new Button { Text = text, AutomationId = id, Padding = new Thickness(6, 2), FontSize = 12 };
		button.Clicked += (_, _) => action();
		return button;
	}

	sealed class Row
	{
		public Row(int index)
		{
			Title = $"Row {index:D3}";
			Values = Enumerable.Range(0, 10).Select(field => $"Value {index:D3}-{field}").ToArray();
		}

		public string Title { get; }
		public string[] Values { get; }
	}

	sealed class Counters
	{
		public int Created;
		public int Bound;
		public int Measured;
		public int Arranged;
	}

	sealed class InstrumentedRow : ContentView
	{
		readonly Counters counters;

		public InstrumentedRow(Counters counters, int entryCount, bool checkSpelling)
		{
			this.counters = counters;
			counters.Created++;
			Padding = 8;
			var content = new VerticalStackLayout { Spacing = 4 };
			var title = new Label { FontSize = 14, HeightRequest = 24 };
			title.SetBinding(Label.TextProperty, nameof(Row.Title), BindingMode.OneWay);
			content.Add(title);
			if (entryCount > 0)
			{
				var fields = new Grid
				{
					ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) },
					ColumnSpacing = 8,
					RowSpacing = 0
				};
				for (var row = 0; row < (entryCount + 1) / 2; row++)
					fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				for (var index = 0; index < entryCount; index++)
				{
					var entry = new Entry { FontSize = 14, HeightRequest = 44, IsSpellCheckEnabled = checkSpelling };
					entry.SetBinding(Entry.TextProperty, $"{nameof(Row.Values)}[{index}]", BindingMode.OneWay);
					fields.Add(entry, index % 2, index / 2);
				}
				content.Add(fields);
			}
			Content = content;
			BindingContextChanged += (_, _) =>
			{
				if (BindingContext is Row)
					counters.Bound++;
			};
		}

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			counters.Measured++;
			return base.MeasureOverride(widthConstraint, heightConstraint);
		}

		protected override Size ArrangeOverride(Rect bounds)
		{
			counters.Arranged++;
			return base.ArrangeOverride(bounds);
		}
	}
}
