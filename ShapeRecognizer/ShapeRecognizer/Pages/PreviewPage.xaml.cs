using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ShapeRecognizer
{
	public partial class PreviewPage : ContentPage
	{
		public PreviewPage(IEnumerable<SKImage> drawings)
		{
			InitializeComponent();

			BindingContext = new TrainingViewModel(drawings);
		}
	}
}
