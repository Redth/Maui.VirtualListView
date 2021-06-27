using System.ComponentModel;

namespace VirtualListViewSample
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public MainViewModel()
		{
			Adapter = new JsonGroupedAdapter();
		}

		public JsonGroupedAdapter Adapter { get; set; }

		public void NotifyPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}

}
