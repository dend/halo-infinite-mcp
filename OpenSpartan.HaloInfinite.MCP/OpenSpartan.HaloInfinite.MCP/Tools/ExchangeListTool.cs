using Den.Dev.Grunt.Models;
using Den.Dev.Grunt.Models.HaloInfinite;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;
using OpenSpartan.HaloInfinite.MCP.Models;
using Serilog;
using System.Text.Json;

namespace OpenSpartan.HaloInfinite.MCP.Tools
{
    public class ExchangeListTool : ITool
    {
        public string Name => "opsp_exchange_list";
        public string Description => "Lists all of the items that are currently available on the Halo Infinite exchange.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            var exchangeItems = new List<ItemMetadataContainer>();

            var exchangeOfferings = await HaloInfiniteAPIBridge.SafeAPICall(async () =>
            {
                return await HaloInfiniteAPIBridge.HaloClient.EconomyGetSoftCurrencyStore($"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})");
            });

            if (exchangeOfferings != null && exchangeOfferings.Result != null)
            {
                exchangeItems = await ProcessExchangeItems(exchangeOfferings.Result);
            }
            else
            {
                Log.Logger.Error($"Exchange offerings were not obtained.");
            }

            if (exchangeItems != null && exchangeItems.Count > 0)
            {
                var contentItems = new List<Content>();

                foreach (var item in exchangeItems)
                {
                    // Add JSON representation of the item
                    contentItems.Add(new Content()
                    {
                        Text = JsonSerializer.Serialize(item),
                        Type = "text",
                        MimeType = "application/json"
                    });

                    var imagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", item.ImagePath);

                    Log.Logger.Information($"Testing image path: {imagePath}...");

                    // Add image if the path exists
                    if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                    {
                        Log.Logger.Information($"{imagePath} exists.");

                        try
                        {
                            // Load the image
                            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath, cancellationToken);

                            // Use SkiaSharp to resize (cross-platform)
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

                                contentItems.Add(new Content()
                                {
                                    Type = "resource",
                                    Resource = new ResourceContents()
                                    {
                                        Blob = base64Image,
                                        MimeType = "image/png",
                                        Uri = $"opsp://resources/localimage/{item.ImagePath}",
                                    }
                                });

                                //contentItems.Add(new Content()
                                //{
                                //    Type = "image",
                                //    MimeType = "image/png",
                                //    Data = base64Image
                                //});
                            }
                        }
                        catch (Exception ex)
                        {
                            contentItems.Add(new Content()
                            {
                                Text = $"Failed to load or resize image for item: {ex.Message}",
                                Type = "text"
                            });
                        }
                    }
                }

