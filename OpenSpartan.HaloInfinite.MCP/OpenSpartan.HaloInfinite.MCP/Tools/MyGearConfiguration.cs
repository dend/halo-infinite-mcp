using Den.Dev.Grunt.Models.HaloInfinite;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.Forerunner.MCP.Core;
using OpenSpartan.Forerunner.MCP.Helpers;
using Serilog;
using System.Text.Json;

namespace OpenSpartan.Forerunner.MCP.Tools
{
    public class MyGearConfiguration : ITool
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private const int ThumbnailSize = 128;

        public string Name => "opsp_my_gear_configuration";
        public string Description => "Returns Halo Infinite customizations with their images for the authenticated user.";

        public JsonElement InputSchema => JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object"
        }
        """);

        public async Task<CallToolResponse> ExecuteAsync(Dictionary<string, object> arguments, IMcpServer server, CancellationToken cancellationToken)
        {
            var customizations = await HaloInfiniteAPIBridge.SafeAPICall(async () =>
                await HaloInfiniteAPIBridge.HaloClient.EconomyGetMultiplePlayersCustomization([$"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})"]));

            if (customizations == null)
            {
                return new CallToolResponse()
                {
                    Content = [new Content() { Text = "No API endpoints could be obtained.", Type = "text" }]
                };
            }

            var contentItems = new List<Content>
            {
                new()
                {
                    Text = customizations.Response.Message,
                    Type = "text",
                    MimeType = "application/json"
                }
            };

            string jsonCacheDir = Path.Combine(Configuration.AppDataDirectory, "jsoncache");
            string imageCacheDir = Path.Combine(Configuration.AppDataDirectory, "imagecache");
            FileSystemHelpers.EnsureDirectoryExists(jsonCacheDir);
            FileSystemHelpers.EnsureDirectoryExists(imageCacheDir);

            var jsonPaths = ExtractJsonPaths(customizations.Response.Message);

            foreach (var jsonPath in jsonPaths)
            {
                try
                {
                    string normalizedPath = jsonPath.TrimStart('/');
                    string cachedJsonPath = Path.Combine(jsonCacheDir, normalizedPath);

                    FileSystemHelpers.EnsureDirectoryExists(Path.GetDirectoryName(cachedJsonPath));

                    InGameItem itemResult;

                    if (System.IO.File.Exists(cachedJsonPath))
                    {
                        try
                        {
                            string cachedJson = await System.IO.File.ReadAllTextAsync(cachedJsonPath, cancellationToken);
                            itemResult = JsonSerializer.Deserialize<InGameItem>(cachedJson);
                            Log.Logger.Debug($"Loaded item from cache: {jsonPath}");
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Warning($"Failed to read {jsonPath} from cache: {ex.Message}. Fetching from API.");
                            var item = await HaloInfiniteAPIBridge.SafeAPICall(async () =>
                                await HaloInfiniteAPIBridge.HaloClient.GameCmsGetProgressionFile<InGameItem>(jsonPath));

                            if (item.Result == null)
                            {
                                Log.Logger.Error($"API returned null for {jsonPath}");
                                continue;
                            }

                            itemResult = item.Result;

                            await System.IO.File.WriteAllTextAsync(cachedJsonPath, JsonSerializer.Serialize(itemResult, _jsonOptions), cancellationToken);
                        }
                    }
                    else
                    {
                        var item = await HaloInfiniteAPIBridge.SafeAPICall(async () =>
                            await HaloInfiniteAPIBridge.HaloClient.GameCmsGetProgressionFile<InGameItem>(jsonPath));

                        if (item.Result == null)
                        {
                            Log.Logger.Error($"API returned null for {jsonPath}");
                            continue;
                        }

                        itemResult = item.Result;

                        await System.IO.File.WriteAllTextAsync(cachedJsonPath, JsonSerializer.Serialize(itemResult, _jsonOptions), cancellationToken);
                        Log.Logger.Debug($"Cached item: {jsonPath}");
                    }

                    if (itemResult?.CommonData?.DisplayPath?.Media?.MediaUrl?.Path != null)
                    {
                        var imageServicePath = itemResult.CommonData.DisplayPath.Media.MediaUrl.Path;
                        imageServicePath = imageServicePath.TrimStart('/');

                        string qualifiedItemImagePath = Path.Combine(imageCacheDir, imageServicePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(qualifiedItemImagePath));

                        try
                        {
                            if (!System.IO.File.Exists(qualifiedItemImagePath))
                            {
                                await HaloInfiniteAPIBridge.DownloadHaloAPIImage(imageServicePath, qualifiedItemImagePath);
                                Log.Logger.Debug($"Downloaded image: {imageServicePath}");
                            }
                            else
                            {
                                Log.Logger.Debug($"Using cached image: {imageServicePath}");
                            }

                            string base64Image = await ImageHelpers.ResizeImageToBase64(qualifiedItemImagePath, ThumbnailSize, ThumbnailSize);

                            // Remove translations and other unnecessary data because
                            // they clutter the returned results.
                            itemResult.CommonData.Title.Translations = null;
                            itemResult.CommonData.Description.Translations = null;
                            itemResult.CommonData.AltName.Translations = null;
                            itemResult.CommonData.ParentPaths = null;

                            contentItems.Add(new Content()
                            {
                                Text = JsonSerializer.Serialize(new
                                {
                                    Path = jsonPath,
                                    Item = itemResult.CommonData,
                                }, _jsonOptions),
                                Type = "text",
                                MimeType = "application/json"
                            });

                            Log.Logger.Information($"IMAGE_TEST: {base64Image}");

                            if (!string.IsNullOrWhiteSpace(base64Image))
                            {
                                contentItems.Add(new Content()
                                {
                                    Type = "image",
                                    MimeType = "image/png",
                                    Data = base64Image
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error($"Could not process image: {imageServicePath} to {qualifiedItemImagePath}. {ex.Message}");
                            contentItems.Add(new Content()
                            {
                                Text = $"Failed to process image for {jsonPath}: {ex.Message}",
                                Type = "text"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    contentItems.Add(new Content()
                    {
                        Text = $"Error processing {jsonPath}: {ex.Message}",
                        Type = "text"
                    });
                }

                if (cancellationToken.IsCancellationRequested) break;
            }

            return new CallToolResponse()
            {
                Content = contentItems
            };
        }

        private List<string> ExtractJsonPaths(string jsonString)
        {
            var jsonPaths = new HashSet<string>();
            var jsonDocument = JsonDocument.Parse(jsonString);

            ExtractJsonPathsRecursive(jsonDocument.RootElement, jsonPaths);

            return [.. jsonPaths];
        }

        private void ExtractJsonPathsRecursive(JsonElement element, HashSet<string> jsonPaths)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        ExtractJsonPathsRecursive(property.Value, jsonPaths);
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        ExtractJsonPathsRecursive(item, jsonPaths);
                    }
                    break;

                case JsonValueKind.String:
                    string value = element.GetString();
                    if (!string.IsNullOrEmpty(value) &&
                        value.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        jsonPaths.Add(value);
                    }
                    break;
            }
        }
    }
}