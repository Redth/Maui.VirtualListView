namespace VirtualListViewSample;

public partial class MusicLibraryPage : ContentPage
{
	MainViewModel vm;

	public MusicLibraryPage()
	{
		InitializeComponent();

		Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page.SetUseSafeArea(this, false);

		vm = new MainViewModel();
		BindingContext = vm;

		Task.Delay(2000).ContinueWith(t =>
		{
			Dispatcher.Dispatch(() =>
			{
				vlv.SelectItems(new ItemPosition(0, 2), new ItemPosition(0, 4));
			});
		});
	}

	void VirtualListView_SelectedItemsChanged(System.Object sender, SelectedItemsChangedEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"Selected Items:"
			+ string.Join(", ", e.NewSelection.Select(s => $"{s.SectionIndex}:{s.ItemIndex}")));
	}
}