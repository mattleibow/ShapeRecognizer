using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class ImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SKImage image)
				return (SKImageImageSource)image;
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
