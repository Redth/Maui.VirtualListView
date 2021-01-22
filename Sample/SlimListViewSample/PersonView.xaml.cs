using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SlimListViewSample
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PersonView : ContentView
	{
		public PersonView()
		{
			InitializeComponent();
		}

		protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (propertyName == "BindingContext")
			{
				var bc = BindingContext;
				Console.WriteLine($"BC");
			}
			Console.WriteLine($"PersonView->{propertyName} changed");
		}
	}
}