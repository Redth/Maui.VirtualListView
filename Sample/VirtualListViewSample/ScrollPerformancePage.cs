using Microsoft.Maui.Adapters;
using Microsoft.Maui.Controls.Shapes;

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
		Items = { "Label only", "Label + 1 Entry", "Label + 10 Entries", "Nested wrapping layout", "Mixed form controls", "Local image cards" },
		SelectedIndex = 2,
		AutomationId = "scroll-complexity"
	};
	Counters counters;
	Action scrollToTop;
	bool virtualList = true;
	int entryCount;
	RowTemplate rowTemplate;

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
				new Label { Text = "200 resident rows. No database or network. Images are bundled locally. Compare the same gestures in Release without a debugger.", FontSize = 12 },
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
		rowTemplate = (RowTemplate)complexity.SelectedIndex;
		entryCount = rowTemplate switch { RowTemplate.OneEntry or RowTemplate.MixedControls => 1, RowTemplate.TenEntries => 10, _ => 0 };
		var nextCounters = new Counters();
		var entries = entryCount;
		var selectedTemplate = rowTemplate;
		var checkSpelling = spellCheck.IsToggled;
		var template = new DataTemplate(() => new InstrumentedRow(nextCounters, selectedTemplate, entries, checkSpelling));
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
		summary.Text = $"{name}; Entries={entryCount}; SpellCheck={(spellCheck.IsToggled ? 1 : 0)}; Template={(int)rowTemplate}; Created={counters.Created}; Bound={counters.Bound}; "
			+ $"Measured={counters.Measured}; Arranged={counters.Arranged}";
		Console.WriteLine($"[ScrollRepro] {summary.Text}");
	}

	static Button Button(string text, string id, Action action)
	{
		var button = new Button { Text = text, AutomationId = id, Padding = new Thickness(6, 2), FontSize = 12 };
		button.Clicked += (_, _) => action();
		return button;
	}

	enum RowTemplate
	{
		LabelOnly,
		OneEntry,
		TenEntries,
		NestedLayout,
		MixedControls,
		LocalImages
	}

	sealed class Row
	{
		public Row(int index)
		{
			Title = $"Row {index:D3}";
			Values = Enumerable.Range(0, 10).Select(field => $"Value {index:D3}-{field}").ToArray();
			Description = string.Join(" ", Enumerable.Repeat(
				"Resident content wraps differently as recycled rows change size.", 1 + index % 4));
			CategoryIndex = index % 3;
			Category = new[] { "Planned", "In progress", "Reviewed" }[CategoryIndex];
			Enabled = index % 2 == 0;
			Reviewed = index % 3 == 0;
			Progress = index % 101;
			Image = index % 2 == 0 ? "scroll_landscape.png" : "scroll_landscape_alt.png";
			AlternateImage = index % 2 == 0 ? "scroll_landscape_alt.png" : "scroll_landscape.png";
		}

		public string Title { get; }
		public string[] Values { get; }
		public string Description { get; }
		public int CategoryIndex { get; }
		public string Category { get; }
		public bool Enabled { get; }
		public bool Reviewed { get; }
		public double Progress { get; }
		public string Image { get; }
		public string AlternateImage { get; }
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

		public InstrumentedRow(Counters counters, RowTemplate template, int entryCount, bool checkSpelling)
		{
			this.counters = counters;
			counters.Created++;
			Padding = 8;
			var content = new VerticalStackLayout { Spacing = 4 };
			var title = new Label { FontSize = 14, HeightRequest = 24 };
			title.SetBinding(Label.TextProperty, nameof(Row.Title), BindingMode.OneWay);
			content.Add(title);
			switch (template)
			{
				case RowTemplate.NestedLayout:
					content.Add(NestedLayout());
					break;
				case RowTemplate.MixedControls:
					content.Add(MixedControls(checkSpelling));
					break;
				case RowTemplate.LocalImages:
					content.Add(LocalImages());
					break;
				case RowTemplate.LabelOnly:
				case RowTemplate.OneEntry:
				case RowTemplate.TenEntries:
					if (entryCount > 0)
						content.Add(EntryFields(entryCount, checkSpelling));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(template));
			}
			Content = content;
			BindingContextChanged += (_, _) =>
			{
				if (BindingContext is Row)
					counters.Bound++;
			};
		}

		static Grid EntryFields(int entryCount, bool checkSpelling)
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
			return fields;
		}

		static Border NestedLayout()
		{
			var heading = new Grid
			{
				ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) },
				ColumnSpacing = 8
			};
			heading.Add(BoundLabel(nameof(Row.Category), 12));
			heading.Add(new Label { Text = "Resident data", FontSize = 12 }, 1);
			var details = new VerticalStackLayout
			{
				Spacing = 6,
				Children =
				{
					heading,
					BoundLabel(nameof(Row.Description), 14),
					new HorizontalStackLayout
					{
						Spacing = 12,
						Children =
						{
							BoundLabel($"{nameof(Row.Values)}[0]", 11),
							BoundLabel($"{nameof(Row.Values)}[1]", 11)
						}
					}
				}
			};
			return new Border
			{
				Padding = 12,
				Stroke = Colors.SlateGray,
				StrokeShape = new RoundRectangle { CornerRadius = 8 },
				Content = new Grid { Children = { details } }
			};
		}

		static Grid MixedControls(bool checkSpelling)
		{
			var fields = new Grid
			{
				ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) },
				RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
				ColumnSpacing = 8,
				RowSpacing = 4
			};
			var entry = new Entry { FontSize = 14, HeightRequest = 44, IsSpellCheckEnabled = checkSpelling };
			entry.SetBinding(Entry.TextProperty, $"{nameof(Row.Values)}[0]", BindingMode.OneWay);
			fields.Add(entry);
			var category = new Picker { Title = "Status", Items = { "Planned", "In progress", "Reviewed" }, FontSize = 14 };
			category.SetBinding(Picker.SelectedIndexProperty, nameof(Row.CategoryIndex), BindingMode.OneWay);
			fields.Add(category, 1);
			var enabled = new Switch();
			enabled.SetBinding(Switch.IsToggledProperty, nameof(Row.Enabled), BindingMode.OneWay);
			SemanticProperties.SetDescription(enabled, "Enabled");
			fields.Add(new HorizontalStackLayout
			{
				Spacing = 4,
				Children = { new Label { Text = "Enabled", VerticalOptions = LayoutOptions.Center }, enabled }
			}, 0, 1);
			var reviewed = new CheckBox();
			reviewed.SetBinding(CheckBox.IsCheckedProperty, nameof(Row.Reviewed), BindingMode.OneWay);
			SemanticProperties.SetDescription(reviewed, "Reviewed");
			fields.Add(new HorizontalStackLayout
			{
				Spacing = 4,
				Children = { new Label { Text = "Reviewed", VerticalOptions = LayoutOptions.Center }, reviewed }
			}, 1, 1);
			var progress = new Slider { Minimum = 0, Maximum = 100 };
			progress.SetBinding(Slider.ValueProperty, nameof(Row.Progress), BindingMode.OneWay);
			SemanticProperties.SetDescription(progress, "Progress");
			fields.Add(progress, 0, 2);
			Grid.SetColumnSpan(progress, 2);
			return fields;
		}

		static VerticalStackLayout LocalImages()
		{
			var images = new Grid
			{
				ColumnDefinitions = { new(new GridLength(2, GridUnitType.Star)), new(GridLength.Star) },
				RowDefinitions = { new(new GridLength(72)), new(new GridLength(72)) },
				ColumnSpacing = 8,
				RowSpacing = 8
			};
			var large = BoundImage(nameof(Row.Image), 152);
			images.Add(large);
			Grid.SetRowSpan(large, 2);
			images.Add(BoundImage(nameof(Row.AlternateImage), 72), 1);
			images.Add(BoundImage(nameof(Row.Image), 72), 1, 1);
			return new VerticalStackLayout
			{
				Spacing = 6,
				Children = { images, BoundLabel(nameof(Row.Description), 14) }
			};
		}

		static Label BoundLabel(string path, double size)
		{
			var label = new Label { FontSize = size, LineBreakMode = LineBreakMode.WordWrap };
			label.SetBinding(Label.TextProperty, path, BindingMode.OneWay);
			return label;
		}

		static Image BoundImage(string path, double height)
		{
			var image = new Image { HeightRequest = height, Aspect = Aspect.AspectFill };
			image.SetBinding(Image.SourceProperty, path, BindingMode.OneWay);
			return image;
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
