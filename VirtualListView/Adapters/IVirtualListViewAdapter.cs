
namespace Microsoft.Maui.Adapters;

internal class EmptyViewSelector : IVirtualListViewSelector
{
    public IView CreateView(PositionInfo position, object data)
		=> null;
    
    public string GetReuseId(PositionInfo position, object data)
    	=> "ITEM";

    public void RecycleView(PositionInfo position, object data, IView view)
    {
    }

    public bool SectionHasFooter(int sectionIndex)
    	=> false;

    public bool SectionHasHeader(int sectionIndex)
    	=> false;
}

internal class EmptyAdapter : IVirtualListViewAdapter
{
	public int GetNumberOfSections()
		=> 0;

	public object GetSection(int sectionIndex)
		=> null;

	public int GetNumberOfItemsInSection(int sectionIndex)
		=> 0;

	public object GetItem(int sectionIndex, int itemIndex)
		=> null;
	
	public event EventHandler OnDataInvalidated;
	
	public void InvalidateData()
	{
	}
}

public interface IVirtualListViewAdapter
{
	int GetNumberOfSections();

	object GetSection(int sectionIndex);

	int GetNumberOfItemsInSection(int sectionIndex);

	object GetItem(int sectionIndex, int itemIndex);

	event EventHandler OnDataInvalidated;

	void InvalidateData();
}
