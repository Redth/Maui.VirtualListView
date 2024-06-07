namespace VirtualListViewSample;

public class Section : List<Item>
{
	public string Title { get; set; }
}


public class Item : BindableObject
{

    public static readonly BindableProperty NameProperty =
        BindableProperty.Create(nameof(Name), typeof(string), typeof(Item), string.Empty);

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }
}