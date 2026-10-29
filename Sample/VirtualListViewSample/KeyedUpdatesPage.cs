using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public sealed class KeyedUpdatesPage : ContentPage
{
	readonly List<SnapshotItem> first = new();
	readonly List<SnapshotItem> second = new();
	readonly SnapshotAdapter adapter;
	readonly VirtualListView list;
	readonly Label status = new() { Margin = 8, FontSize = 14 };
	long revision;
	int nextKey = 80;
	bool reversedSections;
	bool narrow;

	public KeyedUpdatesPage()
	{
		Title = "Keyed updates";
		for (var index = 0; index < 80; index++)
			(index < 40 ? first : second).Add(Row(index));
		adapter = new SnapshotAdapter(Snapshot());
		list = new VirtualListView
		{
			AutomationId = "keyed-list",
			Adapter = adapter,
			SelectionMode = Microsoft.Maui.SelectionMode.Multiple,
			ItemTemplate = new DataTemplate(CreateRow),
			SectionHeaderTemplate = new DataTemplate(() =>
			{
				var label = new Label { FontAttributes = FontAttributes.Bold, Margin = 12 };
				label.SetBinding(Label.TextProperty, ".");
				return label;
			}),
			SectionFooterTemplate = new DataTemplate(() => new Label { Text = "End of section", Margin = 12 }),
			GlobalHeader = new Label { Text = "Global header - not selectable", Margin = 12 },
			GlobalFooter = new Label { Text = "Global footer - not selectable", Margin = 12 },
			EmptyView = new Label { Text = "No items remain.", Margin = 12 }
		};
		list.OnSelectedItemsChanged += (_, _) => UpdateStatus();
		var actions = new Grid
		{
			ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) },
			RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) },
			ColumnSpacing = 4,
			RowSpacing = 4,
			Margin = 8
		};
		actions.Add(Button("Prepend", "keyed-prepend", () => { first.Insert(0, Row(nextKey++)); Publish(); }), 0, 0);
		actions.Add(Button("Insert", "keyed-insert", () => { first.Insert(Math.Min(1, first.Count), Row(nextKey++)); Publish(); }), 1, 0);
		actions.Add(Button("Remove visible", "keyed-remove", RemoveVisible), 2, 0);
		actions.Add(Button("Reorder", "keyed-reorder", () =>
		{
			if (first.Count > 1)
			{
				var row = first[^1];
				first.RemoveAt(first.Count - 1);
				first.Insert(0, row);
			}
			Publish();
		}), 0, 1);
		actions.Add(Button("Change content", "keyed-content", ChangeContent), 1, 1);
		actions.Add(Button("Swap sections", "keyed-sections", () => { reversedSections = !reversedSections; Publish(); }), 2, 1);
		actions.Add(Button("Three edits", "keyed-burst", () =>
		{
			first.Insert(0, Row(nextKey++)); Publish();
			first.Insert(0, Row(nextKey++)); Publish();
			first.RemoveAt(0); Publish();
		}), 0, 2);
		actions.Add(Button("Go to middle", "keyed-scroll", () =>
			list.ScrollToItem(new ItemPosition(0, Math.Max(0, CurrentFirst.Count / 2)), false)), 1, 2);
		actions.Add(Button("Resize", "keyed-resize", () =>
		{
			narrow = !narrow;
			list.WidthRequest = narrow ? 280 : -1;
		}), 2, 2);
		var grid = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
		grid.Add(new Label { Text = "Select rows, scroll to the middle, then edit. The same row should stay in place; removed selections should disappear.", Margin = 8 }, 0, 0);
		grid.Add(actions, 0, 1);
		grid.Add(status, 0, 2);
		grid.Add(list, 0, 3);
		Content = grid;
		UpdateStatus();
	}

	List<SnapshotItem> CurrentFirst => reversedSections ? second : first;
	List<SnapshotItem> CurrentSecond => reversedSections ? first : second;
	static SnapshotItem Row(int key) => new($"row-{key:D3}", 0, new RowContent($"row-{key:D3}", $"Row {key:D3} - original content"));

	VirtualListViewSnapshot Snapshot()
	{
		var a = new SnapshotSection("first", 0, "First section", first);
		var b = new SnapshotSection("second", 0, "Second section", second);
		return new(revision, reversedSections ? new[] { b, a } : new[] { a, b });
	}

	void Publish()
	{
		revision++;
		adapter.Submit(Snapshot());
		UpdateStatus();
	}

	(List<SnapshotItem> Rows, int Index) VisibleRow()
	{
		var position = list.FindVisiblePositions().Where(info => info.Kind == PositionKind.Item)
			.OrderBy(info => info.SectionIndex).ThenBy(info => info.ItemIndex).FirstOrDefault();
		if (position is null)
			return (CurrentFirst.Count > 0 ? CurrentFirst : CurrentSecond, 0);
		return (position.SectionIndex == 0 ? CurrentFirst : CurrentSecond, position.ItemIndex);
	}

	void RemoveVisible()
	{
		var visible = VisibleRow();
		if (visible.Rows.Count > visible.Index)
			visible.Rows.RemoveAt(visible.Index);
		Publish();
	}

	void ChangeContent()
	{
		var visible = VisibleRow();
		if (visible.Rows.Count > visible.Index)
		{
			var old = visible.Rows[visible.Index];
			var nextRevision = old.Revision + 1;
			visible.Rows[visible.Index] = new(old.Key, nextRevision, new RowContent(old.Key,
				$"{old.Key} - content revision {nextRevision}\n"
				+ string.Join(" ", Enumerable.Repeat("Longer content must rebind and remeasure without losing this key.", (int)(nextRevision % 3) + 1))));
		}
		Publish();
	}

	void UpdateStatus()
	{
		var snapshot = list.AcceptedSnapshot ?? adapter.CaptureSnapshot();
		var selected = (list.SelectedItems ?? Array.Empty<ItemPosition>())
			.Where(position => position.SectionIndex >= 0 && position.SectionIndex < snapshot.Sections.Count
				&& position.ItemIndex >= 0 && position.ItemIndex < snapshot.Sections[position.SectionIndex].Items.Count)
			.Select(position => snapshot.Sections[position.SectionIndex].Items[position.ItemIndex].Key);
		status.Text = $"Revision {revision} | {first.Count + second.Count} rows | Selected: {string.Join(", ", selected)}";
	}

	static Button Button(string text, string id, Action action)
	{
		var button = new Button { Text = text, AutomationId = id, Padding = 6, FontSize = 12 };
		button.Clicked += (_, _) => action();
		return button;
	}

	static View CreateRow()
	{
		var label = new Label { Margin = 12, LineBreakMode = LineBreakMode.WordWrap };
		label.SetBinding(Label.TextProperty, nameof(RowContent.Text));
		var cell = new VirtualViewCell { Content = label };
		cell.Triggers.Add(new DataTrigger(typeof(VirtualViewCell))
		{
			Binding = new Binding(nameof(VirtualViewCell.IsSelected), source: cell),
			Value = true,
			Setters = { new Setter { Property = BackgroundColorProperty, Value = Colors.LightBlue } }
		});
		return cell;
	}

	public sealed record RowContent(string Key, string Text);
}
