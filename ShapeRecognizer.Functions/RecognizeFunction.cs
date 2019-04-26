using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShapeRecognizer.Functions
{
	public static class RecognizeFunction
	{
		[FunctionName("Recognize")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Processing an shape recognition request...");

			var image = await ReadStreamAsync(req.Body, log) ?? await ReadQueryStringAsync(req, log);

			if (image == null)
				return new BadRequestObjectResult("Please pass me an encoded image in the body or a link to an image using the 'uri' query string parameter. The image must be encoded with JPEG, PNG, WEBP or GIF codecs.");

			var shape = await CustomVisionEngine.DetectShapeAsync(image);

			if (string.IsNullOrEmpty(shape))
				return new NotFoundObjectResult("The image does not appear to be something that I recognize.");

			return new OkObjectResult($"The image appears to be a {shape}.");
		}

		private static async Task<SKImage> ReadQueryStringAsync(HttpRequest req, ILogger log)
		{
			try
			{
				var uri = (string)req.Query["uri"] ?? (string)req.Query["url"];
				if (string.IsNullOrEmpty(uri))
					return null;

				var client = new HttpClient();
				var stream = await client.GetStreamAsync(uri);

				return await ReadStreamAsync(stream, log);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Recieved an exception while loading the file located at the URI.");

				return null;
			}
		}

		private static async Task<SKImage> ReadStreamAsync(Stream source, ILogger log)
		{
			try
			{
				// copy the data - 'cause we can
				var stream = new MemoryStream();
				await source?.CopyToAsync(stream);

				// make sure we are ready to read
				await stream.FlushAsync();
				stream.Position = 0;

				// there was nothing found
				if (stream.Length == 0)
					return null;

				// read the stream into a new image object
				return SKImage.FromEncodedData(stream);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Recieved an exception while reading the image stream.");

				return null;
			}
		}
	}
}
