using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace VirtualListViewSample;

public partial class MainViewModel : INotifyPropertyChanged
{
	public MainViewModel()
	{
		Adapter = new MusicDataAdapter();
	}

	public MusicDataAdapter Adapter { get; set; }

	public void NotifyPropertyChanged(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	public event PropertyChangedEventHandler PropertyChanged;

	[RelayCommand]
	async Task Refresh()
	{
		await Task.Delay(3000);
		NotifyPropertyChanged(nameof(Adapter));
	}

	[RelayCommand]
	void Scrolled(ScrolledEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"Scrolled: {e.ScrollX}, {e.ScrollY}");
	}
}
