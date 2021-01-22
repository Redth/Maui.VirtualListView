using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

			if (item is Animal)
				return AnimalTemplate;
			else if (item is Person)
				return PersonTemplate;

			return PersonTemplate;
		}
	}

	public class MainViewModel : INotifyPropertyChanged
	{
		public ListAdapter<Creature> Adapter
		{ get; set; }
			= new ListAdapter<Creature>
			{
				Items = new List<Creature>
				{
					new Person {
						FirstName = "Mr",
						LastName = "Happy",
						Description = "This is a short one" },
					new Animal {
						Breed = "Dog",
						Sound = "Bark",
						Description = "This is a long one\r\n with more than one line\r\nin fact there are several lines to consider and the text can be long and potentially need to wrap to the next line too "
					},
					new Person {
						FirstName = "Mr",
						LastName = "Sad",
						Description = "This is a short one" },
					new Animal {
						Breed = "Cat",
						Sound = "Meow",
						Description = "This is a long one\r\n with more than one line"
					},
					new Animal {
						Breed = "Penguin",
						Sound = "?",
						Description = "Not sure"
					}
				}
			};


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

	public class Creature
	{
		public string Description { get; set; }
	}


	public class Person : Creature
	{
		public string FirstName { get; set; }

		public string LastName { get; set; }
	}

	public class Animal : Creature
	{
		public string Breed { get; set; }

		public string Sound { get; set; }
	}
}
