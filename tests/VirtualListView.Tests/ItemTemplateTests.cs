namespace VirtualListView.Tests;

public class ItemTemplateTests
{
	[Fact]
	public void AbsentTemplateCreatesIndependentTextBoundLabelsWithStableReuseIds()
	{
		var owner = new TestListView();
		using var first = CreateSlot(owner);
		using var second = CreateSlot(owner);
		first.Bind(Item(0), "First row");
		second.Bind(Item(1), "Second row");
		var firstLabel = Assert.IsType<Label>(first.View);
		var secondLabel = Assert.IsType<Label>(second.View);
		Assert.NotSame(firstLabel, secondLabel);
		Assert.Equal("First row", firstLabel.Text);
		Assert.Equal("Second row", secondLabel.Text);
		Assert.Equal(owner.GetReuseId(Item(0), "First row"), owner.GetReuseId(Item(1), "Second row"));
		Assert.Null(owner.ItemTemplate);
		Assert.Null(owner.ItemTemplateSelector);

		first.Bind(Item(2), new TextItem());
		Assert.Same(firstLabel, first.View);
		Assert.Equal("Object row", firstLabel.Text);
		first.Bind(Item(3), null);
		Assert.Same(firstLabel, first.View);
		Assert.True(string.IsNullOrEmpty(firstLabel.Text));
	}

	[Fact]
	public void SettingAndClearingTemplateReplacesTheFallbackTreeAndReuseId()
	{
		var owner = new TestListView();
		using var slot = CreateSlot(owner);
		slot.Bind(Item(0), "Default row");
		var first = slot.View;
		var defaultId = owner.GetReuseId(Item(0), null);
		owner.ItemTemplate = new DataTemplate(() => new ContentView());
		Assert.NotEqual(defaultId, owner.GetReuseId(Item(0), null));
		slot.Bind(Item(0), "Custom row");
		var custom = Assert.IsType<ContentView>(slot.View);
		Assert.Null(((Element)first).Parent);

		owner.ItemTemplate = null;
		Assert.Equal(defaultId, owner.GetReuseId(Item(0), null));
		slot.Bind(Item(0), "Default again");
		Assert.Equal("Default again", Assert.IsType<Label>(slot.View).Text);
		Assert.NotSame(first, slot.View);
		Assert.Null(custom.Parent);
	}

	[Fact]
	public void ConfiguredSelectorTakesPrecedenceAndCanFallBackToExplicitTemplate()
	{
		var template = new DataTemplate(() => new ContentView());
		var selector = new ItemSelector { Template = new DataTemplate(() => new VirtualViewCell()) };
		var owner = new TestListView { ItemTemplate = template, ItemTemplateSelector = selector };
		using var slot = CreateSlot(owner);
		var selectedId = owner.GetReuseId(Item(0), null);
		slot.Bind(Item(0), "Selected");
		Assert.IsType<VirtualViewCell>(slot.View);
		selector.Template = null;
		Assert.NotEqual(selectedId, owner.GetReuseId(Item(0), null));
		slot.Bind(Item(0), "Explicit fallback");
		Assert.IsType<ContentView>(slot.View);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConfiguredInvalidSelectorDoesNotUseDefaultTemplate(bool invalidContent)
	{
		var owner = new TestListView
		{
			ItemTemplateSelector = new ItemSelector
			{
				Template = invalidContent ? new DataTemplate(() => new object()) : null
			}
		};
		using var slot = CreateSlot(owner);
		var error = Assert.Throws<InvalidOperationException>(() => slot.Bind(Item(0), "Row"));
		Assert.Equal("The view selector returned no view for Item.", error.Message);
		Assert.Null(slot.View);
	}

	[Fact]
	public void ConfiguredInvalidTemplateDoesNotUseDefaultTemplate()
	{
		var owner = new TestListView { ItemTemplate = new DataTemplate(() => new object()) };
		using var slot = CreateSlot(owner);
		Assert.Throws<InvalidOperationException>(() => slot.Bind(Item(0), "Row"));
		Assert.Null(slot.View);
	}

	[Fact]
	public void SelectorExceptionsAreNotHiddenByTheDefaultTemplate()
	{
		var owner = new TestListView { ItemTemplateSelector = new ThrowingSelector() };
		using var slot = CreateSlot(owner);
		var error = Assert.Throws<ApplicationException>(() => slot.Bind(Item(0), "Row"));
		Assert.Equal("Template selection failed.", error.Message);
	}

	[Fact]
	public void ConfiguredViewSelectorDoesNotUseDefaultTemplate()
	{
		var owner = new TestListView { Selector = new EmptyViewSelector() };
		using var slot = CreateSlot(owner);
		Assert.Throws<InvalidOperationException>(() => slot.Bind(Item(0), "Row"));
		Assert.Null(slot.View);
	}

	static RetainedView CreateSlot(IVirtualListView owner) => new(owner, _ => { }, () => { });
	static PositionInfo Item(int index) => PositionInfo.ForItem(index, 0, index, 10, 1, false);

	sealed class TextItem
	{
		public override string ToString() => "Object row";
	}

	sealed class ItemSelector : VirtualListViewItemTemplateSelector
	{
		public DataTemplate Template { get; set; }
		public override DataTemplate SelectTemplate(object item, int sectionIndex, int itemIndex) => Template;
	}

	sealed class ThrowingSelector : VirtualListViewItemTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, int sectionIndex, int itemIndex)
			=> throw new ApplicationException("Template selection failed.");
	}

	sealed class EmptyViewSelector : IVirtualListViewSelector
	{
		public bool SectionHasHeader(int index) => false;
		public bool SectionHasFooter(int index) => false;
		public string GetReuseId(PositionInfo position, object data) => "empty";
		public IView CreateView(PositionInfo position, object data) => null;
		public void RecycleView(PositionInfo position, object data, IView view) { }
	}
}
