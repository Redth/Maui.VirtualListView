namespace VirtualListViewSample;

public partial class MainPage : ContentPage, ISafeAreaView
{
	MainViewModel vm;

	public MainPage()
	{
		InitializeComponent();
	}

	private void Button_Clicked(object sender, EventArgs e)
	{
		Navigation.PushAsync(new MusicLibraryPage());
	}

	private void Button_Clicked_1(object sender, EventArgs e)
	{
		Navigation.PushAsync(new ObservableCollectionPage());
	}

	private void Button_Clicked_2(object sender, EventArgs e)
	{
		Navigation.PushAsync(new SectionedAdapterPage());
	}
	
	private void Button_Clicked_3(object sender, EventArgs e)
	{
		Navigation.PushAsync(new ComplexScrollingPage());
	}

	private void Button_Clicked_4(object sender, EventArgs e)
	{
		Navigation.PushAsync(new BindableSelectedItemPage());
	}
}
