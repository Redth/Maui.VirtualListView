using Foundation;
using UIKit;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualListViewSample
{
	[Register("SceneDelegate")]
	internal class SceneDelegate : MauiUISceneDelegate
	{
		[Export("scene:willConnectToSession:options:")]
		public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
		{
			var windowScene = scene as UIWindowScene;

			var titlebar = windowScene.Titlebar;
			titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
			titlebar.Toolbar = null;

			base.WillConnect(scene, session, connectionOptions);
		}
	}
}