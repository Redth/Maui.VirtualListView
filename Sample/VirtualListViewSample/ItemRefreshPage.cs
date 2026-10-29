using Microsoft.Maui.Adapters;

namespace VirtualListViewSample;

public sealed class ItemRefreshPage : ContentPage
{
	readonly List<List<Row>> sections;
	readonly VirtualListViewSectionedAdapter<List<Row>, Row> adapter;
	readonly VirtualListView list;
	int revision;

	public ItemRefreshPage()
	{
		Title = "Targeted item refresh";
		sections = Enumerable.Range(0, 3).Select(section => Enumerable.Range(0, 30)
			.Select(item => new Row($"Section {section}, item {item}", false)).ToList()).ToList();
		adapter = new(sections);
		list = new VirtualListView
		{
			AutomationId = "item-refresh-list",
			Adapter = adapter,
			SelectionMode = Microsoft.Maui.SelectionMode.Multiple,
			ItemTemplateSelector = new RowTemplates(),
			SectionHeaderTemplate = new DataTemplate(() => new Label { Text = "Section", Padding = 8, FontAttributes = FontAttributes.Bold }),
			SectionFooterTemplate = new DataTemplate(() => new Label { Text = "End of section", Padding = 8 }),
			GlobalHeader = new Label { Text = "Global header: select rows, then update only those rows.", Padding = 8 },
			GlobalFooter = new Label { Text = "Global footer", Padding = 8 }
		};
		var refresh = new Button { Text = "Update selected text", AutomationId = "refresh-items" };
		refresh.Clicked += (_, _) => RefreshSelected(false);
		var template = new Button { Text = "Toggle selected template", AutomationId = "refresh-template" };
		template.Clicked += (_, _) => RefreshSelected(true);
		var first = new Button { Text = "First row", AutomationId = "refresh-first" };
		first.Clicked += (_, _) =>
		{
			list.ClearSelectedItems();
			list.SelectItem(new ItemPosition(0, 0));
			list.ScrollToItem(new ItemPosition(0, 0), false);
		};
		var offscreen = new Button { Text = "Offscreen row", AutomationId = "refresh-offscreen" };
		offscreen.Clicked += (_, _) =>
		{
			list.ClearSelectedItems();
			list.ScrollToItem(new ItemPosition(1, 10), false);
		};
		var controls = new VerticalStackLayout
		{
			Children =
			{
				new Label { Text = "With no selection, updates section 1 / item 10. Scroll there after an offscreen update.", Padding = 8 },
				refresh,
				template,
				new HorizontalStackLayout { Children = { first, offscreen } }
			}
		};
		var grid = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
		grid.Add(controls);
		grid.Add(list, 0, 1);
		Content = grid;
	}

	void RefreshSelected(bool changeTemplate)
	{
		var positions = list.SelectedItems?.ToArray() ?? Array.Empty<ItemPosition>();
		if (positions.Length == 0)
			positions = new[] { new ItemPosition(1, 10) };
		revision++;
		foreach (var position in positions)
		{
			var old = sections[position.SectionIndex][position.ItemIndex];
			var text = $"Section {position.SectionIndex}, item {position.ItemIndex}; refresh {revision}. "
				+ (revision % 2 == 0 ? "Short text." : string.Concat(Enumerable.Repeat("Longer wrapping content. ", 12)));
			sections[position.SectionIndex][position.ItemIndex] = new Row(text, changeTemplate ? !old.Alternate : old.Alternate);
		}
		adapter.InvalidateItems(positions);
	}

	public sealed record Row(string Text, bool Alternate);

	sealed class RowTemplates : VirtualListViewItemTemplateSelector
	{
		readonly DataTemplate normal = Template(Colors.LightBlue, 16);
		readonly DataTemplate alternate = Template(Colors.LightGoldenrodYellow, 22);
		public override DataTemplate SelectTemplate(object item, int sectionIndex, int itemIndex)
			=> item is Row { Alternate: true } ? alternate : normal;

		static DataTemplate Template(Color color, double fontSize) => new(() =>
		{
			var label = new Label { Padding = 12, FontSize = fontSize };
			label.SetBinding(Label.TextProperty, nameof(Row.Text));
			return new VirtualViewCell
			{
				Content = label,
				Margin = 4,
				UnselectedBackground = new SolidColorBrush(color),
				SelectedBackground = new SolidColorBrush(Colors.CornflowerBlue)
			};
		});
	}
}
