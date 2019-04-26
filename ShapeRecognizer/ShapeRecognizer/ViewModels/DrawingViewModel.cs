using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace ShapeRecognizer
{
	public class DrawingViewModel : BindableObject
	{
		private readonly List<SKImage> drawings = new List<SKImage>();
		private bool isTraining;
		private string detectedShape;
		private string statusMessage;
		private bool hasStatusButtons;
		private SKImage currentDrawing;

		public DrawingViewModel()
		{
			AddDrawingCommand = new Command(OnAddDrawing);
			PreviewTrainingCommand = new Command(OnPreviewTraining);
			CancelTrainingCommand = new Command(OnCancelTraining);
			StartTrainingCommand = new Command(OnStartTraining);
			DismissStatusCommand = new Command(OnDismissStatus);
			ClearCommand = new Command(OnClear);

			_ = InitializeEngineAsync();
		}

		public ICommand AddDrawingCommand { get; }

		public ICommand PreviewTrainingCommand { get; }

		public ICommand CancelTrainingCommand { get; }

		public ICommand StartTrainingCommand { get; }

		public ICommand DismissStatusCommand { get; }

		public ICommand ClearCommand { get; }

		public bool IsTraining
		{
			get { return isTraining; }
			set
			{
				isTraining = value;
				OnPropertyChanged();
			}
		}

		public string DetectedShape
		{
			get { return detectedShape; }
			set
			{
				detectedShape = value;
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

		public bool HasStatusButtons
		{
			get { return hasStatusButtons; }
			set
			{
				hasStatusButtons = value;
				OnPropertyChanged();
			}
		}

		public SKImage CurrentDrawing
		{
			get { return currentDrawing; }
			set
			{
				currentDrawing = value;
				OnPropertyChanged();

				_ = DetectShapeAsync();
			}
		}

		private void OnClear()
		{
			CurrentDrawing = null;

			if (IsTraining)
				OnAddDrawing();
			else
				OnCancelTraining();
		}

		private void OnStartTraining()
		{
			IsTraining = true;
			DetectedShape = $"Draw the shape and add it to the dataset. After at least {CustomVisionEngine.RequiredImageCount} drawings, you can train the model.";
			OnAddDrawing();
		}

		private void OnCancelTraining()
		{
			IsTraining = false;
			StatusMessage = null;
			DetectedShape = "Draw a shape below.";
			CurrentDrawing = null;
			drawings.Clear();
		}

		private void OnAddDrawing()
		{
			if (CurrentDrawing != null)
				drawings.Add(CurrentDrawing);
			CurrentDrawing = null;

			if (drawings.Count < CustomVisionEngine.RequiredImageCount)
				StatusMessage = $"You need at least {CustomVisionEngine.RequiredImageCount} images. You have {drawings.Count} so far.";
			else
				StatusMessage = $"You have {drawings.Count} images.";
			HasStatusButtons = false;
		}

		private void OnPreviewTraining()
		{
			if (drawings.Count < CustomVisionEngine.RequiredImageCount)
			{
				StatusMessage = $"At least {CustomVisionEngine.RequiredImageCount} images are required to train a new shape.";
				HasStatusButtons = false;
				return;
			}

			MessagingCenter.Send(this, App.RequestTrainingMessage, (IEnumerable<SKImage>)drawings);
		}

		private void OnDismissStatus() =>
			StatusMessage = null;

		private async Task InitializeEngineAsync()
		{
			StatusMessage = "Initializing recognition engine...";
			HasStatusButtons = false;

			DetectedShape = "Draw a shape below.";
			try
			{
				await CustomVisionEngine.InitializeAsync();

				StatusMessage = null;
			}
			catch (Exception ex)
			{
				StatusMessage = "There was an error during the initialization: " + ex.Message;
				HasStatusButtons = false;
			}
		}

		private async Task DetectShapeAsync()
		{
			if (isTraining)
				return;

			if (!CustomVisionEngine.IsInitialized)
				return;

			if (CurrentDrawing == null)
				return;

			StatusMessage = "Trying to recognize the shape...";
			HasStatusButtons = false;
			try
			{
				var shape = await CustomVisionEngine.DetectShapeAsync(CurrentDrawing);
				if (string.IsNullOrEmpty(shape))
				{
					DetectedShape = "The shape was not recognized.";

					StatusMessage = "Do you want to add this shape to the model?";
					HasStatusButtons = true;
				}
				else
				{
					DetectedShape = $"The shape was recognized as a {shape}.";

					StatusMessage = "Do you want to update the model?";
					HasStatusButtons = true;
				}
			}
			catch (CustomVisionErrorException ex)
			{
				StatusMessage = "There was an error during the recognition: " + ex.Body.Message;
			}
			catch (Exception ex)
			{
				StatusMessage = "There was an error during the recognition: " + ex.Message;
				HasStatusButtons = false;
			}
		}
	}
}
