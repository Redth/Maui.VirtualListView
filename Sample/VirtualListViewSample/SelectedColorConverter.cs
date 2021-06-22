using System;
using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace VirtualListViewSample
{
	public class SelectedColorConverter : IValueConverter
	{
		static readonly Color SelectedColor = Color.FromHex("#efefef");
		static readonly Color UnselectedColor = Color.White;

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
