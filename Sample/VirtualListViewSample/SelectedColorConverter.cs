using System;
using System.Globalization;
using Xamarin.Forms;

namespace VirtualListViewSample
{
	public class SelectedColorConverter : IValueConverter
	{
		static readonly Color SelectedColor = Color.DarkBlue;
		static readonly Color UnselectedColor = Color.Transparent;

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
