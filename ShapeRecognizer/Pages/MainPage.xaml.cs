using SkiaSharp;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	[DesignTimeVisible(true)]
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			MessagingCenter.Subscribe<DrawingViewModel, IEnumerable<SKImage>>(this, App.RequestTrainingMessage, OnTrainingRequested);
		}

		protected override void OnDisappearing()
		{
			MessagingCenter.Unsubscribe<DrawingViewModel, IEnumerable<SKImage>>(this, App.RequestTrainingMessage);

			base.OnDisappearing();
		}

		private void OnTrainingRequested(DrawingViewModel sender, IEnumerable<SKImage> args)
		{
			Navigation.PushAsync(new PreviewPage(args));
		}
	}
}
