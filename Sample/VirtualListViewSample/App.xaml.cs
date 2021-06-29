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
		public App() : base()
		{
			InitializeComponent();
		}

		protected override Window CreateWindow(IActivationState activationState)
		{
			return new Microsoft.Maui.Controls.Window(new MainPage());
		}
	}
}
