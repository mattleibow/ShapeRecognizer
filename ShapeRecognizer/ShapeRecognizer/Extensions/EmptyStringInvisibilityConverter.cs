using System;
using System.Globalization;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class EmptyStringInvisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
			!string.IsNullOrEmpty(value as string);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
