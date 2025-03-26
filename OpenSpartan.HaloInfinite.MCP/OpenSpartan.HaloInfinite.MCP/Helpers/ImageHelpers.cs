using Serilog;
using SkiaSharp;

namespace OpenSpartan.HaloInfinite.MCP.Helpers
{
    public static class ImageHelpers
    {
        /// <summary>
        /// Resizes an image from a file path and returns it as a base64 encoded string
        /// </summary>
        /// <param name="imagePath">Path to the source image</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <param name="quality">Output quality (1-100)</param>
        /// <param name="format">Output image format</param>
        /// <returns>Base64 encoded string of the resized image.</returns>
        public static async Task<string> ResizeImageToBase64(
            string imagePath,
            int width,
            int height,
            int quality = 100,
            SKEncodedImageFormat format = SKEncodedImageFormat.Png)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    Log.Logger.Warning($"Image not found: {imagePath}");
                    return null;
                }

                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                return ResizeImageBytesToBase64(imageBytes, width, height, quality, format);
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to resize image from path {imagePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resizes an image from a byte array and returns it as a base64 encoded string
        /// </summary>
        /// <param name="imageBytes">Source image as byte array</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <param name="quality">Output quality (1-100)</param>
        /// <param name="format">Output image format</param>
        /// <returns>Base64 encoded string of the resized image.</returns>
        public static string ResizeImageBytesToBase64(
            byte[] imageBytes,
            int width,
            int height,
            int quality = 100,
            SKEncodedImageFormat format = SKEncodedImageFormat.Png)
        {
            try
            {
                using (var inputStream = new MemoryStream(imageBytes))
                using (var outputStream = new MemoryStream())
                {
                    using (var original = SKBitmap.Decode(inputStream))
                    {
                        if (width <= 0 && height > 0)
                        {
                            width = (int)(height * ((float)original.Width / original.Height));
                        }
                        else if (height <= 0 && width > 0)
                        {
                            height = (int)(width * ((float)original.Height / original.Width));
                        }
                        else if (width <= 0 && height <= 0)
                        {
                            width = original.Width;
                            height = original.Height;
                        }

                        var imageInfo = new SKImageInfo(width, height);

                        var samplingOptions = new SKSamplingOptions(SKCubicResampler.Mitchell);

                        using var resized = original.Resize(imageInfo, samplingOptions);
                        using var image = SKImage.FromBitmap(resized);
                        using var data = image.Encode(format, quality);
                        data.SaveTo(outputStream);
                    }

                    return Convert.ToBase64String(outputStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to resize image bytes: {ex.Message}");
                return null;
            }
        }
    }
}