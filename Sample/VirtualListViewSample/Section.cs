namespace VirtualListViewSample;

public class Section : List<Item>
{
	public string Title { get; set; }
}

public class Item
{
	public string Text { get; set; }
	public double Height { get; set; }
}
