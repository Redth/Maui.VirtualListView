using System;
using System.IO;
using System.Reflection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public partial class App : Microsoft.Maui.Controls.Application
	{
		public App()
		{
			InitializeComponent();
		}

		protected override IWindow CreateWindow(IActivationState activationState)
		{
			return new Microsoft.Maui.Controls.Window(new MainPage());
		}
	}
}
