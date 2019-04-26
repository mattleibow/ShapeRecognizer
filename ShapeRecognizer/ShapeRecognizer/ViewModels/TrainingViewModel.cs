using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class TrainingViewModel : BindableObject
	{
		private string shapeName;
		private string statusMessage;

		public TrainingViewModel(IEnumerable<SKImage> drawings)
		{
			Drawings = new ObservableCollection<SKImage>(drawings);
			TrainCommand = new Command(OnTrain);
		}

		public ICommand TrainCommand { get; }

		public ObservableCollection<SKImage> Drawings { get; }

		public string ShapeName
		{
			get { return shapeName; }
			set
			{
				shapeName = value;
				OnPropertyChanged();
			}
		}

		public string StatusMessage
		{
			get { return statusMessage; }
			set
			{
				statusMessage = value;
				OnPropertyChanged();
			}
		}

		private async void OnTrain()
		{
			if (string.IsNullOrWhiteSpace(ShapeName))
			{
				StatusMessage = "The shape needs to have a non-whitespace name.";
				return;
			}

			if (Drawings == null || Drawings.Count < CustomVisionEngine.RequiredImageCount)
			{
				StatusMessage = $"There needs to be at least {CustomVisionEngine.RequiredImageCount} drawings.";
				return;
			}

			StatusMessage = "Initiating training...";
			try
			{
				await CustomVisionEngine.TrainAsync(ShapeName, Drawings);
				StatusMessage = "Training complete.";
			}
			catch (CustomVisionErrorException ex)
			{
				StatusMessage = "There was an error during the training: " + ex.Body.Message;
			}
			catch (Exception ex)
			{
				StatusMessage = "There was an error during the training: " + ex.Message;
			}
		}
	}
}
