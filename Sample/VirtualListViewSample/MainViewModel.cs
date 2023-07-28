using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace VirtualListViewSample;

public partial class MainViewModel : ObservableObject
{
	public MainViewModel()
	{
		Adapter = new MusicDataAdapter();
	}

	[ObservableProperty]
	MusicDataAdapter adapter;

	[RelayCommand]
	async Task Refresh()
	{
		await Task.Delay(3000);
	}

	[RelayCommand]
	void Scrolled(ScrolledEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"Scrolled: {e.ScrollX}, {e.ScrollY}");
	}
}
