using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace NextGenCompany
{
    public static class ResizerHttpTrigger
    {
        [FunctionName("ResizerHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (!int.TryParse(req.Query["w"], out int w))
            {
                return new BadRequestObjectResult("Please pass a width on the query string");
            }
            if (!int.TryParse(req.Query["h"], out int h))
            {
                return new BadRequestObjectResult("Please pass a height on the query string");
            }

            byte[] targetImageBytes;
            using (var msInput = new MemoryStream())
            {
                // Récupère le corps du message en mémoire
                try
                {
                    await req.Body.CopyToAsync(msInput);
                }
                catch (Exception e) when (e is ArgumentNullException || e is ObjectDisposedException || e is NotSupportedException)
                {
                    return new BadRequestObjectResult("Please pass a valid image in the request body");
                }
                msInput.Position = 0;

                // Charge l'image
                try
                {
                    using (var image = Image.Load(msInput))
                    {
                        // Effectue la transformation
                        try
                        {

                            image.Mutate(x => x.Resize(w, h));
                        }
                        catch (Exception e) when (e is ArgumentNullException || e is ObjectDisposedException || e is ImageProcessingException)
                        {
                            return new BadRequestObjectResult("Could not resize the image");
                        }


                        // Sauvegarde en mémoire               
                        using (var msOutput = new MemoryStream())
                        {
                            try
                            {
                                image.SaveAsJpeg(msOutput);
                            }
                            catch (ArgumentNullException)
                            {
                                return new BadRequestObjectResult("Could not save the image");
                            }
                            targetImageBytes = msOutput.ToArray();
                        }
                    }
                }
                catch (Exception e) when (e is ArgumentNullException || e is NotSupportedException || e is InvalidImageContentException || e is UnknownImageFormatException)
                {
                    return new BadRequestObjectResult("Could not process the image: " + e.Message);
                }
            }
            return new FileContentResult(targetImageBytes, "image/jpeg");
        }

    }
}
