using System.ComponentModel;

namespace VirtualListViewSample
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public MainViewModel()
		{
			Adapter = new SqliteGroupedAdapter<PersonGroup, Person>();
		}

		public SqliteGroupedAdapter<PersonGroup, Person> Adapter { get; set; }

		public void NotifyPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public event PropertyChangedEventHandler PropertyChanged;
	}

}
