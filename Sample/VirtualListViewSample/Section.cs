using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VirtualListViewSample;

public class Section : List<SectionItem>
{
	public string Title { get; set; }
}

public partial class SectionItem : ObservableObject
{
	[ObservableProperty]
	string text;

	[ObservableProperty]
	bool isDetailVisible;

	[RelayCommand]
	void ToggleDetail()
	{
		IsDetailVisible = !IsDetailVisible;
	}
}
