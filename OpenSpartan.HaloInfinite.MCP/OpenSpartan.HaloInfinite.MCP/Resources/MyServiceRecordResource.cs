using Den.Dev.Grunt.Core;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;

namespace OpenSpartan.HaloInfinite.MCP.Resources
{
    public class MyServiceRecordResource() : IResourceProvider
    {
        public IEnumerable<Resource> GetResourceDefinitions()
        {
            yield return new Resource
            {
                Uri = "opsp://player/servicerecord",
                Name = "Player Statistics",
                MimeType = "application/json"
            };
        }

        public IEnumerable<ResourceTemplate> GetResourceTemplates()
        {
            yield return new ResourceTemplate
            {
                UriTemplate = "opsp://player/{stat}",
                Name = "Player Data"
            };
        }

        public bool CanHandleUri(string uri)
        {
            return uri?.StartsWith("opsp://player/", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task<ResourceContents> GetResourceContentsAsync(string uri, CancellationToken cancellationToken)
        {
            if (uri == "opsp://player/servicerecord")
            {
                // Get the player stats from the API

                var playerStats = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.StatsGetPlayerServiceRecord($"xuid({HaloInfiniteAPIBridge.HaloClient.Xuid})", Den.Dev.Grunt.Models.HaloInfinite.LifecycleMode.Matchmade));

                if (playerStats != null)
                {
                    return new ResourceContents
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = playerStats.Response.Message,
                    };
                }
                else
                {
                    return new ResourceContents
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = "{\"error\":true,\"message\":\"Player service record could not be obtained.\",\"code\":\"SERVICE_RECORD_UNAVAILABLE\"}"
                    };
                }
            }

            throw new McpServerException($"Resource not found: {uri}");
        }
    }
}
