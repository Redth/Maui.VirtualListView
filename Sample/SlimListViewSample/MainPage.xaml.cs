using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using Xamarin.Forms;
using XFSlimListView;

namespace SlimListViewSample
{
	public partial class MainPage : ContentPage
	{
		MainViewModel vm;

		public MainPage()
		{
			InitializeComponent();
			vm = new MainViewModel();
			BindingContext = vm;
		}

		void SlimListView_SelectedItemsChanged(System.Object sender, XFSlimListView.SelectedItemsChangedEventArgs e)
		{
			Console.WriteLine($"Selected Items:");
			foreach (var s in e.NewSelection)
			{
				Console.WriteLine($"  -> {s.SectionIndex} ... {s.ItemIndex}");
			}
		}
	}

	public class SelectedColorConverter : IValueConverter
	{
		static readonly Color SelectedColor = Color.DarkBlue;
		static readonly Color UnselectedColor = Color.Transparent;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool b && b)
				return SelectedColor;

			return UnselectedColor;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class ItemSelector : XFSlimListView.AdapterItemDataTemplateSelector
	{
		public DataTemplate PersonTemplate { get; set; }
		public DataTemplate AnimalTemplate { get; set; }

		public ItemSelector()
		{
			PersonTemplate = new DataTemplate(typeof(PersonView));
			AnimalTemplate = new DataTemplate(typeof(AnimalView));
		}

		public override DataTemplate SelectItemTemplate(ISlimListViewAdapter adapter, int sectionIndex, int itemIndex)
		{
			var item = adapter.Item(sectionIndex, itemIndex);

			return PersonTemplate;
		}
	}

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

	public class ListAdapter<T> : ISlimListViewAdapter
	{
		public List<T> Items { get; set; } = new List<T>();

		public int Sections => 1;

		public object Item(int sectionIndex, int itemIndex)
			=> Items[itemIndex];

		public int ItemsForSection(int sectionIndex)
			=> Items.Count;

		public object Section(int sectionIndex)
			=> null;
	}

	public class GroupedAdapter<TGroup, TItem> : ISlimListViewAdapter
		where TGroup : IList<TItem> 
	{
		
		public List<TGroup> Groups { get; set; } = new List<TGroup>();

		public int Sections => Groups.Count;

		public object Item(int sectionIndex, int itemIndex)
			=> Groups[sectionIndex][itemIndex];

		public int ItemsForSection(int sectionIndex)
 			=> Groups[sectionIndex].Count;

		public object Section(int sectionIndex)
			=> Groups[sectionIndex];
	}


	public class SqliteGroupedAdapter<TGroup, TItem> : ISlimListViewAdapter
		where TGroup : IList<TItem>
	{

		public class DbPersonGroup
		{
			public string GroupName { get; set; }

			public int GroupCount { get; set; }
		}

		public SqliteGroupedAdapter()
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "sampledata.db");
			
			if (!File.Exists(path))
			{
				// note that the prefix includes the trailing period '.' that is required
				var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
				using (var stream = assembly.GetManifestResourceStream(nameof(SlimListViewSample) + ".sampledata.db"))
				using (var outStream = File.Create(path))
					stream.CopyTo(outStream);
			}

			Db = new SQLiteConnection(path);
			Db.CreateTable<Person>();
		}

		public SQLiteConnection Db { get; }

		List<DbPersonGroup> sections;

		List<DbPersonGroup> GetSections()
		{
			if (sections == null)
				sections = Db.Query<DbPersonGroup>("select IndexCharacter as GroupName, count(IndexCharacter) as GroupCount from Person group by IndexCharacter ORDER BY IndexCharacter");

			return sections;
		}

		public int Sections
			=> GetSections().Count;

		public object Item(int sectionIndex, int itemIndex)
			=> Db.Query<Person>(
				"select * from Person Where IndexCharacter = ? ORDER BY LastName, FirstName LIMIT 1 OFFSET " + itemIndex,
				GetSections()[sectionIndex].GroupName)
				?.FirstOrDefault();

		public int ItemsForSection(int sectionIndex)
 			=> GetSections()[sectionIndex].GroupCount;

		public object Section(int sectionIndex)
			=> GetSections()[sectionIndex];
	}

}
