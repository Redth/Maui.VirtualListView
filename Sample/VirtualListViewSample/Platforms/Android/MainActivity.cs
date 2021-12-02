using Android.App;
using Microsoft.Maui;
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace VirtualListViewSample
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true)]
	public class MainActivity : MauiAppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			Platform.Init(this, savedInstanceState);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
	}
}