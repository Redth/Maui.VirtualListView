using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;

namespace VirtualListViewSample
{
	[Application(Theme = "@style/AppTheme", SupportsRtl = true, AllowBackup = true)]
	public class MainApplication : MauiApplication<Startup>
	{
		public MainApplication(IntPtr handle, JniHandleOwnership ownerShip) : base(handle, ownerShip)
		{
		}
	}
}