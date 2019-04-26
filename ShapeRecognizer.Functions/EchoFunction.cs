using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ShapeRecognizer.Functions
{
	public static class EchoFunction
	{
		[FunctionName("Echo")]
		public static IActionResult Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
			ILogger log)
		{
			log.LogInformation("Echoing something...");

			var text = (string)req.Query["text"];

			return string.IsNullOrWhiteSpace(text)
				? new BadRequestObjectResult("Please shout something to me using the 'text' query string parameter.")
				: (ActionResult)new OkObjectResult($"{text}, {text}, {text}, ...");
		}
	}
}
