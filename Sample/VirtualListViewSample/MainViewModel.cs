using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Windows.Input;

namespace VirtualListViewSample
{
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
		async Task RefreshAsync()
		{
			await Task.Delay(3000);
			NotifyPropertyChanged(nameof(Adapter));
		}
	}

}
