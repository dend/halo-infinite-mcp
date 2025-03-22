using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using OpenSpartan.HaloInfinite.MCP.Core;

namespace OpenSpartan.HaloInfinite.MCP.Resources
{
    public class EndpointSettingsResource() : IResourceProvider
    {
        public IEnumerable<Resource> GetResourceDefinitions()
        {
            yield return new Resource
            {
                Uri = "opsp://settings/endpoints",
                Name = "Halo Infinite API endpoints",
                Description = "Provides a list of all available Halo Infinite API endpoints that clients can use. This list is an aggregation of all possible API calls that can be made against the Halo Infinite REST API.",
                MimeType = "application/json"
            };
        }

        public IEnumerable<ResourceTemplate> GetResourceTemplates()
        {
            yield return new ResourceTemplate
            {
                UriTemplate = "opsp://settings/{type}",
                Name = "Halo Infinite settings"
            };
        }

        public bool CanHandleUri(string uri)
        {
            return uri?.StartsWith("opsp://settings/", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public async Task<ResourceContents> GetResourceContentsAsync(string uri, CancellationToken cancellationToken)
        {
            if (uri == "opsp://settings/endpoints")
            {
                var endpointSettings = await HaloInfiniteAPIBridge.SafeAPICall(async () => await HaloInfiniteAPIBridge.HaloClient.GetApiSettingsContainer());

                if (endpointSettings != null)
                {
                    return new ResourceContents
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = endpointSettings.Response.Message,
                    };
                }
                else
                {
                    return new ResourceContents
                    {
                        Uri = uri,
                        MimeType = "application/json",
                        Text = "{\"error\":true,\"message\":\"Settings could not be obtained\",\"code\":\"SETTINGS_UNAVAILABLE\"}"
                    };
                }
            }

            throw new McpServerException($"Resource not found: {uri}");
        }
    }
}
