using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShapeRecognizer
{
	public static class CustomVisionEngine
	{
		private const string ProjectName = "Shape Recognizer";
		private const string ProjectDescription = "A simple shape recognition app.";

		private const string TrainingKey = "baf1696044e44308915d432214e8c2f9";
		private const string TrainingEndpoint = "https://northeurope.api.cognitive.microsoft.com";
		private const string TrainingResourceId = "/subscriptions/70cfe53f-cf82-4986-ac7b-74c1a6ae9c28/resourceGroups/shape-recognizer/providers/Microsoft.CognitiveServices/accounts/shape-recognizer";
		private const string PredictionKey = "3aefcb5c97cf4f35a6c028ee1324a8ac";
		private const string PredictionEndpoint = "https://northeurope.api.cognitive.microsoft.com";
		private const string PredictionResourceId = "/subscriptions/70cfe53f-cf82-4986-ac7b-74c1a6ae9c28/resourceGroups/shape-recognizer/providers/Microsoft.CognitiveServices/accounts/shape-recognizer_prediction";

		private static CustomVisionTrainingClient trainingApi;
		private static CustomVisionPredictionClient predictionApi;
		private static Project project;
		private static string modelName;

		private static Task initializationTask;

		public static bool IsInitialized => initializationTask.IsCompleted;

		public static int RequiredImageCount => 5;

		public static Task InitializeAsync() =>
			initializationTask ?? (initializationTask = CreateInitializationTask());

		public static async Task<string> DetectShapeAsync(SKImage drawing)
		{
			await InitializeAsync();

			// we can't detect any shapes
			if (drawing == null || predictionApi == null || modelName == null)
				return null;

			// detect the shape
			using (var data = drawing.Encode(SKEncodedImageFormat.Png, 100))
			using (var stream = data.AsStream())
			{
				var classification = await predictionApi.ClassifyImageAsync(project.Id, modelName, stream);
				var predictions = classification.Predictions.OrderBy(p => p.Probability);
				var best = predictions.LastOrDefault();
				if (best != null)
					return best.TagName;
			}

			return null;
		}

		public static async Task TrainAsync(string shape, IEnumerable<SKImage> drawings)
		{
			await InitializeAsync();

			if (project == null)
				return;

			// load or create the tag
			var tags = await trainingApi.GetTagsAsync(project.Id);
			var tag = tags.FirstOrDefault(t => t.Name.Equals(shape, StringComparison.OrdinalIgnoreCase));
			if (tag == null)
				tag = await trainingApi.CreateTagAsync(project.Id, shape);

			// upload the images with their tags
			foreach (var drawing in drawings)
			{
				using (var data = drawing.Encode(SKEncodedImageFormat.Png, 100))
				using (var stream = data.AsStream())
				{
					var image = await trainingApi.CreateImagesFromDataAsync(project.Id, stream, new[] { tag.Id });
				}
			}

			// unpublish and delete the old models
			var iterations = await trainingApi.GetIterationsAsync(project.Id);
			var allButLast = iterations.OrderBy(i => i.LastModified).Take(iterations.Count - 1);
			foreach (var iter in allButLast)
			{
				if (!string.IsNullOrEmpty(iter.PublishName))
					await trainingApi.UnpublishIterationAsync(project.Id, iter.Id);
				await trainingApi.DeleteIterationAsync(project.Id, iter.Id);
			}

			// train the model with the new images
			var iteration = await trainingApi.TrainProjectAsync(project.Id);
			while (iteration.Status.Equals("Training", StringComparison.OrdinalIgnoreCase))
			{
				await Task.Delay(1000);
				iteration = await trainingApi.GetIterationAsync(project.Id, iteration.Id);
			}

			// publish the trained model
			await trainingApi.PublishIterationAsync(project.Id, iteration.Id, iteration.Id.ToString(), PredictionResourceId);

			modelName = iteration.Id.ToString();
		}

		private static async Task CreateInitializationTask()
		{
			// create the training API
			trainingApi = new CustomVisionTrainingClient
			{
				ApiKey = TrainingKey,
				Endpoint = TrainingEndpoint
			};

			// check to see if the project exists
			var projects = await trainingApi.GetProjectsAsync();
			project = projects.FirstOrDefault(p => p.Name == ProjectName);

			// create a new project if it does not exist
			if (project == null)
			{
				project = await trainingApi.CreateProjectAsync(
					ProjectName,
					ProjectDescription,
					classificationType: Classifier.Multiclass);
			}

			// get the latest iteration
			var iterations = await trainingApi.GetIterationsAsync(project.Id);
			var iteration = iterations
				.Where(i => !string.IsNullOrEmpty(i.PublishName))
				.OrderBy(i => i.Created)
				.LastOrDefault();
			modelName = iteration?.PublishName;

			// create the prediction API
			predictionApi = new CustomVisionPredictionClient
			{
				ApiKey = PredictionKey,
				Endpoint = PredictionEndpoint
			};
		}
	}
}
