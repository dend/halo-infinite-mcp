using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.Forerunner.MCP.Core;
using Serilog;

namespace OpenSpartan.Forerunner.MCP.Resources
{
    public class GetLocalImageResource() : IResourceProvider
    {
        public IEnumerable<Resource> GetResourceDefinitions()
        {
            yield return new Resource
            {
                Uri = "opsp://resources/localimage",
                Name = "opsp_res_local_image",
                Description = "Allows accessing local images that were cached from the Halo Infinite API.",
                MimeType = "image/png"
            };
        }

        public IEnumerable<ResourceTemplate> GetResourceTemplates()
        {
            yield return new ResourceTemplate
            {
                UriTemplate = "opsp://resources/localimage/{image_path}",
                Name = "opsp_res_local_image_by_path"
            };
        }

        public bool CanHandleUri(string uri)
        {
            return uri?.StartsWith("opsp://resources/localimage", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task<ResourceContents> GetResourceContentsAsync(string uri, CancellationToken cancellationToken)
        {
            if (uri.StartsWith("opsp://resources/localimage"))
            {
                var plainPath = uri.Replace("opsp://resources/localimage/", "");

                var imagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", plainPath);

                Log.Logger.Information($"Testing image path: {imagePath}...");

                if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                {
                    byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath, cancellationToken);

                    using (var inputStream = new MemoryStream(imageBytes))
                    using (var outputStream = new MemoryStream())
                    {
                        // Load the original bitmap
                        using (var original = SkiaSharp.SKBitmap.Decode(inputStream))
                        {
                            var imageInfo = new SkiaSharp.SKImageInfo(128, 128);

                            // Use the newer sampling options API instead of the obsolete SKFilterQuality
                            var samplingOptions = new SkiaSharp.SKSamplingOptions(
                                SkiaSharp.SKCubicResampler.Mitchell); // High quality resampling

                            using (var resized = original.Resize(imageInfo, samplingOptions))
                            using (var image = SkiaSharp.SKImage.FromBitmap(resized))
                            using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                            {
                                // Save to memory stream
                                data.SaveTo(outputStream);
                            }
                        }

                        // Convert to base64
                        outputStream.Position = 0;
                        string base64Image = Convert.ToBase64String(outputStream.ToArray());

                        return new ResourceContents
                        {
                            Uri = uri,
                            MimeType = "image/png",
                            Blob = base64Image,
                        };
                    }
                }
                else
                {
                    return new ResourceContents
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = "{\"error\":true,\"message\":\"Image resource could not be obtained\",\"code\":\"IMAGE_UNAVAILABLE\"}"
                    };
                }
            }

            throw new McpServerException($"Resource not found: {uri}");
        }
    }
}