                return new CallToolResponse()
                {
                    Content = contentItems
                };
            }
            else
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = "Exchange items could not be obtained.", Type = "text" }]
                };
            }
        }

        private static async Task<List<ItemMetadataContainer>> ProcessExchangeItems(StoreItem exchangeStoreItem)
        {
            var exchangeItems = new List<ItemMetadataContainer>();

            foreach (var offering in exchangeStoreItem.Offerings)
            {
                // We're only interested in offerings that have items attached to them.
                // Other items are not relevant, and we can skip them (there are no currency
                // or seasonal offers attached to Exchange items.
                if (offering != null && offering.IncludedItems.Any())
                {
                    // Current Exchange offering can contain more items in one (e.g., logos)
                    // but ultimately maps to just one item.
                    var item = offering.IncludedItems.FirstOrDefault();

                    if (item != null)
                    {
                        var itemMetadata = await HaloInfiniteAPIBridge.SafeAPICall(async () =>
                        {
                            return await HaloInfiniteAPIBridge.HaloClient.GameCmsGetItem(item.ItemPath, HaloInfiniteAPIBridge.HaloClient.ClearanceToken);
                        });

                        if (itemMetadata != null && itemMetadata.Result != null)
                        {
                            string folderPath = !string.IsNullOrWhiteSpace(itemMetadata.Result.CommonData.DisplayPath.FolderPath) ? itemMetadata.Result.CommonData.DisplayPath.FolderPath : itemMetadata.Result.CommonData.DisplayPath.Media.FolderPath;
                            string fileName = !string.IsNullOrWhiteSpace(itemMetadata.Result.CommonData.DisplayPath.FileName) ? itemMetadata.Result.CommonData.DisplayPath.FileName : itemMetadata.Result.CommonData.DisplayPath.Media.FileName;

                            var metadataContainer = new ItemMetadataContainer
                            {
                                ItemType = item.ItemType,
                                // There is usually just one price, since it's just one offering. There may be
                                // several included items (e.g., shoulder pads) but the price should still be the
                                // same regardless, at least from the current Exchange implementation.
                                // If for some reason there is no price assigned, we will default to -1.
                                ItemValue = (offering.Prices != null && offering.Prices.Any()) ? offering.Prices[0].Cost : -1,
                                ImagePath = (!string.IsNullOrWhiteSpace(folderPath) && !string.IsNullOrWhiteSpace(fileName)) ? Path.Combine(folderPath, fileName).Replace("\\", "/") : itemMetadata.Result.CommonData.DisplayPath.Media.MediaUrl.Path,
                                ItemDetails = new InGameItem()
                                {
                                    CommonData = itemMetadata.Result.CommonData,
                                },
                            };

                            // There is a chance that the image lookup is going to fail. In that case, we want to
                            // fallback to the "dumb" logic, and that is - get the offering and all the related metadata.
                            if (string.IsNullOrWhiteSpace(metadataContainer.ImagePath))
                            {
                                if (offering.OfferingDisplayPath != null)
                                {
                                    var offeringData = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.GameCmsGetStoreOffering(offering.OfferingDisplayPath));
                                    if (offeringData != null && offeringData.Result != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(offeringData.Result.ObjectImagePath))
                                        {
                                            metadataContainer.ImagePath = offeringData.Result.ObjectImagePath.Replace("\\", "/");
                                        }
                                    }
                                }
                            }

                            if (Path.IsPathRooted(metadataContainer.ImagePath))
                            {
                                metadataContainer.ImagePath = metadataContainer.ImagePath.TrimStart(Path.DirectorySeparatorChar);
                                metadataContainer.ImagePath = metadataContainer.ImagePath.TrimStart(Path.AltDirectorySeparatorChar);
                            }

                            string qualifiedItemImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", metadataContainer.ImagePath);

                            FileSystemHelpers.EnsureDirectoryExists(qualifiedItemImagePath);

                            await DownloadAndSetImage(metadataContainer.ImagePath, qualifiedItemImagePath);

                            exchangeItems.Add(metadataContainer);

                            Log.Logger.Information($"Got item for Exchange listing: {item.ItemPath}");
                        }
                        else
                        {
                            Log.Logger.Information($"Failed to obtain exchange item: {item.ItemPath}");
                        }
                    }
                }
            }

            return exchangeItems;
        }

        private static async Task DownloadAndSetImage(string serviceImagePath, string localImagePath, bool isOnWaypoint = false)
        {
            try
            {
                // Check if local image file exists
                if (System.IO.File.Exists(localImagePath))
                {
                    return;
                }

                HaloApiResultContainer<byte[], RawResponseContainer>? image = null;

                Func<Task<HaloApiResultContainer<byte[], RawResponseContainer>>> apiCall = isOnWaypoint ?
                    async () => await HaloInfiniteAPIBridge.HaloClient.GameCmsGetGenericWaypointFile(serviceImagePath) :
                    async () => await HaloInfiniteAPIBridge.HaloClient.GameCmsGetImage(serviceImagePath);

                image = await HaloInfiniteAPIBridge.SafeAPICall(apiCall);

                // Check if the image retrieval was successful
                if (image != null && image.Result != null && image.Response.Code == 200)
                {
                    // In case the folder does not exist, make sure we create it.
                    FileInfo file = new(localImagePath);
                    file.Directory.Create();

                    await System.IO.File.WriteAllBytesAsync(localImagePath, image.Result);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Failed to download and set image '{serviceImagePath}' to '{localImagePath}'. Error: {ex.Message}");
            }
        }
    }
}
