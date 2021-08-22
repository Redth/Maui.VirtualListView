using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;

namespace VirtualListViewSample
{
	public class SelectedColorConverter : IValueConverter
	{
		static readonly Color SelectedColor = Color.FromArgb("#efefef");
		static readonly Color UnselectedColor = Colors.White;

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
}
